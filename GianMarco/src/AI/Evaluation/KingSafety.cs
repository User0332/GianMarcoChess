using System.Runtime.CompilerServices;
using ChessChallenge.API;
using GianMarco.Search.Utils;

namespace GianMarco.Evaluation.King;
// TODO: be able to calculate piece distance using index so we can optimize out the new Square() and only use index instead
public static class KingSafety
{
	static readonly sbyte[] WhiteKingNormalScores = {
		25, 35, 25, 15, 15, 25, 35, 25,
		20, 20, 20, 10, 10, 20, 20, 20,
		10, 10, 10, 0 , 0 , 10, 10, 10,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		-10,-10,-10,-10,-10,-10,-10,-10,
		-20,-20,-20,-20,-20,-20,-20,-20,
		-40,-40,-40,-40,-40,-40,-40,-40,
	};

	static readonly sbyte[] BlackKingNormalScores = {		
		-40,-40,-40,-40,-40,-40,-40,-40,	
		-20,-20,-20,-20,-20,-20,-20,-20,
		-10,-10,-10,-10,-10,-10,-10,-10,		
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,		
		10, 10, 10, 0 , 0 , 10, 10, 10,
		20, 20, 20, 10, 10, 20, 20, 20,
		25, 35, 25, 15, 15, 25, 35, 25,
	};

	static readonly sbyte[] KingEndgameScores = {
		-10, -5, -5, -5, -5, -5, -5, -10,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-5  , 5 , 5 , 15, 15, 5 , 5 , -5,
		-5  , 5 , 15, 20, 20, 15, 5 , -5,
		-5  , 5 , 15, 20, 20, 15, 5 , -5,
		-5  , 5 , 5 , 15, 15, 5 , 5 , -5,
		-5  , 0 , 5 , 5 , 5 , 5 , 0 , -5,
		-10, -5, -5, -5, -5, -5, -5, -10,
	};

	const short KingEndgameDistanceWeight = 25;

	const short DangerousPieceProximityWeight = 30; // king safety is important!!!

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short CalculateSquareDistance(int squareOne, int squareTwo)
	{
		(int rankOne, int fileOne) = (squareOne >> 3, squareOne & 0b000111);
		(int rankTwo, int fileTwo) = (squareTwo >> 3, squareTwo & 0b000111);

		short fileDistance = (short) Math.Abs(fileOne-fileTwo);
		short rankDistance = (short) Math.Abs(rankOne-rankTwo);

		return (short) (fileDistance+rankDistance);
	}

	static short CalculateDangerousPieceProximityScore(Board board, int kingSquare, bool whiteIsAttacking)
	{
		short score = 0;

		PieceList queens = board.allPieceLists[whiteIsAttacking ? ChessChallenge.Chess.PieceHelper.WhiteQueen: ChessChallenge.Chess.PieceHelper.BlackQueen];

		foreach (var queen in queens)
		{
			short distance = CalculateSquareDistance(queen.Square.Index, kingSquare);

			if (distance == 1) continue; // queen is right next to king

			score+=(short) (distance*DangerousPieceProximityWeight); // the farther dangerous pieces are from our king, the better
		}

		return score;
	}

	/// <summary>
	///  In the endgame, we want kings farther away from the corners
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short KingPositionSafetyEndgame(int whiteKingSquare, int blackKingSquare, int materialDifference)
	{
		short score = (short) (KingEndgameScores[whiteKingSquare]-KingEndgameScores[blackKingSquare]);

		short kingDistance = (short) (CalculateSquareDistance(whiteKingSquare, blackKingSquare)*KingEndgameDistanceWeight);

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
	static short KingPositionSafetyNormal(int whiteKingSquare, int blackKingSquare)
	{
		return (short) (WhiteKingNormalScores[whiteKingSquare]-BlackKingNormalScores[blackKingSquare]);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Evaluate(Board board, int materialDifference)
	{
		int whiteKingSquare = board.board.KingSquare[ChessChallenge.Chess.Board.WhiteIndex];
		int blackKingSquare = board.board.KingSquare[ChessChallenge.Chess.Board.BlackIndex];

		short dangerousPieceProximityScore = (short) (CalculateDangerousPieceProximityScore(board, whiteKingSquare, false)-CalculateDangerousPieceProximityScore(board, blackKingSquare, true));

		if (GamePhaseUtils.IsEndgame(board)) return (short) (KingPositionSafetyEndgame(whiteKingSquare, blackKingSquare, materialDifference)+dangerousPieceProximityScore);

		return (short) (KingPositionSafetyNormal(whiteKingSquare, blackKingSquare)+dangerousPieceProximityScore);
	}
}