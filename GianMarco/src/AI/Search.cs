using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;

namespace GianMarco.Search;

class BasicSearch
{
	const byte MOVE_STACKALLOC_AMT = 100;

	private readonly Board board;
	public bool SearchEnded = false;

	public bool KillSearch = false;
	public Move? BestMove = null;

	public BasicSearch(Board board)
	{
		this.board = board;
	}

	void GetOrderedLegalMoves(ref Span<Move> moveSpan, bool capturesOnly = false)
	{
		board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly: capturesOnly);

		MoveOrdering.OrderMoves(board, ref moveSpan);
	}

	public Move Execute(int depth)
	{
		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves);

		int bestScore = 0;
		Move bestMove = moves[0];

		foreach (Move move in moves)
		{
			if (KillSearch) { return Move.NullMove; }

			board.MakeMove(move);

			int score = -NegaMax(depth, Constants.MinEval, Constants.MaxEval);

			board.UndoMove(move);

			if (score > bestScore)
			{
				bestScore = score;
				bestMove = move;
			}
		}
		
		SearchEnded = true;

		BestMove = bestMove;

		Console.WriteLine(bestScore);

		return bestMove;
	}

	int NegaMaxQuiesce(int alpha, int beta)
	{
		if (KillSearch) { return 0; }

		int stand_pat = Evaluator.EvalPositionWithPerspective(board);

		if (stand_pat >= beta) return beta;

		alpha = Math.Max(alpha, stand_pat);

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, capturesOnly: true);

		foreach (Move move in moves)
		{
			if (KillSearch) { return 0; }
			board.MakeMove(move);

			int eval = -NegaMaxQuiesce(-beta, -alpha);

			board.UndoMove(move);

			if (eval >= beta) return beta;

			alpha = Math.Max(alpha, eval);			
		}

		return alpha;
	}
	public int NegaMax(int depth, int alpha, int beta)
	{
		if (KillSearch) { return 0; }

		if (depth == 0) return NegaMaxQuiesce(alpha, beta);

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves);

		if (moves.Length == 0)
		{
			if (board.IsInCheck()) return Constants.MinEval; // checkmate!

			return Constants.DrawValue; // stalemate
		}

		foreach (Move move in moves)
		{
			if (KillSearch) { return 0; }
			
			board.MakeMove(move);

			int eval = -NegaMax(depth-1, -beta, -alpha);

			board.UndoMove(move);

			if (eval >= beta) return beta;

			alpha = Math.Max(alpha, eval);
		}

		return alpha;
	}
}

class IterDeepSearch
{
	private string boardFEN;
	private ushort maxDepth;
	private ushort startDepth = 3;
	private Dictionary<ushort, BasicSearch> searches = new(5);
	public IterDeepSearch(Board board, ushort maxDepth)
	{
		boardFEN = board.GetFenString();
		this.maxDepth = maxDepth;
	}

	public void StartSearching()
	{
		for (ushort i = startDepth; i<=maxDepth; i++)
		{
			var search = new BasicSearch(
				Board.CreateBoardFromFEN(boardFEN) // create a copy of the board for every search
			);

			var thread = new Thread(() => search.Execute(i));
			thread.Start();

			searches.Add(i, search);
		}
	}

	public Move TerminateSearch()
	{
		ushort bestDepth = startDepth;

		while (!searches[startDepth].SearchEnded);

		Move bestMove = (Move) searches[startDepth].BestMove;

		searches.Remove(startDepth);

		foreach (var entry in searches)
		{
			if (entry.Key > bestDepth)
			{
				if (entry.Value.SearchEnded)
				{
					bestMove = (Move) entry.Value.BestMove;
					bestDepth = entry.Key;
				}
				else entry.Value.KillSearch = true;
			}
		}

		searches.Clear();

		return bestMove;
	}
}