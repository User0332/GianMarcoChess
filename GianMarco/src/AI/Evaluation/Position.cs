using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Position;

public static class PiecePositionalEval
{
	static readonly sbyte[] KnightBonuses = {
		-10, -10, -10, -10, -10, -10, -10, -10,
		-10, 0  , 0  , 0  , 0  , 0  , 0  , -10,
		-10, 0  , 5  , 15 , 15 , 5  , 0  , -10,
		-10, 0  , 15 , 20 , 20 , 15 , 0  , -10,
		-10, 0  , 15 , 20 , 20 , 15 , 0  , -10,
		-10, 0  , 5  , 15 , 15 , 5  , 0  , -10,
		-10, 0  , 0  , 0  , 0  , 0  , 0  , -10,
		-10, -10, -10, -10, -10, -10, -10, -10,
	};

	static readonly sbyte[] BishopBonuses = {
		-15, -10, -5 , -5 , -5 , -5 , -10, -15,
		-10, 0  , 5  , 5  , 5  , 5  , 0  , -10,
		-5 , 5  , 5  , 15 , 15 , 5  , 5  , -5 ,
		-5 , 5  , 15 , 20 , 20 , 15 , 5  , -5 ,
		-5 , 5  , 15 , 20 , 20 , 15 , 5  , -5 ,
		-5 , 5  , 5  , 15 , 15 , 5  , 5  , -5 ,
		-10, 0  , 5  , 5  , 5  , 5  , 0  , -10,
		-15, -10, -5 , -5 , -5 , -5 , -10, -15,
	};

	static readonly sbyte[] RookBonuses = {
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
	};

	static short EvaluateForColor(Board board, bool white)
	{
		short score = 0;

		PieceList knights = board.GetPieceList(PieceType.Knight, white);
		PieceList bishops = board.GetPieceList(PieceType.Bishop, white);
		PieceList rooks = board.GetPieceList(PieceType.Rook, white);

		foreach (Piece knight in knights)
			score+=KnightBonuses[knight.Square.Index];

		foreach (Piece bishop in bishops)
			score+=BishopBonuses[bishop.Square.Index];

		foreach (Piece rook in rooks)
			score+=RookBonuses[rook.Square.Index];

		return score;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Evaluate(Board board) // we want pieces closer to the center
	{
		return (short) (EvaluateForColor(board, true)-EvaluateForColor(board, false));
	}
}