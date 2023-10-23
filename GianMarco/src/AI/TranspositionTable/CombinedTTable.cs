using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ChessChallenge.API;

namespace GianMarco.TTable;

public readonly ref struct CombinationTTable
{
	readonly RefStructTTable stackTTable;
	readonly TranspositionTable heapTTable;
	readonly Board board;
	readonly ulong size;

	public CombinationTTable(Board board, Span<Entry> entrySpan, ushort heapSizeMB) 
	{
		stackTTable = new(board, entrySpan);
		heapTTable = new(
			board,
			(uint) Math.Floor((double) (heapSizeMB*1000000)/Marshal.SizeOf<Entry>())
		);

		this.board = board;

		size = (ulong) stackTTable.size+heapTTable.size;
	}

	public int Index {
		get => (int) (board.ZobristKey % size);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly int LookupEval(int depth, int alpha, int beta)
	{
		if (Index < stackTTable.size)
			return stackTTable.LookupEval(depth, alpha, beta);

		return heapTTable.LookupEval(depth, alpha, beta);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void StoreEvaluation(ushort depth, int eval, byte evalType, Move move)
	{
		if (Index < stackTTable.size)
		{
			stackTTable.StoreEvaluation(depth, eval, evalType, move);
			return;
		}

		heapTTable.StoreEvaluation(depth, eval, evalType, move);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Clear()
	{
		stackTTable.Clear();
		heapTTable.Clear();
	}
}