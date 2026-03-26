using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Position;

public static class PiecePositionalEval
{
	static readonly int[] KnightBonuses = {
		-10, -10, -10, -10, -10, -10, -10, -10,
		-10, 0  , 0  , 0  , 0  , 0  , 0  , -10,
		-10, 0  , 5  , 15 , 15 , 5  , 0  , -10,
		-10, 0  , 15 , 20 , 20 , 15 , 0  , -10,
		-10, 0  , 15 , 20 , 20 , 15 , 0  , -10,
		-10, 0  , 5  , 15 , 15 , 5  , 0  , -10,
		-10, 0  , 0  , 0  , 0  , 0  , 0  , -10,
		-10, -10, -10, -10, -10, -10, -10, -10,
	};

	static readonly int[] BishopBonuses = {
		-15, -10, -5 , -5 , -5 , -5 , -10, -15,
		-10, 0  , 5  , 5  , 5  , 5  , 0  , -10,
		-5 , 5  , 5  , 15 , 15 , 5  , 5  , -5 ,
		-5 , 5  , 15 , 20 , 20 , 15 , 5  , -5 ,
		-5 , 5  , 15 , 20 , 20 , 15 , 5  , -5 ,
		-5 , 5  , 5  , 15 , 15 , 5  , 5  , -5 ,
		-10, 0  , 5  , 5  , 5  , 5  , 0  , -10,
		-15, -10, -5 , -5 , -5 , -5 , -10, -15,
	};

	static readonly int[] RookBonuses = {
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
	};

	static int EvaluateForColor(Board board, bool white)
	{
		int score = 0;

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
	public static int Evaluate(Board board) // we want pieces closer to the center
	{
		return EvaluateForColor(board, true)-EvaluateForColor(board, false);
	}
}