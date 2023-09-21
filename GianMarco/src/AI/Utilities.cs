using GianMarco.Evaluation.Material;
using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;

static class MoveUtils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetUCI(Move move)
	{
		return ChessChallenge.Chess.MoveUtility.GetMoveNameUCI(move.move);
	}
}

static class GamePhaseUtils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEndgame(Board board)
	{
		return board.board.totalPieceCountWithoutPawnsAndKings <= 6;
	}
}

static class MoveOrdering
{
	// new results for 20-generation search: Best Results: capture_bonus=937 promote_bonus=347 castle_bonus=66
	public static short CaptureBonus = 937;
	public static short PromotionBonus = 347;
	public static short CastleBonus = 66;

	static void QSort(in Span<Move> values, in Span<short> scores, int low, int high)
	{
		if (low < high)
		{
			int pivotIndex = Partition(in values, in scores, low, high);
			QSort(in values, in scores, low, pivotIndex - 1);
			QSort(in values, in scores, pivotIndex + 1, high);
		}
	}

	static int Partition(in Span<Move> values, in Span<short> scores, int low, int high)
	{
		short pivotScore = scores[high];
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short CalculateMoveScore(Move move)
	{
		short score = 0;

		if (move.IsCapture)
			score+=(short) (CaptureBonus+MaterialEval.GetPieceValue(move.CapturePieceType)-MaterialEval.GetPieceValue(move.MovePieceType));
		
		if (move.IsPromotion)
			score+=(short) (PromotionBonus+MaterialEval.GetPieceValue(move.PromotionPieceType));
		
		if (move.IsCastles)
			score+=CastleBonus;

		return score;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void OrderMoves(Board board, ref Span<Move> moves)
	{
		Span<short> scores = stackalloc short[moves.Length];

		for (byte i=0; i<moves.Length; i++) scores[i] = CalculateMoveScore(moves[i]);

		QSort(in moves, in scores, 0, moves.Length-1);
	}
}