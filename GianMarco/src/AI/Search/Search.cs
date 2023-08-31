using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;

namespace GianMarco.Search;

class BasicSearch
{
	const byte MOVE_STACKALLOC_AMT = 218;

	private readonly List<Move>? customOrdered;

	private readonly Board board;
	private readonly bool isWhite;
	public bool SearchEnded = false;
	public bool KillSearch = false;
	public List<Move>? BestMoves = null;
	public long TimeSearchedMs = 0;
	public short BestScore = Constants.MinEval;

	public BasicSearch(Board board, List<Move>? customOrderedMoves = null)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		customOrdered = customOrderedMoves;
	}

	void GetOrderedLegalMoves(ref Span<Move> moveSpan, bool capturesOnly = false)
	{
		board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly: capturesOnly);

		MoveOrdering.OrderMoves(board, ref moveSpan);
	}

	void PrintIterable<T>(Span<T> values)
	{
		foreach (var item in values)
		{
			Console.Write($"{item} ");
		}

		Console.WriteLine();
	}

	void PrintIterable<T>(IEnumerable<T> values)
	{
		foreach (var item in values)
		{
			Console.Write($"{item} ");
		}

		Console.WriteLine();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	short WorstEvalForBot()
	{
		return (board.IsWhiteToMove && isWhite) ? Constants.MinEval : Constants.MaxEval;
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
				if (!customOrdered.Contains(move))
					customOrdered.Add(move);
			}

			moves = CollectionsMarshal.AsSpan(customOrdered);
		}

		short bestScore = Constants.MinEval;
		var bestMoves = new List<Move>(2) { moves[0] };

		foreach (Move move in moves)
		{
			if (KillSearch) break;

			board.MakeMove(move);

			short score = (short) -NegaMax((ushort) (depth-1), Constants.MinEval, Constants.MaxEval);

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
			Console.WriteLine($"info depth {depth} time {timeSearchedMs} pv {MoveUtils.GetUCI(bestMove)} score cp {bestScore}");
		}

		return bestMoves;
	}

	short NegaMaxQuiesce(short alpha, short beta)
	{
		if (KillSearch) return WorstEvalForBot();

		if (board.IsInCheckmate()) return Constants.MinEval;
		if (board.IsDraw()) return Constants.DrawValue;

		short stand_pat = Evaluator.EvalPositionWithPerspective(board);

		if (stand_pat >= beta) return beta;

		alpha = Math.Max(alpha, stand_pat);

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, capturesOnly: true);

		foreach (Move move in moves)
		{
			if (KillSearch) return WorstEvalForBot();

			board.MakeMove(move);

			short eval = (short) -NegaMaxQuiesce((short) -beta, (short) -alpha);

			board.UndoMove(move);

			if (eval >= beta) return beta;

			alpha = Math.Max(alpha, eval);			
		}

		return alpha;
	}
	public short NegaMax(ushort depth, short alpha, short beta)
	{
		if (KillSearch) return WorstEvalForBot();

		if (depth == 0) return NegaMaxQuiesce(alpha, beta);

		if (board.IsInCheckmate()) return Constants.MinEval;
		if (board.IsDraw()) return Constants.DrawValue;

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves);

		foreach (Move move in moves)
		{
			if (KillSearch) return WorstEvalForBot();
			
			board.MakeMove(move);

			short eval = (short) -NegaMax((ushort) (depth-1), (short) -beta, (short) -alpha);

			board.UndoMove(move);

			if (eval >= beta) return beta;

			alpha = Math.Max(alpha, eval);
		}

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
					bestMoveLists.Any() ? bestMoveLists.SelectMany(item => item).Reverse().ToList() : null
				);

				searches.Add(search);

				List<Move> bestMoves = search.Execute(i);

				bestMoveLists.Add(bestMoves);

				if (endSearchFlag) return;
			}
		}).Start();
	}

	public Move EndSearch()
	{
		endSearchFlag = true;

		foreach (var search in searches) search.KillSearch = true;

		List<Move> bestMoves = bestMoveLists.Last();
		Move bestMove = bestMoves[Random.Shared.Next(bestMoves.Count)];

		BasicSearch lastSearch;

		try { lastSearch = searches.Last(search => search.BestScore != Constants.MinEval); }
		catch (InvalidOperationException) { lastSearch = searches.Last(); }

		string moveName = MoveUtils.GetUCI(bestMove);
		
		Console.WriteLine($"info depth {searches.Count} time {lastSearch.TimeSearchedMs} pv {moveName} score cp {lastSearch.BestScore}");

		Console.WriteLine($"bestmove {moveName}");

		return bestMove;
	}
}