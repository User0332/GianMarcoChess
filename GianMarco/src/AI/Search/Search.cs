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

	private readonly List<Move>? customOrdered;

	private readonly Board board;
	private readonly TranspositionTable tt;
	private readonly bool isWhite;
	public bool SearchEnded = false;
	public bool KillSearch = false;
	public List<Move>? BestMoves = null;
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

	public List<Move> Execute(ushort depth)
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

		int bestScore = Constants.MinEval;
		var bestMoves = new List<Move>(2) { moves[0] };

		foreach (Move move in moves)
		{
			if (KillSearch) break;

			NodesSearched++;

			board.MakeMove(move);

			byte extension = (byte) (MovePlayedWasInterestingMove(move) ? 1 : 0);

			int score = -NegaMax((ushort) (depth-1+extension), Constants.MinEval, Constants.MaxEval, 1, extension);

			board.UndoMove(move);

			if (score == bestScore) bestMoves.Add(move);

			if (score > bestScore)
			{
				bestScore = score;
				bestMoves.Clear();
				bestMoves.Add(move);
			}
		}
		
		SearchEnded = true;

		BestMoves = bestMoves;
		BestScore = bestScore;

		long timeSearchedMs = (DateTime.Now.Ticks - startTimeTicks) / TimeSpan.TicksPerMillisecond;

		TimeSearchedMs = timeSearchedMs;

		if (!KillSearch)
		{
			Move bestMove = bestMoves[Random.Shared.Next(bestMoves.Count)];
			string scoreString = Evaluator.IsMateScore(bestScore) ? $"mate {Evaluator.ExtractMateInNMoves(bestScore)}" : $"cp {bestScore}";

			Console.WriteLine($"info depth {MaxDepthSearched} time {timeSearchedMs} nodes {NodesSearched} pv {MoveUtils.GetUCI(bestMove)} score {scoreString}");
		}

		return bestMoves;
	}

	int NegaMaxQuiesce(int alpha, int beta, ushort depthFromRoot)
	{
		if (board.IsDraw()) return Constants.DrawValue;
		if (board.IsInCheckmate()) return Evaluator.MateIn(depthFromRoot);

		if (KillSearch) return WorstEvalForBot();

		int eval = Evaluator.EvalPositionWithPerspective(board);

		if (eval >= beta) return beta;

		if (eval > alpha) alpha = eval;
		
		Span<Move> moves = stackalloc Move[CAPTURE_MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, capturesOnly: true);

		foreach (Move move in moves)
		{
			if (KillSearch) return WorstEvalForBot();

			NodesSearched++;

			board.MakeMove(move);

			eval = -NegaMaxQuiesce(-beta, -alpha, (ushort) (depthFromRoot+1));

			board.UndoMove(move);

			if (eval >= beta) return beta;

			if (eval > alpha) alpha = eval;	
		}

		if (depthFromRoot > MaxDepthSearched) MaxDepthSearched = depthFromRoot;

		return alpha;
	}
	public int NegaMax(ushort depth, int alpha, int beta, ushort depthFromRoot, byte numExtensions)
	{
		if (board.IsRepeatedPosition() || board.IsFiftyMoveDraw() || board.IsInsufficientMaterial()) return Constants.DrawValue;

		if (KillSearch) return WorstEvalForBot();

		int lookupVal = tt.LookupEval(depth, alpha, beta);

		if (lookupVal != TranspositionTable.LookupFailed) return lookupVal;

		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot);

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves);

		if (moves.Length == 0)
		{
			if (board.IsInCheck()) return Evaluator.MateIn(depthFromRoot);

			return Constants.DrawValue; // stalemate
		}

		byte evalBound = TranspositionTable.UpperBound;
		Move bestMove = Move.NullMove;

		foreach (Move move in moves)
		{
			if (KillSearch) return WorstEvalForBot();

			NodesSearched++;
			
			board.MakeMove(move);

			byte extension = (byte) ((numExtensions < ExtensionCap) && MovePlayedWasInterestingMove(move) ? (GamePhaseUtils.IsEndgame(board) ? 2 : 1) : 0);

			int eval = -NegaMax((ushort) (depth-1+extension), -beta, -alpha, (ushort) (depthFromRoot+1), (byte) (numExtensions+extension));

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
			}
		}

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

	private List<List<Move>> bestMoveLists = new(4);

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
					bestMoveLists.Any() ? bestMoveLists.SelectMany(item => item).ToList() : null
				);

				searches.Add(search);

				List<Move> bestMoves = search.Execute(i);

				bestMoveLists.Insert(0, bestMoves);

				if (endSearchFlag) return;
			}
		}).Start();
	}

	public Move EndSearch()
	{
		endSearchFlag = true;

		foreach (var search in searches) search.KillSearch = true;

		List<Move> bestMoves = bestMoveLists.First();
		Move bestMove = bestMoves[Random.Shared.Next(bestMoves.Count)];

		BasicSearch lastSearch;

		try { lastSearch = searches.Last(search => search.BestScore != Constants.MinEval); }
		catch (InvalidOperationException) { lastSearch = searches.Last(); }

		string moveName = MoveUtils.GetUCI(bestMove);

		string scoreString = Evaluator.IsMateScore(lastSearch.BestScore) ? $"mate {Evaluator.ExtractMateInNMoves(lastSearch.BestScore)}" : $"cp {lastSearch.BestScore}";
		
		Console.WriteLine($"info depth {lastSearch.MaxDepthSearched} time {lastSearch.TimeSearchedMs} nodes {lastSearch.NodesSearched} pv {moveName} score {scoreString}");

		Console.WriteLine($"bestmove {moveName}");

		return bestMove;
	}
}