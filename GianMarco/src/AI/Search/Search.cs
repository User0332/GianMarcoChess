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
	public const byte MAX_LINE_SIZE = 10;

	private readonly List<Move>[]? customOrderedLines;

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

	public BasicSearch(Board board, List<Move>[]? customOrderedLines = null)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		this.customOrderedLines = customOrderedLines;
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

		bool customOrderedIsValid = (customOrderedLines is not null) && (customOrderedLines[0].Count != 0);

		if (customOrderedIsValid)
		{
			// add custom ordered moves to front
			foreach (Move move in moves)
			{
				if (customOrderedLines[0].Any(item => item.Equals(move))) continue;

				customOrderedLines[0].Add(move);
			}

			moves = CollectionsMarshal.AsSpan(customOrderedLines[0]);
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

			int playedMoveLineIndex;

			if (customOrderedIsValid && (i < customOrderedLines[0].Count))
				playedMoveLineIndex = i;
			else
				playedMoveLineIndex = -1;

			int score = -NegaMax((ushort) (depth-1+extension), Constants.MinEval, Constants.MaxEval, 1, extension, ref currLine, playedMoveLineIndex);

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

		// if ((depthFromRoot < 10) && (customOrderedLines is not null) && (customOrderedLines[depthFromRoot].Count != 0))
		// {
		// 	// add custom ordered moves to front
		// 	foreach (Move move in moves)
		// 	{
		// 		if (customOrderedLines[depthFromRoot].Any(item => item.Equals(move))) continue;

		// 		customOrderedLines[depthFromRoot].Add(move);
		// 	}

		// 	moves = CollectionsMarshal.AsSpan(customOrderedLines[depthFromRoot]);
		// }

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

		if (depthFromRoot < 10) currLineBuilder[depthFromRoot] = bestMove;

		if (depthFromRoot > MaxDepthSearched) MaxDepthSearched = depthFromRoot;

		return alpha;
	}
	public int NegaMax(ushort depth, int alpha, int beta, ushort depthFromRoot, byte numExtensions, ref Span<Move> currLineBuilder, int playedMoveLineIndex)
	{
		if (board.IsRepeatedPosition() || board.IsFiftyMoveDraw() || board.IsInsufficientMaterial()) return Constants.DrawValue;

		if (KillSearch) return WorstEvalForBot();

		int lookupVal = tt.LookupEval(depth, alpha, beta);

		if (lookupVal != TranspositionTable.LookupFailed) return lookupVal;

		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot, ref currLineBuilder);

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves);

		if (moves.Length == 0)
		{
			if (board.IsInCheck()) return Evaluator.MateIn(depthFromRoot);

			return Constants.DrawValue; // stalemate
		}

		bool customOrderedIsValid = (depthFromRoot < 10) && (customOrderedLines is not null) && (customOrderedLines[depthFromRoot].Count != 0) && (playedMoveLineIndex != -1) && (customOrderedLines[depthFromRoot].Count > playedMoveLineIndex);

		if (customOrderedIsValid)
		{
			Move addToFront = customOrderedLines[depthFromRoot][playedMoveLineIndex];
			Move[] newOrdered = new Move[moves.Length+1];

			for (int i = 1; i < newOrdered.Length; i++) newOrdered[i] = moves[i-1];

			newOrdered[0] = addToFront;

			moves = newOrdered.AsSpan();
		}

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		byte evalBound = TranspositionTable.UpperBound;
		Move bestMove = Move.NullMove;

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			if (KillSearch) return WorstEvalForBot();

			NodesSearched++;
			
			board.MakeMove(move);

			byte extension = (byte) ((numExtensions < ExtensionCap) && MovePlayedWasInterestingMove(move) ? (GamePhaseUtils.IsEndgame(board) ? 2 : 1) : 0);

			if (!(customOrderedIsValid && i == 0)) playedMoveLineIndex = -1;

			int eval = -NegaMax((ushort) (depth-1+extension), -beta, -alpha, (ushort) (depthFromRoot+1), (byte) (numExtensions+extension), ref currLine, playedMoveLineIndex);

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
					bestMoveLinesTransformed
				);

				searches.Add(search);

				search.Execute(i);

				Move[] bestLine = search.BestLine.ToArray();

				for (int j = 0; j < bestLine.Length; j++)
				{
					bestMoveLinesTransformed[j].Insert(0, bestLine[j]);
				}

				if (endSearchFlag) return;
			}
		}).Start();
	}

	public Move EndSearch()
	{
		endSearchFlag = true;

		foreach (var search in searches) search.KillSearch = true;

		BasicSearch lastSearch;

		try { lastSearch = searches.Last(search => search.BestScore != Constants.MinEval); }
		catch (InvalidOperationException) { lastSearch = searches.Last(); }

		string moveName = MoveUtils.GetUCI(lastSearch.BestMove);

		string scoreString = Evaluator.IsMateScore(lastSearch.BestScore) ? $"mate {Evaluator.ExtractMateInNMoves(lastSearch.BestScore)}" : $"cp {lastSearch.BestScore}";
		string lineString = string.Join(' ', lastSearch.BestLine.Select(MoveUtils.GetUCI));

		Console.WriteLine($"info depth {searches.Count} seldepth {lastSearch.MaxDepthSearched} time {lastSearch.TimeSearchedMs} nodes {lastSearch.NodesSearched} score {scoreString} pv {lineString}");

		Console.WriteLine($"bestmove {moveName}");

		return lastSearch.BestMove;
	}
}