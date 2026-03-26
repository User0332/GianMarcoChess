using System.Numerics;
using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Pawn;

public static class PawnEval
{
	const int PassedPawnBonus = 40;
	const int StackedPawnPenalty = 20;
	const int IsolatedPawnPenalty = 25;

	static readonly byte[] WhitePawnRankBonus = {
		0, 0, 0, 0, 0, 0, 0, 0, // first rank only needed to keep indexes correct, white pawns cannot be here
		0, 0, 0, 0, 0, 0, 0, 0,
		5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 5,
		10, 10, 10, 10, 10, 10, 10, 10,
		10, 10, 10, 10, 10, 10, 10, 10,
		20, 20, 20, 20, 20, 20, 20, 20,
		// 30, 30, 30, 30, 30, 30, 30, 30, // last rank not needed, white pawns promote here
	};

	static readonly byte[] BlackPawnRankBonus = {
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

		int shifter;
		
		if (white)
			shifter = (square.Rank+1) << 3; // << 3 same as * 8
		else
			shifter = (8-square.Rank+1) << 3;

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

	static int EvaluatePassedPawnsForColor(PieceList pawns, ulong enemyPawns, bool white)
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int EvaluatePassedPawns(PieceList whitePawns, PieceList blackPawns, ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluatePassedPawnsForColor(whitePawns, blackPawnBitboard, true)-EvaluatePassedPawnsForColor(blackPawns, whitePawnBitboard, false);
	}

	// TODO: only use bitboards to calculate this
	static int EvaluateStackedPawnsForColor(PieceList pawns, ulong friendlyPawnBitboard)
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int EvaluateStackedPawnPenalty(PieceList whitePawns, PieceList blackPawns, ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluateStackedPawnsForColor(whitePawns, whitePawnBitboard)-EvaluateStackedPawnsForColor(blackPawns, blackPawnBitboard);
	}

	static int EvaluateIsolatedPawnsForColor(PieceList pawns, ulong friendlyPawnBitboard)
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int EvaluateIsolatedPawnPenalty(PieceList whitePawns, PieceList blackPawns, ulong whitePawnBitboard, ulong blackPawnBitboard)
	{
		return EvaluateIsolatedPawnsForColor(whitePawns, whitePawnBitboard)-EvaluateIsolatedPawnsForColor(blackPawns, blackPawnBitboard);
	}

	static int EvaluatePushedPawns(PieceList whitePawns, PieceList blackPawns)
	{
		int score = 0;

		foreach (var whitePawn in whitePawns)
			score+=WhitePawnRankBonus[whitePawn.Square.Index];

		foreach (var blackPawn in blackPawns)
			score+=BlackPawnRankBonus[blackPawn.Square.Index];

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