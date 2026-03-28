using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;

public static class MoveUtils
{
	public static string GetUCI(Move move)
	{
		return ChessChallenge.Chess.MoveUtility.GetMoveNameUCI(move.move);
	}
}