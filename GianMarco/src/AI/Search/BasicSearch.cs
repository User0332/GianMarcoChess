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
	const byte TTSizeMB = 128;
	const byte ExtensionCap = 10;
	const short NullMovePruneThreshold = 5;
	const ushort PVSDepth = 2;
	const short FutilityPruneMoveScoreThreshold = 300; // THIS SHOULD BE EQUAL TO THE MoveOrdering.CastleBonus VALUE
	const byte DEPTH_GRACE_THRESHOLD = 0;
	const byte NULL_MOVE_PRUNE_DEPTH = 3;
	const byte FUTUILITY_PRUNE_DEPTH = 1;
	public const byte MAX_LINE_SIZE = 10;

	private readonly List<Move> customOrdered;
	private readonly Board board;
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

	public BasicSearch(Board board, List<Move> customOrderedMoves)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		customOrdered = customOrderedMoves;
		// tt = new(board, (uint) Math.Floor((double) (TTSizeMB*1000000)/Marshal.SizeOf<Entry>()));
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

	public Move Execute(ushort depth, ref CombinationTTable tt)
	{
		long startTimeTicks = DateTime.Now.Ticks;

		bool inCheck = board.IsInCheck();

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, 0);

		// add custom ordered moves to front
		foreach (Move move in moves)
		{
			if (customOrdered.Any(item => item.Equals(move))) continue;

			customOrdered.Add(move);
		}

		moves = CollectionsMarshal.AsSpan(customOrdered);

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		int bestScore = Constants.MinEval;
		
		Move bestMove = moves[0];

		bool foundPV = false;
		bool madeQuietMove = false;
		byte numActionMovesSeen = 0;

		for (int i = 0; i < moves.Length; i++)
		{			
			Move move = moves[i];

			currLine.Fill(Move.NullMove);

			if (KillSearch) break;

			NodesSearched++;

			board.MakeMove(move);

			bool moveWasACheck = board.IsInCheck();
			bool isQuietMove = !(move.IsCapture || move.IsPromotion || moveWasACheck);

			if (!isQuietMove) numActionMovesSeen++;
			else if (numActionMovesSeen > 5)
			{
				board.UndoMove(move);
				continue;
			}

			if (!madeQuietMove && isQuietMove) madeQuietMove = true;

			byte extension = 0; // (byte) (MovePlayedWasInterestingMove(move) ? 1 : 0);

			int score;
			
			if (foundPV && !inCheck && depth >= 3) // NOTE: extension == 0 is currently a placeholder for threat detection
			{
				score = -NegaMax((ushort) Math.Min(PVSDepth, depth-3), -Constants.MinEval-1, -Constants.MinEval, 1, ExtensionCap, ref currLine, false, tt);
			
				currLine.Fill(Move.NullMove);

				if ((score > Constants.MinEval) && (score < Constants.MaxEval))
				{
					score = -NegaMax((ushort) (depth-1+extension), -Constants.MaxEval, -Constants.MinEval, 1, extension, ref currLine, i >= 8, tt);
				}
			}
			else // Normal AlphaBeta NegaMax
			{
				score = -NegaMax((ushort) (depth-1+extension), -Constants.MaxEval, -Constants.MinEval, 1, extension, ref currLine, i >= 8, tt);
			}

			board.UndoMove(move);

			if (score > bestScore)
			{
				bestScore = score;
				bestMove = move;

				currLine[0] = move;
				BestLine = currLine.ToArray();

				foundPV = true;
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

	int NegaMaxQuiesce(int alpha, int beta, ushort depthFromRoot, int maxDepth = Constants.MaxEval)
	{
		if (maxDepth == 0) return Evaluator.EvalPositionWithPerspective(board);

		if (depthFromRoot > MaxDepthSearched) MaxDepthSearched = depthFromRoot;
		
		// if commented out, reduces runtime (recongized depth of 9 search -(200)ms), check if this leads to bad play
		if (board.IsDraw()) return Constants.DrawValue;
		if (board.IsInCheckmate()) return Evaluator.MateIn(depthFromRoot);

		if (KillSearch) return WorstEvalForBot;

		int eval = Evaluator.EvalPositionWithPerspective(board);

		if (eval >= beta) return beta;

		if (eval > alpha) alpha = eval;
		
		Span<Move> moves = stackalloc Move[CAPTURE_MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot, capturesOnly: true);

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			if (KillSearch) return WorstEvalForBot;

			NodesSearched++;

			board.MakeMove(move);

			eval = -NegaMaxQuiesce(-beta, -alpha, (ushort) (depthFromRoot+1), maxDepth-1);

			board.UndoMove(move);

			if (eval >= beta) return beta;

			if (eval > alpha)
			{
				alpha = eval;
			}
		}

		return alpha;
	}

	public int NegaMax(ushort depth, int alpha, int beta, ushort depthFromRoot, byte numExtensions, ref Span<Move> currLineBuilder, bool nullMovePruningEnabled, in CombinationTTable tt)
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
			return lookupVal;
		}

		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot);

		bool inCheck = board.IsInCheck();
		int eval;

		Span<Move> throwaway = stackalloc Move[MAX_LINE_SIZE];

		// Null move pruning -- removed for now due to occasional blunders
		// if (!inCheck && (depth >= NULL_MOVE_PRUNE_DEPTH) && nullMovePruningEnabled)
		// {
		// 	board.MakeMove(Move.NullMove);			
			
		// 	eval = -NegaMax((ushort) (depth-NULL_MOVE_PRUNE_DEPTH), -beta, -beta+1, (ushort) (depthFromRoot+1), ExtensionCap, ref throwaway, false, tt);

		// 	board.UndoMove(Move.NullMove);
			
		// 	if ((eval >= beta-NullMovePruneThreshold) && !(Evaluator.IsMateScore(eval) && Evaluator.ExtractMateInNMoves(eval) > 0))
		// 	{
		// 		return eval;
		// 	}
		// }

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot);

		if (moves.Length == 0)
		{
			if (inCheck)
			{
				return Evaluator.MateIn(depthFromRoot);
			}

			return Constants.DrawValue; // stalemate
		}

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		bool foundPV = false;
		bool madeQuietMove = false;

		byte evalBound = TranspositionTable.UpperBound;
		Move bestMove = moves[0];

		bool canSacrificePrune = true;
		byte numActionMovesSeen = 0;

		int i = -1;

		ref var move = ref MemoryMarshal.GetReference(moves);
		ref var end = ref Unsafe.Add(ref move, moves.Length);

		while (Unsafe.IsAddressLessThan(ref move, ref end))
		{
			i++;
			currLine.Fill(Move.NullMove);
			canSacrificePrune = !canSacrificePrune;

			if (KillSearch)
			{
				return WorstEvalForBot;
			}

			NodesSearched++;
			
			board.MakeMove(move);

			bool moveWasACheck = board.IsInCheck();
			bool isQuietMove = !(move.IsCapture || move.IsPromotion || moveWasACheck);

			if (!isQuietMove) numActionMovesSeen++; // try beat depth 10 -> 6549158 nodes
			else if (numActionMovesSeen > 5)
			{
				board.UndoMove(move);

				move = ref Unsafe.Add(ref move, 1);
				continue;
			}

			// futility pruning -- it is depth 1
			if (!inCheck && !foundPV && depth <= FUTUILITY_PRUNE_DEPTH && madeQuietMove && isQuietMove)
			{
				board.UndoMove(move);
				
				move = ref Unsafe.Add(ref move, 1);
				continue;
			}

			// custom pruning method -- sacrificial pruning -- get below 1305771 depth 9 search
			// if (false && !foundPV && (Math.Abs(Math.Abs(alpha)-Math.Abs(NegaMaxQuiesce(-beta, -alpha, (ushort) (depthFromRoot+1), maxDepth: 1))) > 300) && depth <= 3)
			// {
			// 	 // huge game sacrifice without sufficient depth to search it through, just skip
			// 	board.UndoMove(move);
			// 	continue;
			// }

			if (!madeQuietMove && isQuietMove) madeQuietMove = true;

			byte extension = 0; // FOR NOW, EXTENSIONS ARE DISABLED - LEADS TO LESS NODE SEARCHES

			// PVS

			if (foundPV && !inCheck && (depth >= 3)) // NOTE: extension == 0 is currently a placeholder for threat detection
			{
				eval = -NegaMax((ushort) Math.Min(PVSDepth, depth-3), -alpha-1, -alpha, (ushort) (depthFromRoot+1), ExtensionCap, ref currLine, false, tt);
			
				currLine.Fill(Move.NullMove);

				if ((eval > alpha) && (eval < beta))
				{
					eval = -NegaMax((ushort) (depth-1+extension), -beta, -alpha, (ushort) (depthFromRoot+1), (byte) (numExtensions+extension), ref currLine, i >= 8, tt);
				}
			}
			else // Normal AlphaBeta NegaMax
			{
				eval = -NegaMax((ushort) (depth-1+extension), -beta, -alpha, (ushort) (depthFromRoot+1), (byte) (numExtensions+extension), ref currLine, i >= 8, tt);
			}

			board.UndoMove(move);

			if (eval >= beta)
			{
				tt.StoreEvaluation(depth, beta, TranspositionTable.LowerBound, move);
				
				if (!(move.IsCapture || move.IsPromotion))
				{
					if (depthFromRoot < MoveOrdering.MaxKillerMovePly)
					{
						ref Move[] arr = ref MoveOrdering.killerMoves[depthFromRoot];
						
						
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

			move = ref Unsafe.Add(ref move, 1);
		}

		if (depthFromRoot < MAX_LINE_SIZE) currLineBuilder[depthFromRoot] = bestMove;

		tt.StoreEvaluation(depth, alpha, evalBound, bestMove);

		return alpha;
	}
}