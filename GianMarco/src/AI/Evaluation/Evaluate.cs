using ChessChallenge.API;
using System.Runtime.CompilerServices;
using GianMarco.Evaluation.Material;
using GianMarco.Evaluation.King;
using GianMarco.Evaluation.Pawn;
using GianMarco.Evaluation.Outpost;
using GianMarco.Evaluation.Position;
using GianMarco.Search.Utils;
using GianMarco.Evaluation.Endgame;

namespace GianMarco.Evaluation;

public static class Constants
{
	public const int MaxEval = 100000000;
	public const int MinEval = -100000000;
	public const int DrawValue = 0;
}

public static class Evaluator
{

	public static bool IsMateScore(int score)
	{
		return ((score < Constants.MinEval+500) && (score != Constants.MinEval)) || ((score > Constants.MaxEval-500) && (score != Constants.MaxEval));
	}


	public static int MateIn(int ply)
	{
		return Constants.MinEval+ply;
	}


	public static int ExtractMateInNMoves(int score)
	{
		if (score < Constants.MinEval+500)
			return (int) Math.Floor((double) (Constants.MinEval-score)/2); // MinEval-score so we get a negative value; opponent (black) is getting mated

		return (int) Math.Ceiling((double) (Constants.MaxEval-score)/2);
	}

	public static int EvalPosition(Board board)
	{
		int score = MaterialEval.CountMaterial(board);

		score+=(
			KingSafety.Evaluate(board, score) +
			PawnEval.Evaluate(board) +
			// OutpostEval.Evaluate(board) +
			PiecePositionalEval.Evaluate(board)
		);

		// Endgame Only Evals (to help with checkmates and puzzles)
		// if (GamePhaseUtils.IsEndgame(board))
		// {
		// 	score+=(
		// 		BishopMate.Evaluate(board) +
		// 		BishopAndKnightMate.Evaluate(board) +
		// 		ThreeKnightsMate.Evaluate(board) +
		// 		PawnEndgame.Evaluate(board)
		// 	);
		// }

		return score;
	}

	public static int EvalPositionWithPerspective(Board board)
	{
		return board.IsWhiteToMove ? EvalPosition(board) : -EvalPosition(board);
	}
}