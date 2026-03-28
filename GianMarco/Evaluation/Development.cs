using System.Numerics;
using ChessChallenge.API;

namespace GianMarco.Evaluation;

public static class Development
{
	const ulong WhiteBackRank = 0xFF;
	const ulong BlackBackRank = 0xFF00000000000000;
	const int UndevelopedPenalty = -10;

	static int EvaluateForColor(Board board, bool white)
	{
		int score = 0;

		ulong backRank = white ? WhiteBackRank : BlackBackRank;

		for (PieceType pieceType = PieceType.Knight; pieceType <= PieceType.Queen; pieceType++)
		{
			// don't penalize rooks since they are often activated in endgame only
			if (pieceType == PieceType.Rook) continue;

			ulong pieceBitboard = board.board.pieceBitboards[white ? (int) pieceType : (int) pieceType | 8];

			int piecesOnBackRank = BitOperations.PopCount(pieceBitboard & backRank);

			score+=piecesOnBackRank*UndevelopedPenalty;
		}

		return score;
	}

	public static int Evaluate(Board board)
	{
		return EvaluateForColor(board, true)-EvaluateForColor(board, false);
	}
}