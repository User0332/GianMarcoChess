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
		 7, 10, 13, 15, 15, 13, 10,  7,
		 5,  7, 10, 13, 13, 10,  7,  5,
		 3,  5,  7, 10, 10,  7,  5,  3,
		 0,  3,  5,  7,  7,  5,  3,  0,
		 0,  0,  5,  5,  5,  5,  0,  0,
		 0,  0,  0,  0,  0,  0,  0,  0, // black starting rank
		 0,  0,  0,  0,  0,  0,  0,  0, // black cannot be here
	};

	const ulong FileBitBoard = 0x0101010101010101;

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

		for (int i = 0; i < pawns.Count; i++)
		{
			ulong frontViewMask = GetFrontViewMask(pawns[i].Square, white);

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

		for (int i = 0; i < pawns.Count; i++)
		{
			ulong fileMask = FileBitBoard << pawns[i].Square.File;

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

		for (int i = 0; i < pawns.Count; i++)
		{
			ulong surroundingMask = GetPawnSurroundingMask(pawns[i].Square);

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

		for (int i = 0; i < whitePawns.Count; i++)
		{
			score+=PSTHelper.GetPSTValue(PawnPositionBonus, whitePawns[i].Square.Index, true);
		}

		for (int i = 0; i < blackPawns.Count; i++)
		{
			score+=PSTHelper.GetPSTValue(PawnPositionBonus, blackPawns[i].Square.Index, false);
		}

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