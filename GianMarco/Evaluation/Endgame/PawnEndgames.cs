using System.Numerics;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Endgame;

public static class PawnEndgames
{
	const int OppositionInPurePawnEndgameBonus = 200;
	const int WinningPawnEndgameBonus = 1000;

	static bool IsSinglePawnEndgameProbablyWon(Board board, Square ourKing, Square enemyKing, Square pawn, bool white)
	{
		bool pawnsTurn = board.IsWhiteToMove == white;
		bool ourKingIsNextToPawnFile = Math.Abs(ourKing.File-pawn.File) <= 1;
		int optimisticPawnRank;
		bool enemyKingIsInCriticalSquare;

		if (white)
		{
			int targetRankIdx = 7;
			optimisticPawnRank = pawnsTurn ? pawn.Rank + 1 : pawn.Rank;

			int criticalSquareRadius = targetRankIdx-optimisticPawnRank;

			if (ourKingIsNextToPawnFile && ourKing.Rank >= 6 && optimisticPawnRank >= 5) return true; // this is an easy escort/red carpet

			enemyKingIsInCriticalSquare = (enemyKing.Rank >= optimisticPawnRank) && (Math.Abs(enemyKing.File-pawn.File) <= criticalSquareRadius);
		}
		else
		{
			int targetRankIdx = 0;
			optimisticPawnRank = pawnsTurn ? pawn.Rank - 1 : pawn.Rank;

			int criticalSquareRadius = optimisticPawnRank-targetRankIdx;

			if (ourKingIsNextToPawnFile && ourKing.Rank <= 1 && optimisticPawnRank <= 2) return true; // this is an easy escort/red carpet

			enemyKingIsInCriticalSquare = (enemyKing.Rank <= optimisticPawnRank) && (Math.Abs(enemyKing.File-pawn.File) <= criticalSquareRadius);
		}

		// enemy king shouldn't be able to reach the pawn in this case
		// we do use an optimistic estimate of the pawn's rank but even if its own king was blocking it, it would still be able to
		// guard the promotion in time so this is definitely won
		if (!enemyKingIsInCriticalSquare) return true;

		// yes technically the kings could move closer to the pawn instead of moving the pawn which creates optimisticPawnRank,
		// but that overcomplicates the eval and could be explored by search instead
		int enemyDistanceFromPawn = Math.Min(Math.Abs(enemyKing.Rank-optimisticPawnRank), Math.Abs(enemyKing.File-pawn.File));
		int ourDistanceFromPawn = Math.Min(Math.Abs(ourKing.Rank-optimisticPawnRank), Math.Abs(ourKing.File-pawn.File));

		if (enemyDistanceFromPawn+1 < ourDistanceFromPawn) return false; // enemy will most likely stop our pawn before we can save it

		// check for opposition

		bool ourKingIsInFrontOfPawn = white ? (ourKing.Rank >= pawn.Rank) : (ourKing.Rank <= pawn.Rank);

		// if it's not our turn, we have opposition, and we are in front of the pawn, we win because we will maintain opposition and move towards the promotion square while the enemy king is forced to move away from the pawn
		bool weHaveOpposition = CalculateOpposition(board, ourKing, enemyKing) == white;

		if (weHaveOpposition && ourKingIsInFrontOfPawn) return true;

		// we could actually still win even if we don't have opposition,
		// if our king is closer to the pawn file-wise,
		// we can still force a win due to outflanking

		int enemyDistanceFromPawnFile = Math.Abs(enemyKing.File-pawn.File);
		int ourDistanceFromPawnFile = Math.Abs(ourKing.File-pawn.File);

		if (pawnsTurn) // our turn, we can move closer to the pawn file
		{
			ourDistanceFromPawnFile = Math.Max(ourDistanceFromPawnFile-1, 0);
		}
		else // enemy's turn, they can move closer to the pawn file
		{
			enemyDistanceFromPawnFile = Math.Max(enemyDistanceFromPawnFile-1, 0);
		}

		bool weAreCloserToPawnFile = ourDistanceFromPawnFile < enemyDistanceFromPawnFile;

		if (weAreCloserToPawnFile && ourKingIsInFrontOfPawn) return true;

		return false; // this is likely a draw
	}

	/// <summary>
	/// Rectangular opposition generalizes for direct & diagonal opposition
	/// </summary>
	/// <param name="board"></param>
	/// <param name="whiteKing"></param>
	/// <param name="blackKing"></param>
	/// <param name="pawn"></param>
	/// <returns>true if white has opposition and false if black does</returns>
	static bool CalculateOpposition(Board board, Square whiteKing, Square blackKing)
	{
		int rankDistance = Math.Abs(whiteKing.Rank-blackKing.Rank);
		int fileDistance = Math.Abs(whiteKing.File-blackKing.File);

		int distanceParity = (rankDistance + fileDistance) % 2;

		return !board.IsWhiteToMove && distanceParity == 0;
	}

	static int EvaluateForColor(Board board, Square whiteKing, Square blackKing, bool white, out bool theoreticalDraw)
	{
		theoreticalDraw = false;

		Square ourKing, enemyKing;

		if (white)
		{
			ourKing = whiteKing;
			enemyKing = blackKing;
		}
		else
		{
			ourKing = blackKing;
			enemyKing = whiteKing;
		}

		ulong ourPawns = board.board.pieceBitboards[white ? ChessChallenge.Chess.PieceHelper.WhitePawn : ChessChallenge.Chess.PieceHelper.BlackPawn];
		ulong opponentPawns = board.board.pieceBitboards[white ? ChessChallenge.Chess.PieceHelper.BlackPawn : ChessChallenge.Chess.PieceHelper.WhitePawn];

		var ourPawnCount = BitOperations.PopCount(ourPawns);
		var opponentPawnCount = BitOperations.PopCount(opponentPawns);

		if (ourPawnCount == 0) return 0;
		if (opponentPawnCount > 0)
		{
			if (opponentPawnCount == 1)
			{
				int opponentPawnSquareIndex = BitOperations.TrailingZeroCount(ourPawns);
				Square opponentPawnSquare = new(opponentPawnSquareIndex);

				// if we can catch this pawn, we can treat it like it doesn't exist, but let's assume that we got dragged
				// to its promotion square while stopping it
				if (!IsSinglePawnEndgameProbablyWon(board, enemyKing, ourKing, opponentPawnSquare, !white)) return 0;

				// remember, if we are white, our opponent is black
				var opponentPromotionRank = white ? 0 : 7;

				ourKing = new(opponentPawnSquare.File, opponentPromotionRank);
			}
			else // the opponent has more than one pawn, this is too complicated for us to evaluate at the moment and the responsibility will be left to search
			{
				return 0;
			}
		}

		if (ourPawnCount > 1)
		{
			return WinningPawnEndgameBonus;
		}

		if (ourPawnCount == 1)
		{
			int pawnSquareIndex = BitOperations.TrailingZeroCount(ourPawns);
			Square pawnSquare = new(pawnSquareIndex);

			if (IsSinglePawnEndgameProbablyWon(board, ourKing, enemyKing, pawnSquare, white))
			{
				return WinningPawnEndgameBonus;
			}
			else
			{
				theoreticalDraw = true;
				return 0;
			}
		}

		return 0;
	}

	public static int Evaluate(Board board, out bool theoreticalDraw)
	{
		// this eval only evaluates pure pawn endgames
		if (board.board.totalPieceCountWithoutPawnsAndKings > 0)
		{
			theoreticalDraw = false;
			return 0;
		}

		var whiteKing = board.GetKingSquare(true);
		var blackKing = board.GetKingSquare(false);

		int eval = EvaluateForColor(board, whiteKing, blackKing, true, out theoreticalDraw);

		if (theoreticalDraw) return 0;

		eval-=EvaluateForColor(board, whiteKing, blackKing, false, out theoreticalDraw);

		return eval;
	}
}