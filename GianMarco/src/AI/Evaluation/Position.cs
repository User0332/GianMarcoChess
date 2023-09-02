using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.Evaluation.Position;

public static class PositionalEval
{
	const ulong OutsideOfTheBoard = 18411139144890810879;
	const ulong SecondToLastOuterRing = 35538699412471296;
	const ulong AlmostCenter = 66125924401152;
	const ulong CenterOfTheBoard = 103481868288;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Evaluate(Board board) // we want pieces closer to the center
	{
		ulong whitePieces = board.WhitePiecesBitboard;
		ulong blackPieces = board.BlackPiecesBitboard;

		return (short) (
			((whitePieces & SecondToLastOuterRing)*1)-((blackPieces & SecondToLastOuterRing)*1)+
			((whitePieces & AlmostCenter)*5)-((blackPieces & AlmostCenter)*5)+
			((whitePieces & CenterOfTheBoard)*8)-((blackPieces & AlmostCenter)*8)
		);
	}
}