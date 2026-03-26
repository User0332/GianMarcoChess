using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;
using GianMarco.TTable;

namespace GianMarco.Search;

class BasicSearch
{
	const int MOVE_STACKALLOC_AMT = 218;
	const int CAPTURE_MOVE_STACKALLOC_AMT = 100;
	const int TTSizeMB = 128;
	const int ExtensionCap = 10;
	const int NullMovePruneThreshold = 5;
	const int PVSDepth = 2;
	const int FutilityPruneMoveScoreThreshold = 300; // THIS SHOULD BE EQUAL TO THE MoveOrdering.CastleBonus VALUE
	const int DEPTH_GRACE_THRESHOLD = 0;
	const int NULL_MOVE_PRUNE_DEPTH = 3;
	const int FUTUILITY_PRUNE_DEPTH = 1;
	public const int MAX_LINE_SIZE = 10;

	private readonly List<Move> customOrdered;
	private readonly Board board;
	private readonly bool isWhite;
	public bool SearchEnded = false;
	public bool KillSearch = false;
	public Move BestMove = Move.NullMove;
	public IEnumerable<Move> BestLine = Enumerable.Empty<Move>();
	public long TimeSearchedMs = 0;
	public int BestScore = Constants.MinEval;
	private readonly int WorstEvalForBot;
	public int MaxDepthSearched = 0;
	public uint NodesSearched = 0;

	public BasicSearch(Board board, List<Move> customOrderedMoves)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		customOrdered = customOrderedMoves;
		// tt = new(board, (uint) Math.Floor((double) (TTSizeMB*1000000)/Marshal.SizeOf<Entry>()));
		WorstEvalForBot = board.IsWhiteToMove && isWhite ? Constants.MinEval : Constants.MaxEval;
	}

	void GetOrderedLegalMoves(ref Span<Move> moveSpan, int depthFromRoot, bool capturesOnly = false)
	{
		board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly: capturesOnly);

		MoveOrdering.OrderMoves(board, ref moveSpan, !capturesOnly, depthFromRoot);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool MovePlayedWasInterestingMove(Move move)
	{
		return board.IsInCheck();
	}

	public Move Execute(int depth, ref CombinationTTable tt)
	{
		long startTimeTicks = DateTime.Now.Ticks;

		bool inCheck = board.IsInCheck();

		uint nodesSearched = 0;
		int maxDepthSearched = 0;

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
		int numActionMovesSeen = 0;

		for (int i = 0; i < moves.Length; i++)
		{			
			Move move = moves[i];

			currLine.Fill(Move.NullMove);

			if (KillSearch) break;

			nodesSearched++;

			board.MakeMove(move);

			bool moveWasACheck = board.IsInCheck();
			bool isQuietMove = !(move.IsCapture || move.IsPromotion || moveWasACheck);

			if (!isQuietMove) numActionMovesSeen++;

			if (!madeQuietMove && isQuietMove) madeQuietMove = true;

			int extension = 0; // (int) (MovePlayedWasInterestingMove(move) ? 1 : 0);

			int score;
			
			if (foundPV && !inCheck && depth >= 3) // NOTE: extension == 0 is currently a placeholder for threat detection
			{
				score = -NegaMax(Math.Min(PVSDepth, depth-3), -Constants.MinEval-1, -Constants.MinEval, 1, ExtensionCap, ref currLine, false, tt, ref nodesSearched, ref maxDepthSearched);
			
				currLine.Fill(Move.NullMove);

				if ((score > Constants.MinEval) && (score < Constants.MaxEval))
				{
					score = -NegaMax(depth-1+extension, -Constants.MaxEval, -Constants.MinEval, 1, extension, ref currLine, i >= 8, tt, ref nodesSearched, ref maxDepthSearched);
				}
			}
			else // Normal AlphaBeta NegaMax
			{
				score = -NegaMax(depth-1+extension, -Constants.MaxEval, -Constants.MinEval, 1, extension, ref currLine, i >= 8, tt, ref nodesSearched, ref maxDepthSearched);
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

		BestLine = BestLine.TakeWhile((move) => move != Move.NullMove);
		BestMove = bestMove;
		BestScore = bestScore;

		long timeSearchedMs = (DateTime.Now.Ticks - startTimeTicks) / TimeSpan.TicksPerMillisecond;

		TimeSearchedMs = timeSearchedMs;

		NodesSearched = nodesSearched;
		MaxDepthSearched = maxDepthSearched;

		if (!KillSearch)
		{
			string scoreString = Evaluator.IsMateScore(bestScore) ? $"mate {Evaluator.ExtractMateInNMoves(bestScore)}" : $"cp {bestScore}";
			string lineString = string.Join(' ', BestLine.Select(MoveUtils.GetUCI));

			Console.WriteLine($"info depth {depth} seldepth {MaxDepthSearched} time {timeSearchedMs} nodes {NodesSearched} score {scoreString} pv {lineString}");
		}

		return bestMove;
	}

	int NegaMaxQuiesce(int alpha, int beta, int depthFromRoot, ref uint nodesSearched, ref int maxDepthSearched, int maxDepth = Constants.MaxEval)
	{
		if (maxDepth == 0) return Evaluator.EvalPositionWithPerspective(board);

		if (depthFromRoot > maxDepthSearched) maxDepthSearched = depthFromRoot;
		
		// if commented out, reduces runtime (recongized depth of 9 search -(200)ms), check if this leads to bad play
		if (board.IsDraw()) return Constants.DrawValue;
		if (board.IsInCheckmate()) return Evaluator.MatedIn(depthFromRoot);

		if (KillSearch) return WorstEvalForBot;

		int eval = Evaluator.EvalPositionWithPerspective(board);

		if (eval >= beta) return beta;

		if (eval > alpha) alpha = eval;
		
		Span<Move> moves = stackalloc Move[CAPTURE_MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot, capturesOnly: true);

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			nodesSearched++;

			board.MakeMove(move);

			eval = -NegaMaxQuiesce(-beta, -alpha, depthFromRoot+1, ref nodesSearched, ref maxDepthSearched, maxDepth-1);

			board.UndoMove(move);

			if (eval >= beta) return beta;

			if (eval > alpha)
			{
				alpha = eval;
			}
		}

		return alpha;
	}

	public int NegaMax(int depth, int alpha, int beta, int depthFromRoot, int numExtensions, ref Span<Move> currLineBuilder, bool nullMovePruningEnabled, in CombinationTTable tt, ref uint nodesSearched, ref int maxDepthSearched)
	{
		if (depthFromRoot > maxDepthSearched) maxDepthSearched = depthFromRoot;

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

		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot, ref nodesSearched, ref maxDepthSearched);

		bool inCheck = board.IsInCheck();
		int eval = 0;

		Span<Move> throwaway = stackalloc Move[MAX_LINE_SIZE];

		// Null move pruning -- removed for now due to occasional blunders
		if (!inCheck && (depth >= NULL_MOVE_PRUNE_DEPTH) && nullMovePruningEnabled)
		{
			board.MakeMove(Move.NullMove);			
			
			eval = -NegaMax(depth-NULL_MOVE_PRUNE_DEPTH, -beta, 1-beta, depthFromRoot+1, ExtensionCap, ref throwaway, false, tt, ref nodesSearched, ref maxDepthSearched);

			board.UndoMove(Move.NullMove);
			
			if ((eval >= beta) && !(Evaluator.IsMateScore(eval) && Evaluator.ExtractMateInNMoves(eval) > 0))
			{
				return beta;
			}
		}

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot);

		if (moves.Length == 0)
		{
			if (inCheck)
			{
				return Evaluator.MatedIn(depthFromRoot);
			}

			return Constants.DrawValue; // stalemate
		}

		Span<Move> currLine = stackalloc Move[MAX_LINE_SIZE];
		currLine.Fill(Move.NullMove);

		bool foundPV = false;
		bool alreadyMadeQuietMove = false;

		int evalBound = TranspositionTable.UpperBound;
		Move bestMove = moves[0];

		bool canSacrificePrune = true;
		int numActionMovesSeen = 0;

		int i = -1;

		ref var move = ref MemoryMarshal.GetReference(moves);
		ref var end = ref Unsafe.Add(ref move, moves.Length);

		while (Unsafe.IsAddressLessThan(ref move, ref end))
		{
			i++;
			currLine.Fill(Move.NullMove);
			canSacrificePrune = !canSacrificePrune;

			nodesSearched++;
			
			board.MakeMove(move);

			bool moveWasACheck = board.IsInCheck();
			bool isQuietMove = !(move.IsCapture || move.IsPromotion || moveWasACheck);

			if (!isQuietMove) numActionMovesSeen++; // try beat depth 10 -> 6549158 nodes

			// futility pruning -- it is depth 1
			if (!inCheck && !foundPV && depth <= FUTUILITY_PRUNE_DEPTH && alreadyMadeQuietMove && isQuietMove)
			{
				board.UndoMove(move);
				
				move = ref Unsafe.Add(ref move, 1);
				continue;
			}

			int alphaRes;
			bool sacrificePruned = false;

			// custom pruning method -- sacrificial pruning -- get below 1305771 depth 9 search
			if (!foundPV && (depth <= 3) && ((alpha-(alphaRes = NegaMaxQuiesce(-beta, -alpha, depthFromRoot, ref nodesSearched, ref maxDepthSearched))) >= 300))
			{
				// huge game sacrifice without sufficient depth to search it through, just use quiesece result
				sacrificePruned = true;
				eval = alphaRes;

				// move = ref Unsafe.Add(ref move, 1);				
				// continue;
			}

			if (!alreadyMadeQuietMove && isQuietMove) alreadyMadeQuietMove = true;

			int extension = 0; // FOR NOW, EXTENSIONS ARE DISABLED - LEADS TO LESS NODE SEARCHES

			// PVS

			if (!sacrificePruned)
			{
				if (foundPV && !inCheck && (depth >= 3)) // NOTE: extension == 0 is currently a placeholder for threat detection
				{
					eval = -NegaMax(Math.Min(PVSDepth, depth-3), -alpha-1, -alpha, depthFromRoot+1, ExtensionCap, ref currLine, false, tt, ref nodesSearched, ref maxDepthSearched);
				
					currLine.Fill(Move.NullMove);

					if ((eval > alpha) && (eval < beta))
					{
						eval = -NegaMax(depth-1+extension, -beta, -alpha, depthFromRoot+1, numExtensions + extension, ref currLine, (i >= 8) && nullMovePruningEnabled, tt, ref nodesSearched, ref maxDepthSearched);
					}
				}
				else // Normal AlphaBeta NegaMax
				{
					eval = -NegaMax(depth-1+extension, -beta, -alpha, depthFromRoot+1, numExtensions + extension, ref currLine, (i >= 8) && nullMovePruningEnabled, tt, ref nodesSearched, ref maxDepthSearched);
				}
			}

			board.UndoMove(move);

			if (eval >= beta)
			{
				tt.StoreEvaluation(depth, beta, TranspositionTable.LowerBound, move);
				
				if (!(move.IsCapture || move.IsPromotion))
				{
					if (depthFromRoot < MoveOrdering.MaxKillerMovePly)
					{					
						if (board.IsWhiteToMove) // gameplay switched when popping moves so the prev move played was actually black's
						{
							MoveOrdering.blackSearchHistory[depthFromRoot, move.StartSquare.Index, move.TargetSquare.Index]+=depth*depth;
						
							Move push = MoveOrdering.blackKillerMoves[depthFromRoot, 0];

							MoveOrdering.blackKillerMoves[depthFromRoot, 1] = push;
							MoveOrdering.blackKillerMoves[depthFromRoot, 0] = move;
						}
						else
						{
							MoveOrdering.whiteSearchHistory[depthFromRoot, move.StartSquare.Index, move.TargetSquare.Index]+=depth*depth;
						
							Move push = MoveOrdering.whiteKillerMoves[depthFromRoot, 0];

							MoveOrdering.whiteKillerMoves[depthFromRoot, 1] = push;
							MoveOrdering.whiteKillerMoves[depthFromRoot, 0] = move;
						}
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
			}

			move = ref Unsafe.Add(ref move, 1);
		}

		if (depthFromRoot < MAX_LINE_SIZE) currLineBuilder[depthFromRoot] = bestMove;

		tt.StoreEvaluation(depth, alpha, evalBound, bestMove);

		return alpha;
	}
}