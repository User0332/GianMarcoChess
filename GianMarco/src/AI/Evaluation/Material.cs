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
	public const short KingValue = 1000;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short GetPieceValue(PieceType pieceType)
	{
		return pieceType switch
		{
			PieceType.Pawn => 100,
			PieceType.Bishop => 300,
			PieceType.Knight => 300,
			PieceType.Rook => 500,
			PieceType.Queen => 900,
			PieceType.King => 1000,
			_ => throw new ArgumentException("Unknown Piece Type!"),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte GetCount(Board board, PieceType pieceType, bool white)
	{
		return (byte) BitOperations.PopCount(board.GetPieceBitboard(pieceType, white));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short GetCombinedMaterialValue(Board board, PieceType pieceType, bool white)
	{
		return (short) (GetCount(board, pieceType, white)*GetPieceValue(pieceType));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short CountMaterial(Board board)
	{
		return (short) (
			GetCombinedMaterialValue(board, PieceType.Pawn, true)-GetCombinedMaterialValue(board, PieceType.Pawn, false)+
			GetCombinedMaterialValue(board, PieceType.Knight, true)-GetCombinedMaterialValue(board, PieceType.Knight, false)+
			GetCombinedMaterialValue(board, PieceType.Bishop, true)-GetCombinedMaterialValue(board, PieceType.Bishop, false)+
			GetCombinedMaterialValue(board, PieceType.Rook, true)-GetCombinedMaterialValue(board, PieceType.Rook, false)+
			GetCombinedMaterialValue(board, PieceType.Queen, true)-GetCombinedMaterialValue(board, PieceType.Queen, false)
		);
	}
}