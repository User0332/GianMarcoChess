using System.Numerics;
using ChessChallenge.API;
using GianMarco.Search.Utils;

namespace GianMarco.Evaluation;
// TODO: be able to calculate piece distance using index so we can optimize out the new Square() and only use index instead
public static class King
{
	const int KingMobilityMultiplier = 30;

	static readonly int[] KingNonEndgameScores = [
		-40, -40, -40, -40, -40, -40, -40, -40,
		-20, -20, -20, -20, -20, -20, -20, -20,
		-20, -20, -20, -20, -20, -20, -20, -20,
		-20, -20, -20, -20, -20, -20, -20, -20,
		-10, -10, -10, -10, -10, -10, -10, -10,
		-10, -10, -10, -10, -10, -10, -10, -10,
		-10, -10, -10, -10, -10, -10, -10, -10,
		 15,  15,  40,   0,   0,  15,  25,  15
	];

	static readonly int[] KingEndgameScores = [
		-15, -5, -5, -5, -5, -5, -5, -15,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-5  , 5 , 5 , 10, 10, 5 , 5 , -5,
		-5  , 5 , 10, 15, 15, 10, 5 , -5,
		-5  , 5 , 10, 15, 15, 10, 5 , -5,
		-5  , 5 , 5 , 10, 10, 5 , 5 , -5,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-15, -5, -5, -5, -5, -5, -5, -15,
	];

	static int KingPositionSafetyNormal(Square whiteKingSquare, Square blackKingSquare)
	{
		return
			PSTHelper.GetPSTValue(KingNonEndgameScores, whiteKingSquare.Index, true)-
			PSTHelper.GetPSTValue(KingNonEndgameScores, blackKingSquare.Index, false);
	}

	static int EvaluateKingMobilityForColor(ulong enemyAttacks, Square kingSquare)
	{
		ulong kingMoves = BitboardHelper.GetKingAttacks(kingSquare);

		int mobility = BitOperations.PopCount(kingMoves & ~enemyAttacks);

		return mobility*KingMobilityMultiplier;
	}

	public static int EvaluateEndgame(Board board, ulong whiteAttacks, ulong blackAttacks)
	{
		Square whiteKingSquare = new(board.board.KingSquare[ChessChallenge.Chess.Board.WhiteIndex]);
		Square blackKingSquare = new(board.board.KingSquare[ChessChallenge.Chess.Board.BlackIndex]);

		int score =
			PSTHelper.GetPSTValue(KingEndgameScores, whiteKingSquare.Index, true)-
			PSTHelper.GetPSTValue(KingEndgameScores, blackKingSquare.Index, false);

		return score;
	}

	public static int EvaluateOpeningAndMiddlegame(Board board)
	{
		Square whiteKingSquare = new(board.board.KingSquare[ChessChallenge.Chess.Board.WhiteIndex]);
		Square blackKingSquare = new(board.board.KingSquare[ChessChallenge.Chess.Board.BlackIndex]);

		return KingPositionSafetyNormal(whiteKingSquare, blackKingSquare);
	}
}