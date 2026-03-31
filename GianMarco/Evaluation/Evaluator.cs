using ChessChallenge.API;
using GianMarco.Evaluation.Endgame;

namespace GianMarco.Evaluation;

public static class Constants
{
	public const int MaxEval = 100000000;
	public const int MinEval = -100000000;
	public const int DrawValue = 0;
	public const int ImpossibleEval = int.MaxValue;
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

	public static int EvalPosition(Board board, bool isEndgame)
	{
		int score = Material.Evaluate(board);

		score+=Pawn.Evaluate(board);

		// we only care about these things in the opening/middlegame; in the endgame, they may mislead the engine
		// in the endgame, we want to know whether or not we are winning which mainly includes higher depth & endgame-specialized evals anyway
		// in the endgame we really only care about depth and whether or not we are theoretically winning
		if (!isEndgame)
		{
			ulong whiteAttacks = AttackUtils.GetPseudoLegalAttackBitboard(board, true);
			ulong blackAttacks = AttackUtils.GetPseudoLegalAttackBitboard(board, false);

			score+=Development.Evaluate(board);
			score+=CenterControl.Evaluate(board, whiteAttacks, blackAttacks);
			score+=PiecePosition.Evaluate(board);
			score+=Queen.Evaluate(board, whiteAttacks, blackAttacks, isEndgame);
			score+=King.EvaluateOpeningAndMiddlegame(board);
		}
		else
		{
			int pawnEndgameEval = PawnEndgames.Evaluate(board, out var theoreticalDraw);

			if (theoreticalDraw) return 0;

			if (pawnEndgameEval != 0)
			{
				score+=pawnEndgameEval;
				goto EvaluationOver;
			}

			int queenVsRookEval = QueenVsRook.Evaluate(board);

			if (queenVsRookEval != 0)
			{
				score+=queenVsRookEval;
				goto EvaluationOver;
			}

			// if we got here then we didn't detect a special endgame, so we just use this eval to force the losing king into a corner
			ulong whiteAttacks = AttackUtils.GetPseudoLegalAttackBitboard(board, true);
			ulong blackAttacks = AttackUtils.GetPseudoLegalAttackBitboard(board, false);

			score+=King.EvaluateEndgame(board, whiteAttacks, blackAttacks);
		}

		EvaluationOver:


		return score;
	}

	public static int EvalPositionWithPerspective(Board board, bool isEndgame)
	{
		var eval =  board.IsWhiteToMove ? EvalPosition(board, isEndgame) : -EvalPosition(board, isEndgame);

		if (
			isEndgame &&
			Material.SideToMoveCannotWin(board)
		)
		{
			eval = Math.Min(eval, Constants.DrawValue); // if we have insufficient material, we cannot be winning, but we could still be losing (if opponent has more material)
		}

		return eval;
	}
}