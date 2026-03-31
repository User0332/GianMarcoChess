using System.Numerics;
using ChessChallenge.API;
using GianMarco.Search.Utils;

namespace GianMarco.Evaluation;

public static class CenterControl
{
	const ulong WhiteCentralSquareTargets = 0x3c3c3c000000;
	const ulong BlackCentralSquareTargets = 0x3c3c3c0000;
	const int CenterControlBonus = 0;

	const ulong WhiteOpeningPawnControlTargets = 0x181818000000;
	const ulong BlackOpeningPawnControlTargets = 0x1818180000;
	const int OpeningPawnControlBonus = 20;

	static int EvaluateForColor(Board board, ulong colorAttacks, bool white)
	{
		int score = 0;

		int centralSquaresControlled = BitOperations.PopCount(colorAttacks & (white ? WhiteCentralSquareTargets : BlackCentralSquareTargets));

		score+=centralSquaresControlled*CenterControlBonus;

		if (GamePhaseUtils.IsOpening(board))
		{
			score+=EvaluateOpeningPawnControlForColor(board, white);
		}

		return score;
	}

	static int EvaluateOpeningPawnControlForColor(Board board, bool white)
	{
		ulong pawns = board.board.pieceBitboards[white ? ChessChallenge.Chess.PieceHelper.WhitePawn : ChessChallenge.Chess.PieceHelper.BlackPawn];

		int centralPawns = BitOperations.PopCount(pawns & (white ? WhiteOpeningPawnControlTargets : BlackOpeningPawnControlTargets));

		return centralPawns*OpeningPawnControlBonus;
	}

	public static int Evaluate(Board board, ulong whiteAttacks, ulong blackAttacks)
	{
		return EvaluateForColor(board, whiteAttacks, true)-EvaluateForColor(board, blackAttacks, false);
	}
}