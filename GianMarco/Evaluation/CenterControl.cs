using System.Numerics;
using ChessChallenge.API;

namespace GianMarco.Evaluation;

public static class CenterControl
{
	const ulong WhiteCentralSquareTargets = 0x3c3c3c000000;
	const ulong BlackCentralSquareTargets = 0x3c3c3c0000;
	const int CenterControlBonus = 5;

	static int EvaluateForColor(ulong colorAttacks, bool white)
	{
		int score = 0;

		int centralSquaresControlled = BitOperations.PopCount(colorAttacks & (white ? WhiteCentralSquareTargets : BlackCentralSquareTargets));

		score+=centralSquaresControlled*CenterControlBonus;

		return score;
	}

	public static int Evaluate(Board board, ulong whiteAttacks, ulong blackAttacks)
	{
		return EvaluateForColor(whiteAttacks, true)-EvaluateForColor(blackAttacks, false);
	}
}