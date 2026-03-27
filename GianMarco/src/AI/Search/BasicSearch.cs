using System.Runtime.CompilerServices;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Evaluation.Material;
using GianMarco.Search.Utils;
using GianMarco.TTable;

namespace GianMarco.Search;

sealed class BasicSearch
{
	const int MOVE_STACKALLOC_AMT = 218;
	const int CAPTURE_MOVE_STACKALLOC_AMT = 100;
	const int TTDepthGraceThreshold = 0;
	const int NullMovePruneDepthReduction = 4;
	const int LateMoveDepthReduction = 3;

	public const int MaxLineSize = 10;

	private readonly Board board;
	private readonly bool isWhite;
	public bool SearchEnded = false;
	public bool KillSearch = false;
	public Move BestMove = Move.NullMove;
	public Move[] BestLine = new Move[MaxLineSize];
	public long TimeSearchedMs = 0;
	public int BestScore = Constants.MinEval;
	private readonly int WorstEvalForBot;
	public int MaxDepthSearched = 0;
	public uint NodesSearched = 0;

	public BasicSearch(Board board)
	{
		this.board = board;
		isWhite = board.IsWhiteToMove;
		// tt = new(board, (uint) Math.Floor((double) (TTSizeMB*1000000)/Marshal.SizeOf<Entry>()));
		WorstEvalForBot = board.IsWhiteToMove && isWhite ? Constants.MinEval : Constants.MaxEval;
	}

	void GetOrderedLegalMoves(ref Span<Move> moveSpan, int depthFromRoot, ReadOnlySpan<Move> probablePV, bool capturesOnly = false)
	{
		board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly: capturesOnly);

		Move shouldBeFirst = probablePV.Length > 0 ? probablePV[0] : Move.NullMove;

		MoveOrdering.OrderMoves(
			board,
			ref moveSpan,
			shouldBeFirst,
			inNormalSearch: !capturesOnly,
			depthFromRoot
		);
	}


	bool MovePlayedWasInterestingMove(Move move)
	{
		return board.IsInCheck();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ReadOnlySpan<Move> PVNext(ReadOnlySpan<Move> pv)
	{
		if (pv.Length == 0) { return []; }

		return pv[1..];
	}

	public Move Execute(int depth, TranspositionTable tt, ReadOnlySpan<Move> probablePV)
	{
		long startTimeTicks = DateTime.Now.Ticks;

		bool inCheck = board.IsInCheck();

		uint nodesSearched = 0;
		int maxDepthSearched = 0;

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, 0, probablePV);

		Span<Move> currLine = stackalloc Move[MaxLineSize];
		currLine.Fill(Move.NullMove);

		int beta = Constants.MaxEval;
		int alpha = Constants.MinEval;

		Move bestMove = moves[0];

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

			int extension = 0; // MovePlayedWasInterestingMove(move) ? 1 : 0; // disable move extensions

			int eval;

			if (!inCheck && i >= 3 && isQuietMove)	// late move reduction
			{
				var reducedDepth = Math.Max(depth - LateMoveDepthReduction, 0);

				eval = -NegaMax(depth-1+extension, -alpha-1, -alpha, 1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);

				if ((eval > alpha) && (beta-alpha > 1)) // this move looks good, research it more
				{
					eval = -NegaMax(depth-1+extension, -beta, -alpha, 1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);
				}
			}
			else
			{
				// PVS

				if (i == 0) // first move, should be PV
				{
					eval = -NegaMax(depth-1+extension, -beta, -alpha, 1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, PVNext(probablePV));
				}
				else
				{
					eval = -NegaMax(depth-1+extension, -alpha-1, -alpha, 1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);

					if ((eval > alpha) && (eval < beta))
					{
						eval = -NegaMax(depth-1+extension, -beta, -alpha, 1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);
					}
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
		if (maxDepth == 0) return Evaluator.EvalPositionWithPerspective(board);

		if (depthFromRoot > maxDepthSearched) maxDepthSearched = depthFromRoot;

		// if commented out, reduces runtime (recongized depth of 9 search -(200)ms), check if this leads to bad play
		if (board.IsDraw()) return Constants.DrawValue;
		if (board.IsInCheckmate()) return Evaluator.MateIn(depthFromRoot);

		if (KillSearch) return WorstEvalForBot;

		int eval = Evaluator.EvalPositionWithPerspective(board);

		if (eval >= beta) return beta;

		if (eval > alpha) alpha = eval;

		Span<Move> moves = stackalloc Move[CAPTURE_MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot, [], capturesOnly: true);

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			nodesSearched++;

			// SEE pruning
			if (move.IsCapture && StaticExchangeEvaluation.IsLosingCapture(board, move)) continue;

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

	public int NegaMax(int depth, int alpha, int beta, int depthFromRoot, Span<Move> currLineBuilder, bool nullMovePruningEnabled, TranspositionTable tt, ref uint nodesSearched, ref int maxDepthSearched, ReadOnlySpan<Move> probablePV)
	{
		if (depthFromRoot > maxDepthSearched) maxDepthSearched = depthFromRoot;

		if (KillSearch)
		{
			return WorstEvalForBot;
		}

		int lookupVal = tt.LookupEval(depth-TTDepthGraceThreshold, alpha, beta);

		if (lookupVal != TranspositionTable.LookupFailed)
		{
			return lookupVal;
		}

		if (depth == 0) return NegaMaxQuiesce(alpha, beta, depthFromRoot, ref nodesSearched, ref maxDepthSearched);

		bool inCheck = board.IsInCheck();
		// int currentStaticEval = Evaluator.EvalPositionWithPerspective(board);
		int eval = 0;

		// Null move pruning
		// TODO: check if only pawns exist and if so, do not null move prune because of zugzwang possibilities
		if (!inCheck && nullMovePruningEnabled)
		{
			Span<Move> throwaway = stackalloc Move[MaxLineSize];

			board.MakeMove(Move.NullMove);

			var reducedDepth = Math.Max(depth - NullMovePruneDepthReduction, 0);

			eval = -NegaMax(reducedDepth, -beta, 1-beta, depthFromRoot+1, throwaway, true, tt, ref nodesSearched, ref maxDepthSearched, []);

			board.UndoMove(Move.NullMove);

			if (eval >= beta)
			{
				tt.StoreEvaluation(depth, beta, TranspositionTable.LowerBound);

				return beta;
			}
		}

		if (board.IsRepeatedPosition() || board.IsFiftyMoveDraw() || board.IsInsufficientMaterial())
		{
			return Constants.DrawValue;
		}

		Span<Move> moves = stackalloc Move[MOVE_STACKALLOC_AMT];

		GetOrderedLegalMoves(ref moves, depthFromRoot, probablePV);

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

		int evalBound = TranspositionTable.UpperBound;
		Move bestMove = moves[0];

		int numActionMovesSeen = 0;

		for (int i = 0; i < moves.Length; i++)
		{
			Move move = moves[i];

			currLine.Fill(Move.NullMove);

			nodesSearched++;

			board.MakeMove(move);

			bool moveWasACheck = board.IsInCheck();
			bool isQuietMove = !(move.IsCapture || move.IsPromotion || moveWasACheck);

			if (!isQuietMove) numActionMovesSeen++; // try beat depth 10 -> 6549158 nodes

			int extension = 0; // MovePlayedWasInterestingMove(move) ? 1 : 0; // FOR NOW, EXTENSIONS ARE DISABLED - LEADS TO LESS NODE SEARCHES

			if (!inCheck && i >= 3 && isQuietMove) // late move reduction
			{
				var reducedDepth = Math.Max(depth - LateMoveDepthReduction, 0);

				eval = -NegaMax(reducedDepth, -alpha-1, -alpha, depthFromRoot+1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);

				if ((eval > alpha) && (beta-alpha > 1)) // this move looks good, research it more
				{
					eval = -NegaMax(depth-1+extension, -beta, -alpha, depthFromRoot+1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);
				}
			}
			else
			{
				// PVS

				if (i == 0) // first move, should be PV
				{
					eval = -NegaMax(depth-1+extension, -beta, -alpha, depthFromRoot+1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, PVNext(probablePV));
				}
				else
				{
					eval = -NegaMax(depth-1+extension, -alpha-1, -alpha, depthFromRoot+1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);

					if ((eval > alpha) && (beta-alpha > 1)) // this move looks good, research it more
					{
						eval = -NegaMax(depth-1+extension, -beta, -alpha, depthFromRoot+1, currLine, true, tt, ref nodesSearched, ref maxDepthSearched, []);
					}
				}
			}

			board.UndoMove(move);

			if (eval >= beta)
			{
				tt.StoreEvaluation(depth, beta, TranspositionTable.LowerBound);

				if (!(move.IsCapture || move.IsPromotion))
				{
					if (depthFromRoot < MoveOrdering.MaxKillerMovePly)
					{
						if (board.IsWhiteToMove)
						{
							MoveOrdering.whiteSearchHistory[depthFromRoot, move.StartSquare.Index, move.TargetSquare.Index]+=depth*depth;

							Move push = MoveOrdering.whiteKillerMoves[depthFromRoot, 0];

							MoveOrdering.whiteKillerMoves[depthFromRoot, 1] = push;
							MoveOrdering.whiteKillerMoves[depthFromRoot, 0] = move;
						}
						else
						{
							MoveOrdering.blackSearchHistory[depthFromRoot, move.StartSquare.Index, move.TargetSquare.Index]+=depth*depth;

							Move push = MoveOrdering.blackKillerMoves[depthFromRoot, 0];

							MoveOrdering.blackKillerMoves[depthFromRoot, 1] = push;
							MoveOrdering.blackKillerMoves[depthFromRoot, 0] = move;
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
			}
		}

		if (depthFromRoot < MaxLineSize) currLineBuilder[depthFromRoot] = bestMove;

		tt.StoreEvaluation(depth, alpha, evalBound);

		return alpha;
	}
}