using ChessChallenge.API;
using System.Numerics;

namespace GianMarco.Evaluation;

public static class Material
{
	public const int PawnValue = 100;

	// add the +10 bonus so we don't eagerly trade knights for three pawns, for example, which may turn out badly
	public const int BishopValue = 310;
	public const int KnightValue = 310;
	public const int RookValue = 510;
	public const int QueenValue = 1200;
	public const int KingValue = 100000;
	public static readonly int[] PieceValuesAccordingToType = [
		0, PawnValue, KnightValue, BishopValue, RookValue, QueenValue, KingValue
	];


	public static int GetPieceValue(PieceType pieceType)
	{
		return PieceValuesAccordingToType[(int) pieceType];
	}


	public static int GetPieceValue(int pieceType)
	{
		return PieceValuesAccordingToType[pieceType];
	}


	public static int GetCount(Board board, int pieceType, bool white)
	{
		return BitOperations.PopCount(board.board.pieceBitboards[white ? pieceType : pieceType | 8]);
	}


	public static int GetCombinedMaterialValue(Board board, int pieceType, bool white)
	{
		return GetCount(board, pieceType, white)*GetPieceValue(pieceType);
	}

	public static int CountMaterialForColor(Board board, bool white)
	{
		return
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Pawn, white)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Knight, white)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Bishop, white)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Rook, white)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Queen, white);
	}


	public static int Evaluate(Board board)
	{
		return CountMaterialForColor(board, true)-CountMaterialForColor(board, false);
	}

	public static bool SideToMoveCannotWin(Board board)
	{
		var pawns = board.board.pieceBitboards[board.IsWhiteToMove ? (int) PieceType.Pawn : (int) PieceType.Pawn | 8];

		if (pawns != 0) return false;

		var queens = board.board.pieceBitboards[board.IsWhiteToMove ? (int) PieceType.Queen : (int) PieceType.Queen | 8];

		if (queens != 0) return false;

		var rooks = board.board.pieceBitboards[board.IsWhiteToMove ? (int) PieceType.Rook : (int) PieceType.Rook | 8];

		if (rooks != 0) return false;

		var knights = board.GetPieceList(PieceType.Knight, board.IsWhiteToMove);

		if (knights.Count > 2) return false; // if there are at least three knights, then there is sufficient material

		var bishops = board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove);

		if (bishops.Count > 0)
		{
			if (knights.Count > 0) return false; // if there is at least one bishop and at least one knight, then there is sufficient material
			if (bishops.Count < 2) return true; // if there is at most one bishop, then there is insufficient material

			bool baseColor = ((bishops[0].Square.Rank + bishops[0].Square.File) % 2) == 0; // if the sum of rank and file is even, it is a dark square; if it is odd, it is a light square

			for (int i = 1; i < bishops.Count; i++)
			{
				bool color = ((bishops[i].Square.Rank + bishops[i].Square.File) % 2) == 0;

				if (color != baseColor) return false; // if there are two bishops on different colors, then there is sufficient material
			}

			return true; // all bishops on same color, insufficient material
		}
		else // bishops.Count == 0
		{
			return true; // if there are no bishops and not enough knights, then there is insufficient material
		}
	}
}