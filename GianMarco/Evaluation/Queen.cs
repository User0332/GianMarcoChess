using System.Numerics;
using ChessChallenge.API;
using GianMarco.Search.Utils;

namespace GianMarco.Evaluation;

// used to prevent the engine from getting its queen trapped
public static class Queen
{
	const int QueenLowSafeMovesPenalty = 40;

	const int Opening_QueenMobilityBonus = 1;
	const int Opening_QueenSafeMovesThresh = 4;

	const int Middlegame_QueenMobilityBonus = 0;
	const int Middlegame_QueenSafeMovesThresh = 4;

	const int Endgame_QueenMobilityBonus = 3;
	const int Endgame_QueenSafeMovesThresh = 5;

	static int GetSafeQueenMoves(Square square, ulong enemyAttacks, ulong occupancy)
	{
		var availableMoves = BitboardHelper.GetQueenAttacks(square, occupancy);

		availableMoves &= ~enemyAttacks; // remove squares attacked by the opponent, as moving there would not be safe

		return BitOperations.PopCount(availableMoves);
	}

	static int EvaluateForColor(Board board, bool white, ulong enemyAttacks, bool isEndgame)
	{
		int score = 0;

		ulong occupancy = board.AllPiecesBitboard;

		PieceList queens = board.GetPieceList(PieceType.Queen, white);

		for (int i = 0; i < queens.Count; i++)
		{
			Square queenSquare = queens[i].Square;

			int safeMoves = GetSafeQueenMoves(queenSquare, enemyAttacks, occupancy);

			if (GamePhaseUtils.IsOpening(board))
			{
				if (safeMoves < Opening_QueenSafeMovesThresh)
				{
					score-=QueenLowSafeMovesPenalty;
				}
				else
				{
					score+=(safeMoves-Opening_QueenSafeMovesThresh)*Opening_QueenMobilityBonus;
				}
			}
			else if (isEndgame) // check endgame before middlegame for performance reasons as the IsMiddlegame call is really just "not opening and not endgame"
			{
				if (safeMoves < Endgame_QueenSafeMovesThresh)
				{
					score-=QueenLowSafeMovesPenalty;
				}
				else
				{
					score+=(safeMoves-Endgame_QueenSafeMovesThresh)*Endgame_QueenMobilityBonus;
				}
			}
			else // middlegame
			{
				if (safeMoves < Middlegame_QueenSafeMovesThresh)
				{
					score-=QueenLowSafeMovesPenalty;
				}
				else
				{
					score+=(safeMoves-Middlegame_QueenSafeMovesThresh)*Middlegame_QueenMobilityBonus;
				}
			}

		}

		return score;
	}


	public static int Evaluate(Board board, ulong whiteAttacks, ulong blackAttacks, bool isEndgame)
	{
		return EvaluateForColor(board, true, blackAttacks, isEndgame)-EvaluateForColor(board, false, whiteAttacks, isEndgame);
	}
}