using System.Numerics;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;
static class GamePhaseUtils
{

	public static bool IsEndgame(Board board)
	{
		return board.board.totalPieceCountWithoutPawnsAndKings <= 4;
	}

	public static bool ZugzwangLikely(Board board)
	{
		return board.board.totalPieceCountWithoutPawnsAndKings <= 1;
	}
}