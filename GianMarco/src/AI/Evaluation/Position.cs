using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Position;

public static class PiecePositionalEval
{
	static readonly int[] KnightBonuses = {
		-15, -10, -8, -5, -5, -8, -10, -15,
		-10, -5,  0,  5,  5,  0, -5,  -10,
		-8,   0,  10, 15, 15, 10, 0,   -8,
		-5,   5,  15, 20, 20, 15, 5,   -5,
		-5,   5,  15, 20, 20, 15, 5,   -5,
		-8,   0,  10, 15, 15, 10, 0,   -8,
		-10, -5,  0,  5,  5,  0, -5,  -10,
		-15, -10, -8, -5, -5, -8, -10, -15,
	};

	static readonly int[] BishopBonuses = {
		-10, -8, -5, -3, -3, -5, -8, -10,
		-8,  -5, 0,  5,  5,  0, -5, -8,
		-5,   0, 10, 12, 12, 10, 0,  -5,
		-3,   5, 12, 15, 15, 12, 5,  -3,
		-3,   5, 12, 15, 15, 12, 5,  -3,
		-5,   0, 10, 12, 12, 10, 0,  -5,
		-8,  -5, 0,  5,  5,  0, -5, -8,
		-10, -8, -5, -3, -3, -5, -8, -10,
	};

	static readonly int[] RookBonuses = {
		0,  0,  0,  0,  0,  0,  0,  0,
		15, 15, 15, 15, 15, 15, 15, 15,
		10, 10, 10, 10, 10, 10, 10, 10,
		5,  5,  5,  5,  5,  5,  5,  5,
		5,  5,  5,  5,  5,  5,  5,  5,
		10, 10, 10, 10, 10, 10, 10, 10,
		15, 15, 15, 15, 15, 15, 15, 15,
		0,  0,  0,  0,  0,  0,  0,  0,
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


	public static int Evaluate(Board board) // we want pieces closer to the center
	{
		return EvaluateForColor(board, true)-EvaluateForColor(board, false);
	}
}