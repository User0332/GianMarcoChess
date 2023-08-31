using GianMarco.Evaluation.Material;
using ChessChallenge.API;
using GianMarco.Evaluation.KingSafety;
using GianMarco.Evaluation.Pawn;

namespace GianMarco.Evaluation;

class Constants
{
	public const short MaxEval = 32767;
	public const short MinEval = -32767;
	public const short DrawValue = 0;
}

static class Evaluator
{
	public static short EvalPosition(Board board)
	{		 
		short score = MaterialEval.CountMaterial(board);

		score+=KingEval.Evaluate(board);
		score+=PawnEval.Evaluate(board);

		return score;
	}

	public static short EvalPositionWithPerspective(Board board)
	{
		return board.IsWhiteToMove ? EvalPosition(board) : (short) -EvalPosition(board);
	}
}