using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Search.Utils;

public static class PresetPositionOrder
{
	public enum Positions
	{
		StartPosWhite,
		StartPosBlack,
		CloseToStartPosWhite,
		CloseToStartPosBlack
	}
}