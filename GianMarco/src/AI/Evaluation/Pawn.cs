using System.Numerics;
using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Pawn;

public static class PawnEval
{
	const int PassedPawnBonus = 40;
	const int StackedPawnPenalty = 20;
	const int IsolatedPawnPenalty = 25;

	static readonly int[] WhitePawnPositionBonus = {
		0,  0,  0,  0,  0,  0,  0,  0,  // rank 1 (white cannot be here)
		0,  0,  0,  0,  0,  0,  0,  0,  // rank 2 (starting position)
		1,  3,  5, 10, 10,  5,  3,  1,  // rank 3 (diffusing from center)
		3,  8, 12, 15, 15, 12,  8,  3,  // rank 4
		5, 12, 18, 25, 25, 18, 12,  5,  // rank 5
		10, 20, 28, 35, 35, 28, 20, 10,  // rank 6
		15, 25, 35, 40, 40, 35, 25, 15,  // rank 7 (promotion zone)
		50, 50, 50, 50, 50, 50, 50, 50,  // rank 8 (promotion)
	};

	static readonly int[] BlackPawnPositionBonus = {
		50, 50, 50, 50, 50, 50, 50, 50,  // rank 1 (promotion)
		15, 25, 35, 40, 40, 35, 25, 15,  // rank 2 (promotion zone)
		10, 20, 28, 35, 35, 28, 20, 10,  // rank 3
		5, 12, 18, 25, 25, 18, 12,  5,   // rank 4
		3,  8, 12, 15, 15, 12,  8,  3,   // rank 5
		1,  3,  5, 10, 10,  5,  3,  1,   // rank 6 (diffusing from center)
		0,  0,  0,  0,  0,  0,  0,  0,   // rank 7 (starting position)
		0,  0,  0,  0,  0,  0,  0,  0,   // rank 8 (black cannot be here)
	};

	public const ulong FileBitBoard = 0x0101010101010101;

	static ulong GetFrontViewMask(Square square, bool white)
	{
		ulong fileMask = GetPawnSurroundingMask(square);

		int shifter;

		if (white)
			shifter = (square.Rank+1) << 3; // << 3 same as * 8
		else
			shifter = (8-square.Rank+1) << 3;

		ulong frontMask = white ? (ulong.MaxValue << shifter) : (ulong.MaxValue >> shifter);

		return frontMask & fileMask;
	}

	static ulong GetPawnSurroundingMask(Square square)
	{
		return
			(FileBitBoard << square.File) |
			(FileBitBoard << Math.Max(0, square.File-1)) |
			(FileBitBoard << Math.Min(7, square.File+1));
	}

	public static int EvaluatePassedPawnsForColor(PieceList pawns, ulong enemyPawns, bool white)
	{
		int score = 0;

		foreach (Piece pawn in pawns)
		{
			ulong frontViewMask = GetFrontViewMask(pawn.Square, white);

			if ((enemyPawns & frontViewMask) == 0) // if there are no enemy pawns in the front view, this is a passed pawn
				score+=PassedPawnBonus;
		}

		return score;
	}

	static int EvaluatePassedPawns(PieceList whitePawns, PieceList blackPawns, ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluatePassedPawnsForColor(whitePawns, blackPawnBitboard, true)-EvaluatePassedPawnsForColor(blackPawns, whitePawnBitboard, false);
	}

	// TODO: only use bitboards to calculate this
	public static int EvaluateStackedPawnsForColor(PieceList pawns, ulong friendlyPawnBitboard)
	{
		int penalty = 0;

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
	static int EvaluateStackedPawnPenalty(PieceList whitePawns, PieceList blackPawns, ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluateStackedPawnsForColor(whitePawns, whitePawnBitboard)-EvaluateStackedPawnsForColor(blackPawns, blackPawnBitboard);
	}

	public static int EvaluateIsolatedPawnsForColor(PieceList pawns, ulong friendlyPawnBitboard)
	{
		int penalty = 0;

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
	static int EvaluateIsolatedPawnPenalty(PieceList whitePawns, PieceList blackPawns, ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluateIsolatedPawnsForColor(whitePawns, whitePawnBitboard)-EvaluateIsolatedPawnsForColor(blackPawns, blackPawnBitboard);
	}

	public static int EvaluatePushedPawns(PieceList whitePawns, PieceList blackPawns)
	{
		int score = 0;

		foreach (var whitePawn in whitePawns)
			score+=WhitePawnPositionBonus[whitePawn.Square.Index];

		foreach (var blackPawn in blackPawns)
			score+=BlackPawnPositionBonus[blackPawn.Square.Index];

		return score;
	}

	public static int Evaluate(Board board)
	{
		PieceList whitePawns = board.allPieceLists[ChessChallenge.Chess.PieceHelper.WhitePawn];
		PieceList blackPawns = board.allPieceLists[ChessChallenge.Chess.PieceHelper.BlackPawn];

		ulong whitePawnBitboard = board.board.pieceBitboards[ChessChallenge.Chess.PieceHelper.WhitePawn];
		ulong blackPawnBitboard = board.board.pieceBitboards[ChessChallenge.Chess.PieceHelper.BlackPawn];

		int score = EvaluatePushedPawns(whitePawns, blackPawns);

		score-=EvaluateIsolatedPawnPenalty(whitePawns, blackPawns, whitePawnBitboard, blackPawnBitboard); // note the subtraction
		score-=EvaluateStackedPawnPenalty(whitePawns, blackPawns, whitePawnBitboard, blackPawnBitboard); // note the subtraction
		score+=EvaluatePassedPawns(whitePawns, blackPawns, whitePawnBitboard, blackPawnBitboard);

		return score;
	}
}