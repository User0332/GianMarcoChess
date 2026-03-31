using System.Runtime.CompilerServices;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;
using GianMarco.Search.Utils.MoveOrdering;
using GianMarco.TranspositionTable;

namespace GianMarco.Search;

public sealed class BasicSearch(Board board, int projectedMaxDepth)
{
	// to solve the puzzle, disable LMR and reverse futility pruning
	// nevermind, we can reach depth 14 not bad (each depth < 7000ms) with a good eval and everything enabled, so let's keep everything and work on the endgame evals instead
	// + as a bonus, e7e4 (best move) is listed by the engine as best up until depth 8, and then it comes back at around depth 13
	const int MoveStackallocAmount = 218;
	const int TTDepthGraceThreshold = 0;

	const bool NullMovePruningEnabled = true;
	const int NullMoveMinimumDepth = 3;
	const int NullMoveDepthReduction = 3;
	const int NullMoveMinimumReducedDepth = 0;

	const bool LateMoveReductionEnabled = true;
	const int LateMoveIndexThreshold = 3;
	const int LateMoveMinimumDepth = 3;
	const int LateMoveNonEndgameDepthReduction = 2;
	const int LateMoveEndgameDepthReduction = 3;
	const int LateMoveMinimumReducedDepth = 0;

	const bool FutilityPruningEnabled = true;
	const int FutilityPruningMaxDepth = 1;
	const int FutilityPruningMargin = 200;

	const bool MainSearchSEEPruningEnabled = true;
	const int MainSearchSEEPruningMaxDepth = 3;

	const bool QSearchSEEPruningEnabled = true;

	const bool DeltaPruningEnabled = true;
	const int DeltaPruningMargin = 150;

	const bool ReverseFutilityPruningEnabled = true;
	const int ReverseFutilityPruningMaxDepth = 1;
	const int ReverseFutilityPruningDepthToMarginMultiplier = 100;

	const bool PVSearchEnabled = true;

	const bool TranspositionTableEnabled = true;

	const int MaxLineSize = 10;

	readonly Board board = board;
	public readonly MoveOrderer MoveOrderer = new(projectedMaxDepth);
	public bool SearchEnded = false;
	public bool KillSearch = false;
	public Move BestMove = Move.NullMove;
	public Move[] BestLine = new Move[MaxLineSize];
	public long TimeSearchedMs = 0;
	public int BestScore = Constants.MinEval;
	public int MaxDepthSearched = 0;
	public uint NodesSearched = 0;

	void GetOrderedLegalMoves(ref Span<Move> moveSpan, int depthFromRoot, ReadOnlySpan<Move> probablePV, bool isEndgame, bool capturesOnly = false)
	{
		board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly: capturesOnly);

		Move shouldBeFirst = probablePV.Length > 0 ? probablePV[0] : Move.NullMove;

		MoveOrderer.OrderMoves(
			board,
			ref moveSpan,
			shouldBeFirst,
			inNormalSearch: !capturesOnly,
			depthFromRoot,
			isEndgame
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool MovePlayedWasInterestingMove(Move move, bool moveWasACheck)
	{
		return moveWasACheck;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ReadOnlySpan<Move> PVNext(ReadOnlySpan<Move> pv)
	{
		if (pv.Length == 0) { return []; }

		return pv[1..];
	}

	public Move Execute(int depth, HeapTranspositionTable tt, ReadOnlySpan<Move> probablePV, int alpha = Constants.MinEval, int beta = Constants.MaxEval)
	{
		long startTimeTicks = DateTime.Now.Ticks;

		bool inCheck = board.IsInCheck();

		uint nodesSearched = 0;
		int maxDepthSearched = 0;

		Span<Move> moves = stackalloc Move[MoveStackallocAmount];

		GetOrderedLegalMoves(ref moves, 0, probablePV, GamePhaseUtils.IsEndgame(board));

		Span<Move> currLine = stackalloc Move[MaxLineSize];
		currLine.Fill(Move.NullMove);

		Move bestMove = moves[0];

		bool madeQuietMove = false;
		int numActionMovesSeen = 0;

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			currLine.Fill(Move.NullMove);

			if (KillSearch) break;

			board.MakeMove(move);

			bool moveWasACheck = board.IsInCheck();
			bool isQuietMove = !(move.IsCapture || move.IsPromotion || moveWasACheck);

			if (!isQuietMove) numActionMovesSeen++;

			if (!madeQuietMove && isQuietMove) madeQuietMove = true;

			int extension = MovePlayedWasInterestingMove(move, moveWasACheck) ? 1 : 0;

			int eval;

			// PVS

			if (!PVSearchEnabled || i == 0) // first move, should be PV
			{
				eval = -NegaMax(depth-1+extension, -beta, -alpha, 1, currLine, tt, ref nodesSearched, ref maxDepthSearched, PVNext(probablePV));
			}
			else
			{
				int newDepth = depth - 1 + extension;

				// late move reduction
				if (LateMoveReductionEnabled && !inCheck && isQuietMove && depth >= LateMoveMinimumDepth && i >= LateMoveIndexThreshold) newDepth = Math.Max(depth - LateMoveNonEndgameDepthReduction, LateMoveMinimumReducedDepth);

				eval = -NegaMax(newDepth, -alpha-1, -alpha, 1, currLine, tt, ref nodesSearched, ref maxDepthSearched, []);

				if ((eval > alpha) && (beta-alpha > 1))
				{
					eval = -NegaMax(depth-1+extension, -beta, -alpha, 1, currLine, tt, ref nodesSearched, ref maxDepthSearched, []);
				}
			}

			board.UndoMove(move);

			if (eval > alpha)
			{
				alpha = eval;
				bestMove = move;

				currLine[0] = move;

				currLine.CopyTo(BestLine);
			}
		}

		SearchEnded = true;

		BestLine = [..BestLine.TakeWhile((move) => move != Move.NullMove)];
		BestMove = bestMove;
		BestScore = alpha;

		long timeSearchedMs = (DateTime.Now.Ticks - startTimeTicks) / TimeSpan.TicksPerMillisecond;

		TimeSearchedMs = timeSearchedMs;

		NodesSearched = nodesSearched;
		MaxDepthSearched = maxDepthSearched;

		if (!KillSearch)
		{
			string scoreString = Evaluator.IsMateScore(alpha) ? $"mate {Evaluator.ExtractMateInNMoves(alpha)}" : $"cp {alpha}";
			string lineString = string.Join(' ', BestLine.Select(MoveUtils.GetUCI));

			Console.WriteLine($"info depth {depth} seldepth {MaxDepthSearched} time {timeSearchedMs} nodes {NodesSearched} score {scoreString} pv {lineString}");
		}

		return bestMove;
	}

	int NegaMaxQuiesce(int alpha, int beta, int depthFromRoot, ref uint nodesSearched, ref int maxDepthSearched, int maxDepth = Constants.MaxEval)
	{
		bool isEndgame = GamePhaseUtils.IsEndgame(board);
		int LateMoveDepthReduction = GamePhaseUtils.IsEndgame(board) ? LateMoveEndgameDepthReduction : LateMoveNonEndgameDepthReduction;

		if (maxDepth == 0) return Evaluator.EvalPositionWithPerspective(board, isEndgame);

		nodesSearched++;

		if (depthFromRoot > maxDepthSearched) maxDepthSearched = depthFromRoot;

		// if commented out, reduces runtime (recongized depth of 9 search -(200)ms), check if this leads to bad play
		if (board.IsDraw()) return Constants.DrawValue;
		if (board.IsInCheckmate()) return Evaluator.MateIn(depthFromRoot);

		if (KillSearch) return Constants.MinEval;

		int standPat = Evaluator.EvalPositionWithPerspective(board,isEndgame);
		int eval = standPat;

		if (eval >= beta)
		{
			return beta;
		}

		if (eval > alpha) alpha = eval;

		Span<Move> moves = stackalloc Move[MoveStackallocAmount];

		// TODO: modify the generator to be able to generate checks, captures, and promotions only instead of generating everything and then filtering out the quiet moves
		GetOrderedLegalMoves(ref moves, depthFromRoot, [], isEndgame, capturesOnly: false);

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			// SEE pruning
			if (QSearchSEEPruningEnabled && move.IsCapture && StaticExchangeEvaluation.IsLosingCapture(board, move)) continue;

			// Delta pruning
			if (DeltaPruningEnabled && move.IsCapture)
			{
				int capturedValue = Material.GetPieceValue(move.CapturePieceType);

				if (standPat + capturedValue + DeltaPruningMargin < alpha)
				{
					continue;
				}
			}

			board.MakeMove(move);

			if (!move.IsCapture && !move.IsPromotion && !board.IsInCheck())
			{
				board.UndoMove(move);

				continue; // if this move is not a capture and does not give check, skip it in quiescence search
			}

			eval = -NegaMaxQuiesce(-beta, -alpha, depthFromRoot+1, ref nodesSearched, ref maxDepthSearched, maxDepth-1);

			board.UndoMove(move);

			if (eval >= beta)
			{
				return beta;
			}

			if (eval > alpha)
			{
				alpha = eval;
			}
		}

		return alpha;
	}

	public int NegaMax(int depth, int alpha, int beta, int depthFromRoot, Span<Move> currLineBuilder, HeapTranspositionTable tt, ref uint nodesSearched, ref int maxDepthSearched, ReadOnlySpan<Move> probablePV)
	{
		bool isEndgame = GamePhaseUtils.IsEndgame(board);
		int LateMoveDepthReduction = GamePhaseUtils.IsEndgame(board) ? LateMoveEndgameDepthReduction : LateMoveNonEndgameDepthReduction;

		if (depthFromRoot > maxDepthSearched) maxDepthSearched = depthFromRoot;

		if (KillSearch)
		{
			return Constants.MinEval;
		}

		if (TranspositionTableEnabled)
		{
			int lookupVal = tt.LookupEval(depth-TTDepthGraceThreshold, alpha, beta);

			if (lookupVal != HeapTranspositionTable.LookupFailed)
			{
				return lookupVal;
			}
		}

		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot, ref nodesSearched, ref maxDepthSearched);

		nodesSearched++;

		bool inCheck = board.IsInCheck();
		bool isPVNode = probablePV.Length > 0;
		int standPat = Evaluator.EvalPositionWithPerspective(board, isEndgame);
		int eval = 0;

		if (ReverseFutilityPruningEnabled && !isPVNode && !inCheck && depth <= ReverseFutilityPruningMaxDepth)
		{
			if (standPat - (depth*ReverseFutilityPruningDepthToMarginMultiplier) >= beta)
			{
				return beta;
			}
		}

		// Null move reduction
		if (NullMovePruningEnabled && !inCheck && !GamePhaseUtils.ZugzwangLikely(board) && depth >= NullMoveMinimumDepth)
		{
			Span<Move> throwaway = stackalloc Move[MaxLineSize];

			board.MakeMove(Move.NullMove);

			var reducedDepth = Math.Max(depth - NullMoveDepthReduction, NullMoveMinimumReducedDepth);

			eval = -NegaMax(reducedDepth, -beta, 1-beta, depthFromRoot+1, throwaway, tt, ref nodesSearched, ref maxDepthSearched, []);

			board.UndoMove(Move.NullMove);

			if (eval >= beta)
			{
				if (TranspositionTableEnabled)
				{
					tt.StoreEvaluation(depth, beta, HeapTranspositionTable.LowerBound);
				}

				return beta;
			}
		}

		if (board.IsRepeatedPosition() || board.IsFiftyMoveDraw() || board.IsInsufficientMaterial())
		{
			return Constants.DrawValue;
		}

		Span<Move> moves = stackalloc Move[MoveStackallocAmount];

		GetOrderedLegalMoves(ref moves, depthFromRoot, probablePV, isEndgame);

		if (moves.Length == 0)
		{
			if (inCheck)
			{
				return Evaluator.MateIn(depthFromRoot);
			}

			return Constants.DrawValue; // stalemate
		}

		Span<Move> currLine = stackalloc Move[MaxLineSize];
		currLine.Fill(Move.NullMove);

		int evalBound = HeapTranspositionTable.UpperBound;
		Move bestMove = moves[0];

		int numActionMovesSeen = 0;

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			currLine.Fill(Move.NullMove);

			int seeScore = StaticExchangeEvaluation.EvaluateMove(board, move);

			board.MakeMove(move);

			bool moveWasACheck = board.IsInCheck();
			bool isQuietMove = !(move.IsCapture || move.IsPromotion || moveWasACheck);

			if (!isQuietMove) numActionMovesSeen++;

			int extension = MovePlayedWasInterestingMove(move, moveWasACheck) ? 1 : 0;

			// futility pruning
			if (FutilityPruningEnabled && i != 0 && !inCheck && isQuietMove && depth <= FutilityPruningMaxDepth)
			{
				if (standPat + FutilityPruningMargin < alpha)
				{
					board.UndoMove(move);

					continue;
				}
			}

			// SEE pruning
			if (MainSearchSEEPruningEnabled && i != 0 && !moveWasACheck && !inCheck && depth < MainSearchSEEPruningMaxDepth && move.IsCapture && !move.IsPromotion && !move.IsEnPassant)
			{
				if (seeScore < 0)
				{
					board.UndoMove(move);

					continue;
				}
			}

			// PVS

			if (!PVSearchEnabled || i == 0) // first move, should be PV
			{
				eval = -NegaMax(depth-1+extension, -beta, -alpha, depthFromRoot+1, currLine, tt, ref nodesSearched, ref maxDepthSearched, PVNext(probablePV));
			}
			else
			{
				int newDepth = depth - 1 + extension;

				// late move reduction
				if (LateMoveReductionEnabled && !inCheck && isQuietMove && depth >= LateMoveMinimumDepth && i >= LateMoveIndexThreshold) newDepth = Math.Max(depth - LateMoveDepthReduction, LateMoveMinimumReducedDepth);

				eval = -NegaMax(newDepth, -alpha-1, -alpha, depthFromRoot+1, currLine, tt, ref nodesSearched, ref maxDepthSearched, []);

				if ((eval > alpha) && (beta-alpha > 1)) // this move looks good, research it more
				{
					eval = -NegaMax(depth-1+extension, -beta, -alpha, depthFromRoot+1, currLine, tt, ref nodesSearched, ref maxDepthSearched, []);
				}
			}

			board.UndoMove(move);

			if (eval >= beta)
			{
				if (TranspositionTableEnabled)
				{
					tt.StoreEvaluation(depth, beta, HeapTranspositionTable.LowerBound);
				}

				if (!move.IsCapture && !move.IsPromotion)
				{
					if (depthFromRoot < MoveOrderer.MaxKillerMovePly)
					{
						if (board.IsWhiteToMove)
						{
							MoveOrderer.whiteSearchHistory[move.StartSquare.Index, move.TargetSquare.Index]+= depth * depth;

							Move push = MoveOrderer.whiteKillerMoves[depthFromRoot, 0];

							MoveOrderer.whiteKillerMoves[depthFromRoot, 1] = push;
							MoveOrderer.whiteKillerMoves[depthFromRoot, 0] = move;
						}
						else
						{
							MoveOrderer.blackSearchHistory[move.StartSquare.Index, move.TargetSquare.Index]+= depth * depth;

							Move push = MoveOrderer.blackKillerMoves[depthFromRoot, 0];

							MoveOrderer.blackKillerMoves[depthFromRoot, 1] = push;
							MoveOrderer.blackKillerMoves[depthFromRoot, 0] = move;
						}
					}
				}

				return beta;
			}

			if (eval > alpha)
			{
				evalBound = HeapTranspositionTable.Exact;
				bestMove = move;

				alpha = eval;

				currLine.CopyTo(currLineBuilder);
			}
		}

		if (depthFromRoot < MaxLineSize) currLineBuilder[depthFromRoot] = bestMove;

		if (TranspositionTableEnabled)
		{
			tt.StoreEvaluation(depth, alpha, evalBound);
		}

		return alpha;
	}
}