using ChessChallenge.API;

namespace GianMarco.Evaluation;

public static class PiecePosition
{
	static readonly int[] KnightBonuses = [
		-10, 0,  0,  0,  0,  0, 0, -10,
		  0, 5,  5,  5,  5,  5, 5,   0,
		  0, 5, 10, 10, 10, 10, 5,   0,
		  0, 5, 10, 10, 10, 10, 5,   0,
		  0, 5, 10, 15, 15, 10, 5,   0,
		  0, 5, 10, 10, 10, 10, 5,   0,
		  0, 5,  5,  5,  5,  5, 5,   0,
		-10, 0,  0,  0,  0,  0, 0, -10
	];

	static readonly int[] BishopBonuses = [
		-10, -5,  0,  0,  0,  0, -5, -10,
		 -5,  0,  5,  5,  5,  5,  0,  -5,
		  0,  5, 10, 10, 10, 10,  5,   0,
		  0,  5, 10, 10, 10, 10,  5,   0,
		  0,  5, 10, 10, 10, 10,  5,   0,
		  0,  5, 10, 10, 10, 10,  5,   0,
		 -5,  0,  5,  5,  5,  5,  0,  -5,
		-10, -5,  0,  0,  0,  0, -5, -10
	];

	static readonly int[] RookBonuses = [
		0, 0, 0, 0, 0, 0, 0, 0,
		15, 15, 15, 15, 15, 15, 15, 15,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 5, 10, 10, 0, 0, 0
	];

	// queens can go basically everywhere and need separate guardrails to not get taken/trapped, etc.
	// so we do not handle them via a PST here; they are already encouraged to develop by QueenSafety.cs

	static int EvaluateForColor(Board board, bool white)
	{
		int score = 0;

		PieceList knights = board.GetPieceList(PieceType.Knight, white);
		PieceList bishops = board.GetPieceList(PieceType.Bishop, white);
		PieceList rooks = board.GetPieceList(PieceType.Rook, white);

		for (int i = 0; i < knights.Count; i++)
		{
			score+=PSTHelper.GetPSTValue(KnightBonuses, knights[i].Square.Index, white);
		}

		for (int i = 0; i < bishops.Count; i++)
		{
			score+=PSTHelper.GetPSTValue(BishopBonuses, bishops[i].Square.Index, white);
		}

		for (int i = 0; i < rooks.Count; i++)
		{
			score+=PSTHelper.GetPSTValue(RookBonuses, rooks[i].Square.Index, white);
		}

		return score;
	}


	public static int Evaluate(Board board) // we want pieces closer to the center
	{
		return EvaluateForColor(board, true)-EvaluateForColor(board, false);
	}
}