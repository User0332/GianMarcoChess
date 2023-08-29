using GianMarco.Evaluation;
using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;

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
	const short CaptureBonus = 200;
	const short PromotionBonus = 400;
	const short CastleBonus = 300;

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

	static short CalculateMoveScore(Move move)
	{
		short score = 0;

		if (move.IsCapture)
			score+=(short) (CaptureBonus+Evaluator.GetPieceValue(move.CapturePieceType)-Evaluator.GetPieceValue(move.MovePieceType));
		
		if (move.IsPromotion)
			score+=(short) (PromotionBonus+Evaluator.GetPieceValue(move.PromotionPieceType));
		
		if (move.IsCastles)
			score+=CastleBonus;

		return score;
	}
	static void OrderEndgame(in Span<Move> moves) { OrderOther(in moves); }
	static void OrderOther(in Span<Move> moves)
	{
		Span<short> scores = stackalloc short[moves.Length];

		for (byte i=0; i<moves.Length; i++) scores[i] = CalculateMoveScore(moves[i]);

		QSort(in moves, in scores, 0, moves.Length-1);
	}

	

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void OrderMoves(Board board, ref Span<Move> moves)
	{
		if (GamePhaseUtils.IsEndgame(board)) { OrderEndgame(moves); return; }

		OrderOther(moves);
	}
}