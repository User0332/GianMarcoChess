using System.Numerics;
using ChessChallenge.API;

namespace GianMarco.Evaluation;

public static class Pawn
{
	const int PassedPawnBonus = 40;
	const int StackedPawnPenalty = 15;
	const int IsolatedPawnPenalty = 20;

	static readonly int[] PawnPositionBonus = {
		50, 50, 50, 50, 50, 50, 50, 50,  // promotion (kept for index alignment)
		15, 15, 15, 15, 15, 15, 15, 15,
		 7, 10, 10, 10, 10, 10, 10,  7,
		 5,  7,  7,  7,  7,  7,  7,  5,
		 3,  5,  5,  5,  5,  5,  5,  3,
		 1,  3,  3,  3,  3,  3,  3,  1,
		 0,  0,  0,  0,  0,  0,  0,  0, // black starting rank
		 0,  0,  0,  0,  0,  0,  0,  0, // black cannot be here
	};

	const ulong FileBitBoard = 0x0101010101010101;

	static ulong GetFrontViewMask(Square square, bool white)
	{
		ulong fileMask = GetPawnSurroundingMask(square);

		int shifter;

		if (white)
		{
			shifter = (square.Rank+1) << 3; // << 3 same as * 8
		}
		else
		{
			shifter = (7-square.Rank+1) << 3;
		}

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

	public static int EvaluatePassedPawnsForColor(ulong friendlyPawns, ulong enemyPawns, bool white)
	{
		int score = 0;

		while (friendlyPawns != 0) // iterate through all friendly pawns using bitboard operations
		{
			int pawnIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref friendlyPawns);

			ulong frontViewMask = GetFrontViewMask(new(pawnIndex), white);

			if ((enemyPawns & frontViewMask) == 0) // if there are no enemy pawns in the front view, this is a passed pawn
			{
				score+=PassedPawnBonus;
			}
		}

		return score;
	}

	static int EvaluatePassedPawns(ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluatePassedPawnsForColor(whitePawnBitboard, blackPawnBitboard, true)-EvaluatePassedPawnsForColor(blackPawnBitboard, whitePawnBitboard, false);
	}

	// TODO: only use bitboards to calculate this
	public static int EvaluateStackedPawnsForColor(ulong friendlyPawnBitboard)
	{
		int penalty = 0;

		for (int i = 0; i < 7; i++)
		{
			int pawnsInFile = BitOperations.PopCount(friendlyPawnBitboard & (FileBitBoard << i));

			if (pawnsInFile > 1) // if there are more than one friendly pawns in the same file, they are stacked
			{
				penalty+=(pawnsInFile-1)*StackedPawnPenalty; // the first pawn in the file is not penalized, but each additional pawn is penalized
			}
		}

		return penalty;
	}

	/// <summary>
	///
	/// </summary>
	/// <returns>the penalty value of both players combined into a single value (this value must be SUBTRACTED from the final eval)</returns>
	static int EvaluateStackedPawnPenalty(ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluateStackedPawnsForColor(whitePawnBitboard)-EvaluateStackedPawnsForColor(blackPawnBitboard);
	}

	public static int EvaluateIsolatedPawnsForColor(ulong friendlyPawnBitboard)
	{
		int penalty = 0;

		ulong friendlyPawnsCopy = friendlyPawnBitboard;

		while (friendlyPawnBitboard != 0) // iterate through all friendly pawns using bitboard operations
		{
			int pawnIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref friendlyPawnBitboard);

			Square pawnSquare = new(pawnIndex);

			ulong surroundingMask = GetPawnSurroundingMask(pawnSquare);

			surroundingMask &= ~(FileBitBoard << pawnSquare.File); // remove the file of the pawn itself from the surrounding mask

			if (BitOperations.PopCount(friendlyPawnsCopy & surroundingMask) == 0) // if there are no friendly pawns in the files surrounding this pawn, it is isolated
			{
				penalty+=IsolatedPawnPenalty;
			}
		}

		return penalty;
	}

	/// <summary>
	///
	/// </summary>
	/// <returns>the penalty value of both players combined into a single value (this value must be SUBTRACTED from the final eval)</returns>
	static int EvaluateIsolatedPawnPenalty(ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluateIsolatedPawnsForColor(whitePawnBitboard)-EvaluateIsolatedPawnsForColor(blackPawnBitboard);
	}

	static int EvaluatePushedPawnsForColor(ulong friendlyPawnBitboard, bool white)
	{
		int score = 0;

		while (friendlyPawnBitboard != 0) // iterate through all friendly pawns using bitboard operations
		{
			int pawnIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref friendlyPawnBitboard);

			score+=PSTHelper.GetPSTValue(PawnPositionBonus, pawnIndex, white);
		}

		return score;
	}

	public static int EvaluatePushedPawns(ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluatePushedPawnsForColor(whitePawnBitboard, true)-EvaluatePushedPawnsForColor(blackPawnBitboard, false);
	}

	public static int Evaluate(Board board)
	{
		ulong whitePawnBitboard = board.board.pieceBitboards[ChessChallenge.Chess.PieceHelper.WhitePawn];
		ulong blackPawnBitboard = board.board.pieceBitboards[ChessChallenge.Chess.PieceHelper.BlackPawn];

		int score = EvaluatePushedPawns(whitePawnBitboard, blackPawnBitboard);

		score-=EvaluateIsolatedPawnPenalty(whitePawnBitboard, blackPawnBitboard); // note the subtraction
		score-=EvaluateStackedPawnPenalty(whitePawnBitboard, blackPawnBitboard); // note the subtraction
		score+=EvaluatePassedPawns(whitePawnBitboard, blackPawnBitboard);

		return score;
	}
}