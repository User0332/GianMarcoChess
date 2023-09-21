using ChessChallenge.API;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GianMarco.Evaluation.Material;

public static class MaterialEval
{
	public const short PawnValue = 100;
	public const short BishopValue = 300;
	public const short KnightValue = 300;
	public const short RookValue = 500;
	public const short QueenValue = 900;
	public const short KingValue = 10000;
	public static readonly short[] PieceValuesAccordingToType = {
		0, PawnValue, KnightValue, BishopValue, RookValue, QueenValue, KingValue
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short GetPieceValue(PieceType pieceType)
	{
		return PieceValuesAccordingToType[(int) pieceType];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short GetPieceValue(int pieceType)
	{
		return PieceValuesAccordingToType[pieceType];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte GetCount(Board board, int pieceType, bool white)
	{
		return (byte) BitOperations.PopCount(board.board.pieceBitboards[white ? pieceType : pieceType | 8]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short GetCombinedMaterialValue(Board board, int pieceType, bool white)
	{
		return (short) (GetCount(board, pieceType, white)*GetPieceValue(pieceType));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short CountMaterial(Board board)
	{
		return (short) (
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Pawn, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Pawn, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Knight, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Knight, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Bishop, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Bishop, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Rook, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Rook, false)+
			GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Queen, true)-GetCombinedMaterialValue(board, ChessChallenge.Chess.PieceHelper.Queen, false)
		);
	}
}