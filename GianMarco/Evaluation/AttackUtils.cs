using ChessChallenge.API;

namespace GianMarco.Evaluation;

public static class AttackUtils
{
	public static ulong GetPseudoLegalAttackBitboard(Board board, bool white)
	{
		ulong attacks = 0;
		ulong occupancy = board.AllPiecesBitboard;

		for (PieceType pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
		{
			PieceList pieces = board.GetPieceList(pieceType, white);

			for (int i = 0; i < pieces.Count; i++)
			{
				ulong attackBitboard = pieceType switch
				{
					PieceType.Pawn => BitboardHelper.GetPawnAttacks(pieces[i].Square, white),
					PieceType.Knight => BitboardHelper.GetKnightAttacks(pieces[i].Square),
					PieceType.Bishop => BitboardHelper.GetBishopAttacks(pieces[i].Square, occupancy),
					PieceType.Rook => BitboardHelper.GetRookAttacks(pieces[i].Square, occupancy),
					PieceType.Queen => BitboardHelper.GetQueenAttacks(pieces[i].Square, occupancy),
					PieceType.King => BitboardHelper.GetKingAttacks(pieces[i].Square),
					_ => 0UL
				};

				attacks|=attackBitboard;
			}
		}

		return attacks;
	}
}