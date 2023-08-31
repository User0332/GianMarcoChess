using ChessChallenge.API;

namespace GianMarco.Evaluation.Pawn;

public static class PawnEval
{
	const short PassedPawnBonus = 40;
	const short StackedPawnPenalty = 20;
	const short IsolatedPawnPenalty = 25;

	static readonly short[] WhitePawnRankBonus = {
		0, 0, 0, 0, 0, 0, 0, 0, // first rank only needed to keep indexes correct, white pawns cannot be here
		0, 0, 0, 0, 0, 0, 0, 0,
		5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 5,
		10, 10, 10, 10, 10, 10, 10, 10,
		10, 10, 10, 10, 10, 10, 10, 10,
		20, 20, 20, 20, 20, 20, 20, 20,
		// 30, 30, 30, 30, 30, 30, 30, 30, // last rank not needed, white pawns promote here
	};

	static readonly short[] BlackPawnRankBonus = {
		// 30, 30, 30, 30, 30, 30, 30, 30, // first rank not needed, black pawns promote here
		20, 20, 20, 20, 20, 20, 20, 20,
		10, 10, 10, 10, 10, 10, 10, 10,
		10, 10, 10, 10, 10, 10, 10, 10,
		5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 5,
		0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, // last rank only needed to keep indexes correct, black pawns cannot be here
	};

	static short EvaluatePassedPawns(Board board)
	{
		short score = 0;

		return score;
	}

	static short EvaluateStackedPawns(Board board)
	{
		short score = 0;

		return score;
	}

	static short EvaluateIsolatedPawns(Board board)
	{
		short score = 0;

		return score;
	}

	static short EvaluatePushedPawns(Board board)
	{
		short score = 0;

		foreach (var whitePawn in board.GetPieceList(PieceType.Pawn, true))
			score+=WhitePawnRankBonus[whitePawn.Square.Index];

		foreach (var blackPawn in board.GetPieceList(PieceType.Pawn, false))
			score+=BlackPawnRankBonus[blackPawn.Square.Index];

		return score;
	}

	public static short Evaluate(Board board)
	{
		short score = EvaluatePushedPawns(board);

		score+=EvaluateIsolatedPawns(board);
		score+=EvaluatePassedPawns(board);
		score+=EvaluateStackedPawns(board);

		return score;
	}
}