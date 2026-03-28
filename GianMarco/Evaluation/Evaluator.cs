using ChessChallenge.API;
using GianMarco.Search.Utils;

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
		int score = Material.Evaluate(board);

		ulong whiteAttacks = AttackUtils.GetPseudoLegalAttackBitboard(board, true);
		ulong blackAttacks = AttackUtils.GetPseudoLegalAttackBitboard(board, false);

		score+=KingSafety.Evaluate(board, score);
		score+=Pawn.Evaluate(board);
		score+=QueenSafety.Evaluate(board, whiteAttacks, blackAttacks);
		score+=PiecePosition.Evaluate(board);

		// we only care about these things in the opening/middlegame; in the endgame, may mislead the engine
		if (!GamePhaseUtils.IsEndgame(board))
		{
			score+=Development.Evaluate(board);
			score+=CenterControl.Evaluate(board, whiteAttacks, blackAttacks);
		}

		return score;
	}

	public static int EvalPositionWithPerspective(Board board)
	{
		var eval =  board.IsWhiteToMove ? EvalPosition(board) : -EvalPosition(board);

		if (
			GamePhaseUtils.IsEndgame(board) &&
			Material.SideToMoveCannotWin(board)
		)
		{
			eval = Math.Min(eval, Constants.DrawValue); // if we have insufficient material, we cannot be winning, but we could still be losing (if opponent has more material)
		}

		return eval;
	}
}