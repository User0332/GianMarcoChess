using System.Runtime.CompilerServices;
using ChessChallenge.API;
using GianMarco.Search.Utils;

namespace GianMarco.Evaluation.King;
// TODO: be able to calculate piece distance using index so we can optimize out the new Square() and only use index instead
public static class KingSafety
{
	static readonly int[] WhiteKingNormalScores = {
		25, 35, 25, 15, 15, 25, 35, 25,
		20, 20, 20, 10, 10, 20, 20, 20,
		10, 10, 10, 0 , 0 , 10, 10, 10,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		-10,-10,-10,-10,-10,-10,-10,-10,
		-20,-20,-20,-20,-20,-20,-20,-20,
		-40,-40,-40,-40,-40,-40,-40,-40,
	};

	static readonly int[] BlackKingNormalScores = {		
		-40,-40,-40,-40,-40,-40,-40,-40,	
		-20,-20,-20,-20,-20,-20,-20,-20,
		-10,-10,-10,-10,-10,-10,-10,-10,		
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,		
		10, 10, 10, 0 , 0 , 10, 10, 10,
		20, 20, 20, 10, 10, 20, 20, 20,
		25, 35, 25, 15, 15, 25, 35, 25,
	};

	static readonly int[] KingEndgameScores = {
		-10, -5, -5, -5, -5, -5, -5, -10,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-5  , 5 , 5 , 15, 15, 5 , 5 , -5,
		-5  , 5 , 15, 20, 20, 15, 5 , -5,
		-5  , 5 , 15, 20, 20, 15, 5 , -5,
		-5  , 5 , 5 , 15, 15, 5 , 5 , -5,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-10, -5, -5, -5, -5, -5, -5, -10,
	};

	const int KingEndgameDistanceWeight = 25;

	const int DangerousPieceProximityWeight = 30; // king safety is important!!!

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int CalculateSquareDistance(int squareOne, int squareTwo)
	{
		(int rankOne, int fileOne) = (squareOne >> 3, squareOne & 0b000111);
		(int rankTwo, int fileTwo) = (squareTwo >> 3, squareTwo & 0b000111);

		int fileDistance = Math.Abs(fileOne-fileTwo);
		int rankDistance = Math.Abs(rankOne-rankTwo);

		return fileDistance+rankDistance;
	}

	static int CalculateDangerousPieceProximityScore(Board board, int kingSquare, bool whiteIsAttacking)
	{
		int score = 0;

		PieceList queens = board.allPieceLists[whiteIsAttacking ? ChessChallenge.Chess.PieceHelper.WhiteQueen: ChessChallenge.Chess.PieceHelper.BlackQueen];

		foreach (var queen in queens)
		{
			int distance = CalculateSquareDistance(queen.Square.Index, kingSquare);

			if (distance == 1) continue; // queen is right next to king

			score+=distance*DangerousPieceProximityWeight; // the farther dangerous pieces are from our king, the better
		}

		return score;
	}

	/// <summary>
	///  In the endgame, we want kings farther away from the corners
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int KingPositionSafetyEndgame(int whiteKingSquare, int blackKingSquare, int materialDifference)
	{
		int score = KingEndgameScores[whiteKingSquare]-KingEndgameScores[blackKingSquare];

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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int KingPositionSafetyNormal(int whiteKingSquare, int blackKingSquare)
	{
		return WhiteKingNormalScores[whiteKingSquare]-BlackKingNormalScores[blackKingSquare];
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Evaluate(Board board, int materialDifference)
	{
		int whiteKingSquare = board.board.KingSquare[ChessChallenge.Chess.Board.WhiteIndex];
		int blackKingSquare = board.board.KingSquare[ChessChallenge.Chess.Board.BlackIndex];

		int dangerousPieceProximityScore = CalculateDangerousPieceProximityScore(board, whiteKingSquare, false)-CalculateDangerousPieceProximityScore(board, blackKingSquare, true);

		if (GamePhaseUtils.IsEndgame(board)) return KingPositionSafetyEndgame(whiteKingSquare, blackKingSquare, materialDifference)+dangerousPieceProximityScore;

		return KingPositionSafetyNormal(whiteKingSquare, blackKingSquare)+dangerousPieceProximityScore;
	}
}