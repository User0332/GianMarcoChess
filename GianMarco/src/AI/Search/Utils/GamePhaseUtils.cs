using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;
static class GamePhaseUtils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOpening(Board board)
	{
		// We consider it the opening if there are at least 8 pieces on the board (excluding pawns and kings) and we are in the first 10 moves (20 ply) of the game
		return board.board.totalPieceCountWithoutPawnsAndKings >= 8 && board.board.gameStateHistory.Count <= 20;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsMiddlegame(Board board)
	{
		return !IsOpening(board) && !IsEndgame(board);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEndgame(Board board)
	{
		return board.board.totalPieceCountWithoutPawnsAndKings <= 4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ZugzwangLikely(Board board)
	{
		return board.board.totalPieceCountWithoutPawnsAndKings <= 1;
	}
}