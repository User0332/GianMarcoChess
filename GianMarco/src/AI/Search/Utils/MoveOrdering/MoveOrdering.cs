using GianMarco.Evaluation.Material;
using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;

static class MoveOrdering
{
	// new results for 20-generation search: Best Results: capture_bonus=937 promote_bonus=347 castle_bonus=66
	public static int CaptureBonus = 900;
	public static int PromotionBonus = 600;
	public static int CastleBonus = 200;
	public const int PVMoveBonus = 10000000;
	public const byte MaxKillerMovePly = 20;
	const int FirstKillerMoveBias = 800;
	const int SecondKillerMoveBias = 700;
	const int CloseToFirstKillerMoveBias = 600;
	const int CloseToSecondKillerMoveBias = 500;
	const int HistoryBonusMultiplier = 1;
	public static Move[,] whiteKillerMoves = new Move[MaxKillerMovePly, 2];
	public static Move[,] blackKillerMoves = new Move[MaxKillerMovePly, 2];
	public static int[,,] whiteSearchHistory = new int[MaxKillerMovePly, 64, 64];
	public static int[,,] blackSearchHistory = new int[MaxKillerMovePly, 64, 64];

	static void QSort(in Span<Move> values, in Span<int> scores, int low, int high)
	{
		if (low < high)
		{
			int pivotIndex = Partition(in values, in scores, low, high);
			QSort(in values, in scores, low, pivotIndex - 1);
			QSort(in values, in scores, pivotIndex + 1, high);
		}
	}

	static int Partition(in Span<Move> values, in Span<int> scores, int low, int high)
	{
		int pivotScore = scores[high];
		int i = low - 1;

		for (int j = low; j <= high - 1; j++)
		{
			if (scores[j] > pivotScore)
			{
				i++;
				(values[i], values[j]) = (values[j], values[i]);
				(scores[i], scores[j]) = (scores[j], scores[i]);
			}
		}
		(values[i + 1], values[high]) = (values[high], values[i + 1]);
		(scores[i + 1], scores[high]) = (scores[high], scores[i + 1]);

		return i + 1;
	}


	public static int CalculateMoveScore(Move move, bool inNormalSearch, int depthFromRoot, int[,,] history, Move[,] killerMoves)
	{
		int score = 0;

		if (move.IsCapture)
			score+=CaptureBonus+MaterialEval.GetPieceValue(move.CapturePieceType)-MaterialEval.GetPieceValue(move.MovePieceType);
		else if (inNormalSearch)
		{
			if (move.IsCastles)
				score+=CastleBonus;

			// killer move ordering
			if 	(depthFromRoot < MaxKillerMovePly)
			{
				if (killerMoves[depthFromRoot, 0].Equals(move))
					return score+FirstKillerMoveBias;

				if (killerMoves[depthFromRoot, 1].Equals(move))
					return score+SecondKillerMoveBias;
			}
		}

		if (move.IsPromotion)
			score+=PromotionBonus+MaterialEval.GetPieceValue(move.PromotionPieceType);

		if (depthFromRoot < MaxKillerMovePly) score+=history[depthFromRoot, move.StartSquare.Index, move.TargetSquare.Index]*HistoryBonusMultiplier;

		return score;
	}


	public static void OrderMoves(Board board, ref Span<Move> moves, Move shouldBeFirst, bool inNormalSearch, int depthFromRoot)
	{
		Span<int> scores = stackalloc int[moves.Length];

		int[,,] history = board.IsWhiteToMove ? whiteSearchHistory : blackSearchHistory;
		Move[,] killerMoves = board.IsWhiteToMove ? whiteKillerMoves : blackKillerMoves;

		// just for performance

		if (shouldBeFirst == Move.NullMove)
		{
			for (int i = 0; i < moves.Length; i++)
			{
				scores[i] = CalculateMoveScore(moves[i], inNormalSearch, depthFromRoot, history, killerMoves);
			}

			QSort(in moves, in scores, 0, moves.Length-1);
		}
		else
		{
			bool foundPVMove = false;

			for (int i = 0; i < moves.Length; i++)
			{
				if (!foundPVMove && moves[i] == shouldBeFirst)
				{
					scores[i] = PVMoveBonus;
					foundPVMove = true;
					continue;
				}

				scores[i] = CalculateMoveScore(moves[i], inNormalSearch, depthFromRoot, history, killerMoves);
			}
		}



		QSort(in moves, in scores, 0, moves.Length-1);
	}
}