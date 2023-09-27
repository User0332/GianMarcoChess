using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;
using GianMarco.TTable;

namespace GianMarco.Search;

class BasicSearch
{
	const byte MOVE_STACKALLOC_AMT = 218;
	const byte CAPTURE_MOVE_STACKALLOC_AMT = 100;
	const byte TTSizeMB = 128;
	const byte ExtensionCap = 10;
	const short NullMovePruneThreshold = 5;
	const ushort PVSDepth = 3;
	const short FutilityPruneMoveScoreThreshold = 300; // THIS SHOULD BE EQUAL TO THE MoveOrdering.CastleBonus VALUE
	const byte DEPTH_GRACE_THRESHOLD = 0;
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
	private readonly int WorstEvalForBot;
	public ushort MaxDepthSearched = 0;
	public uint NodesSearched = 0;

	public BasicSearch(Board board, List<Move>? customOrderedMoves = null)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		customOrdered = customOrderedMoves;
		tt = new(board, (uint) Math.Floor((double) (TTSizeMB*1000000)/Marshal.SizeOf<Entry>()));
		WorstEvalForBot = board.IsWhiteToMove && isWhite ? Constants.MinEval : Constants.MaxEval;
	}

	public BasicSearch(Board board, TranspositionTable transpositionTable, List<Move>? customOrderedMoves = null)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		customOrdered = customOrderedMoves;
		tt = transpositionTable;
		WorstEvalForBot = board.IsWhiteToMove && isWhite ? Constants.MinEval : Constants.MaxEval;
	}

	void GetOrderedLegalMoves(ref Span<Move> moveSpan, ushort depthFromRoot, bool capturesOnly = false)
	{
		board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly: capturesOnly);

		MoveOrdering.OrderMoves(board, ref moveSpan, !capturesOnly, depthFromRoot);
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

		GetOrderedLegalMoves(ref moves, 0);

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

			int score = -NegaMax((ushort) (depth-1+extension), Constants.MinEval, Constants.MaxEval, 1, extension, ref currLine, true);

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

	int NegaMaxQuiesce(int alpha, int beta, ushort depthFromRoot)
	{
		if (depthFromRoot > MaxDepthSearched) MaxDepthSearched = depthFromRoot;
		
		if (board.IsDraw()) return Constants.DrawValue;
		if (board.IsInCheckmate()) return Evaluator.MateIn(depthFromRoot);

		if (KillSearch) return WorstEvalForBot;

		int eval = Evaluator.EvalPositionWithPerspective(board);

		if (eval >= beta) return beta;

		if (eval > alpha) alpha = eval;
		
		Span<Move> moves = stackalloc Move[CAPTURE_MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot, capturesOnly: true);

		foreach (Move move in moves)
		{
			if (KillSearch) return WorstEvalForBot;

			NodesSearched++;

			board.MakeMove(move);

			eval = -NegaMaxQuiesce(-beta, -alpha, (ushort) (depthFromRoot+1));

			board.UndoMove(move);

			if (eval >= beta) return beta;

			if (eval > alpha)
			{
				alpha = eval;
			}
		}

		return alpha;
	}

	public int NegaMax(ushort depth, int alpha, int beta, ushort depthFromRoot, byte numExtensions, ref Span<Move> currLineBuilder, bool nullMovePruningEnabled)
	{
		if (depthFromRoot > MaxDepthSearched) MaxDepthSearched = depthFromRoot;

		if (board.IsRepeatedPosition() || board.IsFiftyMoveDraw() || board.IsInsufficientMaterial())
		{
			return Constants.DrawValue;
		}

		if (KillSearch)
		{
			return WorstEvalForBot;
		}

		int lookupVal = tt.LookupEval(depth-DEPTH_GRACE_THRESHOLD, alpha, beta);

		if (lookupVal != TranspositionTable.LookupFailed)
		{
			if (depthFromRoot < MAX_LINE_SIZE) currLineBuilder[depthFromRoot] = Move.NullMove;
			return lookupVal;
		}

		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot);

		bool inCheck = board.IsInCheck();
		int eval;

		// Null move pruning
		if (!inCheck && (depth >= 3) && nullMovePruningEnabled)
		{
			board.MakeMove(Move.NullMove);			
			Span<Move> throwaway = stackalloc Move[MAX_LINE_SIZE];
			
			eval = -NegaMax((ushort) (depth-3), -beta, -beta+1, (ushort) (depthFromRoot+1), ExtensionCap, ref throwaway, false);

			board.UndoMove(Move.NullMove);
			
			if ((eval >= beta-NullMovePruneThreshold) && !(Evaluator.IsMateScore(eval) && Evaluator.ExtractMateInNMoves(eval) > 0))
			{
				return eval;
			}
		}

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot);

		if (moves.Length == 0)
		{
			if (inCheck)
			{
				if (depthFromRoot < MAX_LINE_SIZE) currLineBuilder[depthFromRoot] = Move.NullMove;
				return Evaluator.MateIn(depthFromRoot);
			}

			if (depthFromRoot < MAX_LINE_SIZE) currLineBuilder[depthFromRoot] = Move.NullMove;

			return Constants.DrawValue; // stalemate
		}

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		bool foundPV = false;
		bool madeQuietMove = false;

		byte evalBound = TranspositionTable.UpperBound;
		Move bestMove = Move.NullMove;

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			if (KillSearch)
			{
				return WorstEvalForBot;
			}

			NodesSearched++;
		
			// futility pruning -- it is depth 2 & below so make sure it doesnt play badly
			if (!inCheck && !foundPV && depth <= 2 && madeQuietMove && !(move.IsCapture || move.IsPromotion))
			{
				board.MakeMove(move);

				bool moveWasACheck = board.IsInCheck();

				board.UndoMove(move);

				if (!moveWasACheck) continue;
			}
			
			board.MakeMove(move);


			if (!madeQuietMove && !(move.IsCapture || move.IsPromotion)) madeQuietMove = true;

			byte extension = 0; // FOR NOW, EXTENSIONS ARE DISABLED - LEADS TO LESS NODE SEARCHES

			// PVS

			if (foundPV && (extension == 0) && (depth >= 3)) // NOTE: extension == 0 is currently a placeholder for threat detection
			{
				eval = -NegaMax((ushort) Math.Min(PVSDepth, depth-3), -alpha-1, -alpha, (ushort) (depthFromRoot+1), ExtensionCap, ref currLine, false);
			
				if ((eval > alpha) && (eval < beta))
				{
					eval = -NegaMax((ushort) (depth-1+extension), -beta, -alpha, (ushort) (depthFromRoot+1), (byte) (numExtensions+extension), ref currLine, i >= 6);
				}
			}
			else // Normal AlphaBeta NegaMax
			{
				eval = -NegaMax((ushort) (depth-1+extension), -beta, -alpha, (ushort) (depthFromRoot+1), (byte) (numExtensions+extension), ref currLine, i >= 6);
			}

			board.UndoMove(move);

			if (eval >= beta)
			{
				tt.StoreEvaluation(depth, beta, TranspositionTable.LowerBound, move);
				
				if (!(move.IsCapture || move.IsPromotion))
				{
					if (depthFromRoot < MoveOrdering.MaxKillerMovePly)
					{
						Move[] arr = MoveOrdering.killerMoves[depthFromRoot];
						arr[1] = arr[0];
						arr[0] = move;
					}
				}
				
				return beta;
			}

			if (eval > alpha)
			{
				evalBound = TranspositionTable.Exact;
				bestMove = move;

				alpha = eval;

				currLine.CopyTo(currLineBuilder);

				foundPV = true;

				if (!(move.IsCapture || move.IsPromotion))
				{
					
				}
			}
		}

		if (depthFromRoot < MAX_LINE_SIZE) currLineBuilder[depthFromRoot] = bestMove;

		tt.StoreEvaluation(depth, alpha, evalBound, bestMove);

		return alpha;
	}
}