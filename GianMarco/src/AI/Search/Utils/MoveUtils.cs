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