using ChessChallenge.API;
using GianMarco.Search.Utils;

namespace GianMarco.Evaluation;
// TODO: be able to calculate piece distance using index so we can optimize out the new Square() and only use index instead
public static class KingSafety
{
	static readonly int[] KingNonEndgameScores = [
		-40, -40, -40, -40, -40, -40, -40, -40,
		-20, -20, -20, -20, -20, -20, -20, -20,
		-20, -20, -20, -20, -20, -20, -20, -20,
		-20, -20, -20, -20, -20, -20, -20, -20,
		-10, -10, -10, -10, -10, -10, -10, -10,
		-10, -10, -10, -10, -10, -10, -10, -10,
		-10, -10, -10, -10, -10, -10, -10, -10,
		 15,  15,  25,   0,   0,  15,  25,  15
	];

	static readonly int[] KingEndgameScores = [
		-10, -5, -5, -5, -5, -5, -5, -10,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-5  , 5 , 5 , 15, 15, 5 , 5 , -5,
		-5  , 5 , 15, 20, 20, 15, 5 , -5,
		-5  , 5 , 15, 20, 20, 15, 5 , -5,
		-5  , 5 , 5 , 15, 15, 5 , 5 , -5,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-10, -5, -5, -5, -5, -5, -5, -10,
	];

	const int KingEndgameDistanceWeight = 5;
	const int DangerousPieceProximityWeight = 10;
	const int CastleBonus = 20;


	static int CalculateSquareDistance(Square squareOne, Square squareTwo)
	{
		int fileDistance = Math.Abs(squareOne.File-squareTwo.File);
		int rankDistance = Math.Abs(squareOne.Rank-squareTwo.Rank);

		return fileDistance+rankDistance;
	}

	static int CalculateDangerousPieceProximityScore(Board board, Square kingSquare, bool whiteIsAttacking)
	{
		int score = 0;

		PieceList queens = board.allPieceLists[whiteIsAttacking ? ChessChallenge.Chess.PieceHelper.WhiteQueen : ChessChallenge.Chess.PieceHelper.BlackQueen];

		for (int i = 0; i < queens.Count; i++)
		{
			int manhattanDistance = CalculateSquareDistance(queens[i].Square, kingSquare);

			if (manhattanDistance > 4) continue; // the enemy queen is decently far away, don't apply any penalties

			score+=manhattanDistance*DangerousPieceProximityWeight; // the farther dangerous pieces are from our king, the better, so we add the distance times a multiplier to the eval
		}

		return score;
	}

	/// <summary>
	///  In the endgame, we want kings farther away from the corners
	/// </summary>

	static int KingPositionSafetyEndgame(Square whiteKingSquare, Square blackKingSquare, int materialDifference)
	{
		// never flip the PST since it's centric/symmetric about origin and not rank-based in endgame
		int score = PSTHelper.GetPSTValue(KingEndgameScores, whiteKingSquare.Index, false)-PSTHelper.GetPSTValue(KingEndgameScores, blackKingSquare.Index, false);

		int kingDistance = CalculateSquareDistance(whiteKingSquare, blackKingSquare)*KingEndgameDistanceWeight;

		if (materialDifference > 500) // if white is up material, their king should be closer to the black king
		{ score-=kingDistance; }

		if (materialDifference < 500) // if black is up material, their king should be closer to the white king
		{ score+=kingDistance; }

		return score;
	}

	/// <summary>
	///  In the opening and middlegame, we want kings farther away from the center and in the corners of their own side
	/// </summary>

	static int KingPositionSafetyNormal(Square whiteKingSquare, Square blackKingSquare)
	{
		return
			PSTHelper.GetPSTValue(KingNonEndgameScores, whiteKingSquare.Index, true)-
			PSTHelper.GetPSTValue(KingNonEndgameScores, blackKingSquare.Index, false);
	}


	public static int Evaluate(Board board, int materialDifference)
	{
		Square whiteKingSquare = new(board.board.KingSquare[ChessChallenge.Chess.Board.WhiteIndex]);
		Square blackKingSquare = new(board.board.KingSquare[ChessChallenge.Chess.Board.BlackIndex]);

		int dangerousPieceProximityScore = 0;
		// int dangerousPieceProximityScore = CalculateDangerousPieceProximityScore(board, whiteKingSquare, false)-CalculateDangerousPieceProximityScore(board, blackKingSquare, true);

		if (GamePhaseUtils.IsEndgame(board))
		{
			return KingPositionSafetyEndgame(whiteKingSquare, blackKingSquare, materialDifference)+dangerousPieceProximityScore;
		}

		return KingPositionSafetyNormal(whiteKingSquare, blackKingSquare)+dangerousPieceProximityScore;
	}
}