using ChessChallenge.API;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GianMarco.Evaluation.Material;

public static class MaterialEval
{
	public const int PawnValue = 100;
	public const int BishopValue = 300;
	public const int KnightValue = 300;
	public const int RookValue = 500;
	public const int QueenValue = 900;
	public const int KingValue = 10000;
	public static readonly int[] PieceValuesAccordingToType = {
		0, PawnValue, KnightValue, BishopValue, RookValue, QueenValue, KingValue
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetPieceValue(PieceType pieceType)
	{
		return PieceValuesAccordingToType[(int) pieceType];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetPieceValue(int pieceType)
	{
		return PieceValuesAccordingToType[pieceType];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetCount(Board board, int pieceType, bool white)
	{
		return BitOperations.PopCount(board.board.pieceBitboards[white ? pieceType : pieceType | 8]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetCombinedMaterialValue(Board board, int pieceType, bool white)
	{
		return GetCount(board, pieceType, white)*GetPieceValue(pieceType);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountMaterial(Board board)
	{
		return
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Pawn, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Pawn, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Knight, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Knight, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Bishop, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Bishop, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Rook, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Rook, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Queen, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Queen, false);
	}
}