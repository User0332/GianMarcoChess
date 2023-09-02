using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Outpost;

public static class OutpostEval
{
	static readonly short[] WhiteOutpostRankBonuses = {
		0,
		0,
		0,
		10,
		20,
		25,
		30,
		20
	};

	static readonly short[] BlackOutpostRankBonuses = {
		20,
		30,
		25,
		20,
		10,
		0,
		0,
		0,
	};

	const ulong RankBitBoard = 18374686479671623680;

	static ulong GetFrontSideView(Square square, bool white)
	{
		ulong sidesMask = (Pawn.PawnEval.FileBitBoard << Math.Max(0, square.File-1)) |
			(Pawn.PawnEval.FileBitBoard << Math.Min(7, square.File+1));

		short shifter = (short) (8 * (square.Rank+1));
		ulong frontMask = white ? (ulong.MaxValue << shifter) : (ulong.MaxValue >> shifter);

		return frontMask & sidesMask;
	}

	static ulong GetPawnProtectionMask(Square square, bool isWhite)
	{
		ulong sidesMask = (Pawn.PawnEval.FileBitBoard << Math.Max(0, square.File-1)) |
			(Pawn.PawnEval.FileBitBoard << Math.Min(7, square.File+1));

		ulong rankMask = RankBitBoard >> (short) (isWhite ? (8 * (square.Rank-1)) : (8 * (square.Rank+1)));

		return sidesMask & rankMask;
	}

	static short EvalKnightOutpostsForColor(Board board, bool white)
	{
		short score = 0;

		PieceList knights = board.GetPieceList(PieceType.Knight, white);
		ulong enemyPawns = board.GetPieceBitboard(PieceType.Pawn, !white);
		ulong friendlyPawns = board.GetPieceBitboard(PieceType.Pawn, white);

		foreach (Piece knight in knights)
		{
			ulong frontSideView = GetFrontSideView(knight.Square, white);
			ulong pawnProtectionMask = GetPawnProtectionMask(knight.Square, white);

			if (((frontSideView & enemyPawns) == 0) && ((pawnProtectionMask & friendlyPawns) != 0)) // no pawns can attack this piece and it is protected by at least one friendly pawn
			{
				score+=white ? WhiteOutpostRankBonuses[knight.Square.Rank] : BlackOutpostRankBonuses[knight.Square.Rank];
			}
		}

		return score;
	}

	static short EvalBishopOutpostsForColor(Board board, bool white)
	{
		short score = 0;

		PieceList bishops = board.GetPieceList(PieceType.Bishop, white);
		ulong enemyPawns = board.GetPieceBitboard(PieceType.Pawn, !white);
		ulong friendlyPawns = board.GetPieceBitboard(PieceType.Pawn, white);

		foreach (Piece bishop in bishops)
		{
			ulong frontSideView = GetFrontSideView(bishop.Square, white);
			ulong pawnProtectionMask = GetPawnProtectionMask(bishop.Square, white);

			if (((frontSideView & enemyPawns) == 0) && ((pawnProtectionMask & friendlyPawns) != 0)) // no pawns can attack this piece and it is protected by at least one friendly pawn
			{
				score+=white ? WhiteOutpostRankBonuses[bishop.Square.Rank] : BlackOutpostRankBonuses[bishop.Square.Rank];
			}
		}

		return score;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short EvalKnightOutposts(Board board)
	{
		return (short) (EvalKnightOutpostsForColor(board, true)-EvalKnightOutpostsForColor(board, false));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short EvalBishopOutposts(Board board)
	{
		return (short) (EvalBishopOutpostsForColor(board, true)-EvalBishopOutpostsForColor(board, false));		
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Evaluate(Board board)
	{
		return (short) (EvalBishopOutposts(board)+EvalKnightOutposts(board));
	}
}