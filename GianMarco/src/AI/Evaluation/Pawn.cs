using System.Numerics;
using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Pawn;

public static class PawnEval
{
	const short PassedPawnBonus = 40;
	const short StackedPawnPenalty = 20;
	const short IsolatedPawnPenalty = 25;

	static readonly short[] WhitePawnRankBonus = {
		0, 0, 0, 0, 0, 0, 0, 0, // first rank only needed to keep indexes correct, white pawns cannot be here
		0, 0, 0, 0, 0, 0, 0, 0,
		5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 5,
		10, 10, 10, 10, 10, 10, 10, 10,
		10, 10, 10, 10, 10, 10, 10, 10,
		20, 20, 20, 20, 20, 20, 20, 20,
		// 30, 30, 30, 30, 30, 30, 30, 30, // last rank not needed, white pawns promote here
	};

	static readonly short[] BlackPawnRankBonus = {
		// 30, 30, 30, 30, 30, 30, 30, 30, // first rank not needed, black pawns promote here
		20, 20, 20, 20, 20, 20, 20, 20,
		10, 10, 10, 10, 10, 10, 10, 10,
		10, 10, 10, 10, 10, 10, 10, 10,
		5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 5,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, // last rank only needed to keep indexes correct, black pawns cannot be here
	};

	public const ulong FileBitBoard = 0x0101010101010101;

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	static ulong GetFrontViewMask(Square square, bool white)
	{
		ulong fileMask = GetPawnSurroundingMask(square);
		short shifter = (short) (8 * (square.Rank+1));
		ulong frontMask = white ? (ulong.MaxValue << shifter) : (ulong.MaxValue >> shifter);

		return frontMask & fileMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ulong GetPawnSurroundingMask(Square square)
	{
		return
			(FileBitBoard << square.File) |
			(FileBitBoard << Math.Max(0, square.File-1)) |
			(FileBitBoard << Math.Min(7, square.File+1));
	}

	static short EvaluatePassedPawnsForColor(Board board, bool white)
	{
		short score = 0;

		ulong enemyPawns = board.GetPieceBitboard(PieceType.Pawn, !white);

		PieceList pawns = board.GetPieceList(PieceType.Pawn, white);

		foreach (Piece pawn in pawns)
		{
			ulong frontViewMask = GetFrontViewMask(pawn.Square, white);

			if ((enemyPawns & frontViewMask) == 0) // if there are no enemy pawns in the front view, this is a passed pawn
				score+=PassedPawnBonus;
		}

		return score;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short EvaluatePassedPawns(Board board)
	{
		return (short) (EvaluatePassedPawnsForColor(board, true)-EvaluatePassedPawnsForColor(board, false));
	}

	static short EvaluateStackedPawnsForColor(Board board, bool white)
	{
		short penalty = 0;
		
		ulong friendlyPawnBitboard = board.GetPieceBitboard(PieceType.Pawn, white);

		PieceList pawns = board.GetPieceList(PieceType.Pawn, white);

		foreach (Piece pawn in pawns)
		{
			ulong fileMask = FileBitBoard << pawn.Square.File;

			if (BitOperations.PopCount(friendlyPawnBitboard & fileMask) == 1) continue; // if there are no OTHER friendly pawns in the same file, it is not stacked; continue;
	
			penalty+=StackedPawnPenalty;
		}

		return penalty;
	}

	/// <summary>
	///  
	/// </summary>
	/// <returns>the penalty value of both players combined into a single value (this value must be SUBTRACTED from the final eval)</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short EvaluateStackedPawnPenalty(Board board)
	{
		return (short) (EvaluateStackedPawnsForColor(board, true)-EvaluateStackedPawnsForColor(board, false));
	}

	static short EvaluateIsolatedPawnsForColor(Board board, bool white)
	{
		short penalty = 0;

		ulong friendlyPawnBitboard = board.GetPieceBitboard(PieceType.Pawn, white);

		PieceList pawns = board.GetPieceList(PieceType.Pawn, white);

		foreach (Piece pawn in pawns)
		{
			ulong surroundingMask = GetPawnSurroundingMask(pawn.Square);

			if (BitOperations.PopCount(friendlyPawnBitboard & surroundingMask) == 1) // if there are no OTHER friendly pawns in the files surrounding this pawn, it is isolated
				penalty+=IsolatedPawnPenalty;
		}

		return penalty;		
	}

	/// <summary>
	///  
	/// </summary>
	/// <returns>the penalty value of both players combined into a single value (this value must be SUBTRACTED from the final eval)</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short EvaluateIsolatedPawnPenalty(Board board)
	{
		return (short) (EvaluateIsolatedPawnsForColor(board, true)-EvaluateIsolatedPawnsForColor(board, false));
	}

	static short EvaluatePushedPawns(Board board)
	{
		short score = 0;

		foreach (var whitePawn in board.GetPieceList(PieceType.Pawn, true))
			score+=WhitePawnRankBonus[whitePawn.Square.Index];

		foreach (var blackPawn in board.GetPieceList(PieceType.Pawn, false))
			score+=BlackPawnRankBonus[blackPawn.Square.Index];

		return score;
	}

	public static short Evaluate(Board board)
	{
		short score = EvaluatePushedPawns(board);

		score-=EvaluateIsolatedPawnPenalty(board); // note the subtraction
		score-=EvaluateStackedPawnPenalty(board); // note the subtraction
		score+=EvaluatePassedPawns(board);

		return score;
	}
}