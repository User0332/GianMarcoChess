using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;
static class GamePhaseUtils
{

	public static bool IsEndgame(Board board)
	{
		return board.board.totalPieceCountWithoutPawnsAndKings <= 4;
	}
}