using GianMarco.Evaluation.Material;
using ChessChallenge.API;
using GianMarco.Evaluation.KingSafety;
using GianMarco.Evaluation.Pawn;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;

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
			return (int) Math.Ceiling((double) (Constants.MinEval-score)/2); // MinEval-score so we get a negative value; opponent (black) is getting mated

		return (int) Math.Floor((double) (Constants.MaxEval-score)/2);
	}

	public static int EvalPosition(Board board)
	{
		int score = MaterialEval.CountMaterial(board);

		score+=KingEval.Evaluate(board, score);
		score+=PawnEval.Evaluate(board);

		return score;
	}

	public static int EvalPositionWithPerspective(Board board)
	{
		return board.IsWhiteToMove ? EvalPosition(board) : -EvalPosition(board);
	}
}