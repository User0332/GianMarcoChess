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
		0 , 5 , 10, 15, 15, 10, 5 , 0 ,
		5 , 5 , 10, 15, 15, 10, 5 , 5 ,
		10, 10, 10, 15, 15, 10, 10, 10,
		15, 15, 15, 20, 20, 15, 15, 15,
		15, 15, 15, 20, 20, 15, 15, 15,
		10, 10, 10, 15, 15, 10, 10, 10,
		5 , 5 , 10, 15, 15, 10, 5 , 5 ,
		0 , 5 , 10, 15, 15, 10, 5 , 0 ,
	};

	/// <summary>
	///  In the endgame, we want kings farther away from the corners
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short KingPositionSafetyEndgame(Board board)
	{
		int whiteKingSquare = board.GetKingSquare(true).Index;
		int blackKingSquare = board.GetKingSquare(false).Index;

		return (short) (KingEndgameScores[whiteKingSquare]-KingEndgameScores[blackKingSquare]);
	}

	/// <summary>
	///  In the opening and middlegame, we want kings farther away from the center and in the corners of their own side
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static short KingPositionSafetyNormal(Board board)
	{
		int whiteKingSquare = board.GetKingSquare(true).Index;
		int blackKingSquare = board.GetKingSquare(false).Index;

		return (short) (WhiteKingNormalScores[whiteKingSquare]-BlackKingNormalScores[blackKingSquare]);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Evaluate(Board board)
	{
		if (GamePhaseUtils.IsEndgame(board)) return KingPositionSafetyEndgame(board);

		return KingPositionSafetyNormal(board);
	}
}