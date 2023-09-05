using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;
using GianMarco.TTable;

namespace GianMarco.Search;

class BasicSearch
{
	const byte MOVE_STACKALLOC_AMT = 218;
	const byte CAPTURE_MOVE_STACKALLOC_AMT = 100;
	const byte TTSizeMB = 64;
	const byte ExtensionCap = 10;
	const short NullMovePruneThreshold = 20;
	public const byte MAX_LINE_SIZE = 10;

	private readonly List<Move>? customOrdered;

	private readonly Board board;
	private readonly TranspositionTable tt;
	private readonly bool isWhite;
	public bool SearchEnded = false;
	public bool KillSearch = false;
	public Move BestMove = Move.NullMove;
	public IEnumerable<Move>? BestLine = null;
	public long TimeSearchedMs = 0;
	public int BestScore = Constants.MinEval;
	public ushort MaxDepthSearched = 0;
	public uint NodesSearched = 0;

	public BasicSearch(Board board, List<Move>? customOrderedMoves = null)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		customOrdered = customOrderedMoves;
		tt = new(board, (uint) Math.Floor((double) (TTSizeMB*1000000)/Marshal.SizeOf<Entry>()));
	}

	void GetOrderedLegalMoves(ref Span<Move> moveSpan, bool capturesOnly = false)
	{
		board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly: capturesOnly);

		MoveOrdering.OrderMoves(board, ref moveSpan);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int WorstEvalForBot()
	{
		return board.IsWhiteToMove && isWhite ? Constants.MinEval : Constants.MaxEval;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool MovePlayedWasInterestingMove(Move move)
	{
		return board.IsInCheck();
	}

	public Move Execute(ushort depth)
	{
		long startTimeTicks = DateTime.Now.Ticks;

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves);

		if (customOrdered is not null)
		{
			// add custom ordered moves to front
			foreach (Move move in moves)
			{
				if (customOrdered.Any(item => item.Equals(move))) continue;

				customOrdered.Add(move);
			}

			moves = CollectionsMarshal.AsSpan(customOrdered);
		}

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		int bestScore = Constants.MinEval;
		Move bestMove = moves[0];

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			if (KillSearch) break;

			NodesSearched++;

			board.MakeMove(move);

			byte extension = (byte) (MovePlayedWasInterestingMove(move) ? 1 : 0);

			int score = -NegaMax((ushort) (depth-1+extension), Constants.MinEval, Constants.MaxEval, 1, extension, ref currLine);

			board.UndoMove(move);

			if (score > bestScore)
			{
				bestScore = score;
				bestMove = move;
				currLine[0] = move;
				BestLine = currLine.ToArray();
			}
		}
		
		SearchEnded = true;

		BestLine ??= Array.Empty<Move>();
		

		BestLine = BestLine.TakeWhile((move) => move != Move.NullMove);
		BestMove = bestMove;
		BestScore = bestScore;

		long timeSearchedMs = (DateTime.Now.Ticks - startTimeTicks) / TimeSpan.TicksPerMillisecond;

		TimeSearchedMs = timeSearchedMs;

		if (!KillSearch)
		{
			string scoreString = Evaluator.IsMateScore(bestScore) ? $"mate {Evaluator.ExtractMateInNMoves(bestScore)}" : $"cp {bestScore}";
			string lineString = string.Join(' ', BestLine.Select(MoveUtils.GetUCI));

			Console.WriteLine($"info depth {depth} seldepth {MaxDepthSearched} time {timeSearchedMs} nodes {NodesSearched} score {scoreString} pv {lineString}");
		}

		return bestMove;
	}

	int NegaMaxQuiesce(int alpha, int beta, ushort depthFromRoot, ref Span<Move> currLineBuilder)
	{
		if (board.IsDraw()) return Constants.DrawValue;
		if (board.IsInCheckmate()) return Evaluator.MateIn(depthFromRoot);

		if (KillSearch) return WorstEvalForBot();

		int eval = Evaluator.EvalPositionWithPerspective(board);

		if (eval >= beta) return beta;

		if (eval > alpha) alpha = eval;
		
		Span<Move> moves = stackalloc Move[CAPTURE_MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, capturesOnly: true);

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		Move bestMove = Move.NullMove;

		foreach (Move move in moves)
		{
			if (KillSearch) return WorstEvalForBot();

			NodesSearched++;

			board.MakeMove(move);

			eval = -NegaMaxQuiesce(-beta, -alpha, (ushort) (depthFromRoot+1), ref currLine);

			board.UndoMove(move);

			if (eval >= beta) return beta;

			if (eval > alpha)
			{
				alpha = eval;
				bestMove = move;

				currLine.CopyTo(currLineBuilder);
			}
		}

		if (depthFromRoot > MaxDepthSearched) MaxDepthSearched = depthFromRoot;

		return alpha;
	}
	public int NegaMax(ushort depth, int alpha, int beta, ushort depthFromRoot, byte numExtensions, ref Span<Move> currLineBuilder)
	{
		if (board.IsRepeatedPosition() || board.IsFiftyMoveDraw() || board.IsInsufficientMaterial())
		{
			if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = Move.NullMove;
			return Constants.DrawValue;
		}

		if (KillSearch)
		{
			if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = Move.NullMove;
			return WorstEvalForBot();
		}

		int lookupVal = tt.LookupEval(depth, alpha, beta);

		if (lookupVal != TranspositionTable.LookupFailed)
		{
			if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = Move.NullMove;
			return lookupVal;
		}


		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot, ref currLineBuilder);

		bool inCheck = board.IsInCheck();

		// Null move pruning
		// if ((depth >= 3) && !inCheck)
		// {
		// 	board.MakeMove(Move.NullMove);
			
		// 	Span<Move> throwaway = stackalloc Move[MAX_LINE_SIZE];
			
		// 	int eval = -NegaMax((ushort) (depth-3), -beta, -alpha, (ushort) (depthFromRoot+1), numExtensions, ref throwaway);
			
		// 	board.UndoMove(Move.NullMove);

		// 	if (eval >= beta) return beta;
		// }

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves);

		if (moves.Length == 0)
		{
			if (inCheck)
			{
				if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = Move.NullMove;
				return Evaluator.MateIn(depthFromRoot);
			}

			if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = Move.NullMove;

			return Constants.DrawValue; // stalemate
		}

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		byte evalBound = TranspositionTable.UpperBound;
		Move bestMove = Move.NullMove;

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			if (KillSearch)
			{
				if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = Move.NullMove;
				return WorstEvalForBot();
			}

			NodesSearched++;
			
			board.MakeMove(move);

			byte extension = (byte) ((numExtensions < ExtensionCap) && MovePlayedWasInterestingMove(move) ? (GamePhaseUtils.IsEndgame(board) ? 2 : 1) : 0);

			int eval = -NegaMax((ushort) (depth-1+extension), -beta, -alpha, (ushort) (depthFromRoot+1), (byte) (numExtensions+extension), ref currLine);

			board.UndoMove(move);

			if (eval >= beta)
			{
				tt.StoreEvaluation(depth, beta, TranspositionTable.LowerBound, move);
				return beta;
			}

			if (eval > alpha)
			{
				evalBound = TranspositionTable.Exact;
				bestMove = move;

				alpha = eval;

				currLine.CopyTo(currLineBuilder);
			}
		}

		if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = bestMove;

		tt.StoreEvaluation(depth, alpha, evalBound, bestMove);

		return alpha;
	}
}

class IterDeepSearch
{
	public const ushort MAX_DEPTH = 40;
	private Board board;
	private ushort maxDepth;
	private ushort startDepth = 1;
	private bool endSearchFlag = false;

	private List<BasicSearch> searches = new(5);

	private readonly List<Move>[] bestMoveLinesTransformed = new List<Move>[BasicSearch.MAX_LINE_SIZE] { new(), new(), new(), new(), new(), new(), new(), new(), new(), new() };
	private readonly List<Move> bestMoves = new(6);

	public IterDeepSearch(Board board, ushort maxDepth)
	{
		if (maxDepth < startDepth) maxDepth = startDepth;
		
		this.board = board;
		this.maxDepth = maxDepth;
	}

	public void Search()
	{
		new Thread(() => {
			for (ushort i = startDepth; i<=maxDepth; i++)
			{
				var search = new BasicSearch(
					board,
					bestMoves
				);

				searches.Add(search);

				Move bestMove = search.Execute(i);

				bestMoves.Insert(0, bestMove);

				if (endSearchFlag) return;
			}
		}).Start();
	}

	public Move EndSearch()
	{
		endSearchFlag = true;

		foreach (var search in searches) search.KillSearch = true;

		BasicSearch lastSearch;

		try { lastSearch = searches.Last(search => Math.Abs(search.BestScore) != Constants.MaxEval); }
		catch (InvalidOperationException) { lastSearch = searches.Last(); }

		string moveName = MoveUtils.GetUCI(lastSearch.BestMove);

		string scoreString = Evaluator.IsMateScore(lastSearch.BestScore) ? $"mate {Evaluator.ExtractMateInNMoves(lastSearch.BestScore)}" : $"cp {lastSearch.BestScore}";
		string lineString = string.Join(' ', lastSearch.BestLine.Select(MoveUtils.GetUCI));

		Console.WriteLine($"info depth {searches.Count} seldepth {lastSearch.MaxDepthSearched} time {lastSearch.TimeSearchedMs} nodes {lastSearch.NodesSearched} score {scoreString} pv {lineString}");

		Console.WriteLine($"bestmove {moveName}");

		return lastSearch.BestMove;
	}
}