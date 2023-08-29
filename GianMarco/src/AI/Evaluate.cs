using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation;

class Constants
{
	public const short MaxEval = 32767;
	public const short MinEval = -32767;
	public const short DrawValue = 0;
}

static class Evaluator
{
	static short[] pieceValues = { 0, 100, 300, 300, 500, 900, 0 };

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
	static short CountMaterial(Board board)
	{
		return (short) (
			pieceValues[1]*(board.GetPieceList(PieceType.Pawn, true).Count-board.GetPieceList(PieceType.Pawn, false).Count)+
			pieceValues[2]*(board.GetPieceList(PieceType.Bishop, true).Count+board.GetPieceList(PieceType.Knight, true).Count-(board.GetPieceList(PieceType.Bishop, false).Count+board.GetPieceList(PieceType.Knight, false).Count))+
			pieceValues[4]*(board.GetPieceList(PieceType.Rook, true).Count-board.GetPieceList(PieceType.Rook, false).Count)+
			pieceValues[5]*(board.GetPieceList(PieceType.Queen, true).Count-board.GetPieceList(PieceType.Queen, false).Count)
		);
	}

	public static short EvalPosition(Board board)
	{
		if (board.IsDraw()) return 0;
		 
		short score = CountMaterial(board);

		return score;
	}

	public static short EvalPositionWithPerspective(Board board)
	{
		return board.IsWhiteToMove ? EvalPosition(board) : (short) -EvalPosition(board);
	}
}