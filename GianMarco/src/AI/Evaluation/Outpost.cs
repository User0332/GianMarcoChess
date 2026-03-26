using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Outpost;

public static class OutpostEval
{
	static readonly int[] WhiteOutpostRankBonuses = {
		0,
		0,
		0,
		10,
		20,
		25,
		30,
		20
	};

	static readonly int[] BlackOutpostRankBonuses = {
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

		int shifter = (square.Rank+1) << 3; // << 3 same as * 8
		ulong frontMask = white ? (ulong.MaxValue << shifter) : (ulong.MaxValue >> shifter);

		return frontMask & sidesMask;
	}

	static ulong GetPawnProtectionMask(Square square, bool isWhite)
	{
		ulong sidesMask = (Pawn.PawnEval.FileBitBoard << Math.Max(0, square.File-1)) |
			(Pawn.PawnEval.FileBitBoard << Math.Min(7, square.File+1));

		ulong rankMask = RankBitBoard >> (isWhite ? ((square.Rank-1) << 3) : ((square.Rank+1) << 3)); // << 3 same as * 8

		return sidesMask & rankMask;
	}

	static int EvalKnightOutpostsForColor(PieceList knights, ulong friendlyPawns, ulong enemyPawns, bool white)
	{
		int score = 0;

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

	static int EvalBishopOutpostsForColor(PieceList bishops, ulong friendlyPawns, ulong enemyPawns, bool white)
	{
		int score = 0;

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
	static int EvalKnightOutposts(PieceList whiteKnights, PieceList blackKnights, ulong whitePawns, ulong blackPawns)
	{
		return (int) (EvalKnightOutpostsForColor(whiteKnights, whitePawns, blackPawns, true)-EvalKnightOutpostsForColor(blackKnights, blackPawns, whitePawns, false));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int EvalBishopOutposts(PieceList whiteBishops, PieceList blackBishops, ulong whitePawns, ulong blackPawns)
	{
		return (int) (EvalBishopOutpostsForColor(whiteBishops, whitePawns, blackPawns, true)-EvalBishopOutpostsForColor(blackBishops, blackPawns, whitePawns, false));		
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Evaluate(Board board)
	{
		ulong whitePawns = board.board.pieceBitboards[ChessChallenge.Chess.PieceHelper.WhitePawn];
		ulong blackPawns = board.board.pieceBitboards[ChessChallenge.Chess.PieceHelper.BlackPawn];

		PieceList whiteBishops = board.allPieceLists[ChessChallenge.Chess.PieceHelper.WhiteBishop];
		PieceList blackBishops = board.allPieceLists[ChessChallenge.Chess.PieceHelper.BlackBishop];
		PieceList whiteKnights = board.allPieceLists[ChessChallenge.Chess.PieceHelper.WhiteKnight];
		PieceList blackKnights = board.allPieceLists[ChessChallenge.Chess.PieceHelper.BlackKnight];


		return (int) (EvalBishopOutposts(whiteBishops, blackBishops, whitePawns, blackPawns)+EvalKnightOutposts(whiteKnights, blackKnights, whitePawns, blackPawns));
	}
}