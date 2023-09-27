using ChessChallenge.API;
using System.Runtime.CompilerServices;
using GianMarco.Evaluation.Material;
using GianMarco.Evaluation.King;
using GianMarco.Evaluation.Pawn;
using GianMarco.Evaluation.Outpost;
using GianMarco.Evaluation.Position;
using GianMarco.Search.Utils;
using GianMarco.Evaluation.Endgame;
using System.Diagnostics;
using GianMarco.Optimization;

namespace GianMarco.Evaluation;

public static class Constants
{
	public const int MaxEval = 2147483647;
	public const int MinEval = -2147483647;
	public const int DrawValue = 0;

	public const int NegativeTestValue = -1234567890;
	public const int TestValue = 1234567890;
}

public static class Evaluator
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsMateScore(int score)
	{
		return ((score < Constants.MinEval+500) && (score != Constants.MinEval)) || ((score > Constants.MaxEval-500) && (score != Constants.MaxEval));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int MateIn(ushort ply)
	{
		return Constants.MinEval+ply;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ExtractMateInNMoves(int score)
	{
		if (score < Constants.MinEval+500)
			return (int) Math.Floor((double) (Constants.MinEval-score)/2); // MinEval-score so we get a negative value; opponent (black) is getting mated

		return (int) Math.Ceiling((double) (Constants.MaxEval-score)/2);
	}

	public static int EvalPosition(Board board)
	{
		int score = MaterialEval.CountMaterial(board);

		score+=KingSafety.Evaluate(board, score);
		
		score+=PawnEval.Evaluate(board);

		score+=OutpostEval.Evaluate(board);

		score+=PositionalEval.Evaluate(board);

		// Endgame Only Evals (to help with checkmates and puzzles)
		if (GamePhaseUtils.IsEndgame(board))
		{
			score+=BishopMate.Evaluate(board);
			score+=BishopAndKnightMate.Evaluate(board);
			score+=ThreeKnightsMate.Evaluate(board);
			score+=PawnEndgame.Evaluate(board);
		}

		return score;
	}

	public static int EvalPositionWithPerspective(Board board)
	{
		return board.IsWhiteToMove ? EvalPosition(board) : -EvalPosition(board);
	}
}