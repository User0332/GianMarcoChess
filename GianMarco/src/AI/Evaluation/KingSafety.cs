using System.Runtime.CompilerServices;
using ChessChallenge.API;
using GianMarco.Search.Utils;

namespace GianMarco.Evaluation.KingSafety;

public static class KingEval
{
	static readonly short[] WhiteKingNormalScores = {
		25, 30, 25, 15, 15, 25, 35, 25,
		20, 20, 20, 10, 10, 20, 20, 20,
		10, 10, 10, 0 , 0 , 10, 10, 10,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		-10,-10,-10,-10,-10,-10,-10,-10,
		-20,-20,-20,-20,-20,-20,-20,-20,
		-40,-40,-40,-40,-40,-40,-40,-40,
	};

	static readonly short[] BlackKingNormalScores = {		
		-40,-40,-40,-40,-40,-40,-40,-40,	
		-20,-20,-20,-20,-20,-20,-20,-20,
		-10,-10,-10,-10,-10,-10,-10,-10,		
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
		0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,		
		10, 10, 10, 0 , 0 , 10, 10, 10,
		20, 20, 20, 10, 10, 20, 20, 20,
		25, 30, 25, 15, 15, 25, 35, 25,
	};

	static readonly short[] KingEndgameScores = {
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

	const short DangerousPieceProximityWeight = 15;

	static short CalculateDangerousPieceProximityScore(Board board, Square kingSquare, bool whiteIsAttacking)
	{
		short score = 0;

		PieceList queens = board.GetPieceList(PieceType.Queen, whiteIsAttacking);

		foreach (var queen in queens)
		{
			short distance = (short) (Math.Abs(queen.Square.Rank-kingSquare.Rank)+Math.Abs(queen.Square.File-kingSquare.File));
			if (distance == 1) continue; // queen is right next to king

			score+=(short) (distance*DangerousPieceProximityWeight); // the farther dangerous pieces are from our king, the better
		}

		return score;
	}

	/// <summary>
	///  In the endgame, we want kings farther away from the corners
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short KingPositionSafetyEndgame(Square whiteKingSquare, Square blackKingSquare, int materialDifference)
	{
		short score = (short) (KingEndgameScores[whiteKingSquare.Index]-KingEndgameScores[blackKingSquare.Index]);

		short kingDistance = (short) ((Math.Abs(whiteKingSquare.Rank-blackKingSquare.Rank)+Math.Abs(whiteKingSquare.File-blackKingSquare.File))*KingEndgameDistanceWeight);

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
	static short KingPositionSafetyNormal(Square whiteKingSquare, Square blackKingSquare)
	{
		return (short) (WhiteKingNormalScores[whiteKingSquare.Index]-BlackKingNormalScores[blackKingSquare.Index]);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Evaluate(Board board, int materialDifference)
	{
		Square whiteKingSquare = board.GetKingSquare(true);
		Square blackKingSquare = board.GetKingSquare(false);

		short dangerousPieceProximityScore = (short) (CalculateDangerousPieceProximityScore(board, whiteKingSquare, false)-CalculateDangerousPieceProximityScore(board, blackKingSquare, true));

		if (GamePhaseUtils.IsEndgame(board)) return (short) (KingPositionSafetyEndgame(whiteKingSquare, blackKingSquare, materialDifference)+dangerousPieceProximityScore);

		return (short) (KingPositionSafetyNormal(whiteKingSquare, blackKingSquare)+dangerousPieceProximityScore);
	}
}