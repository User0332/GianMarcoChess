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

	public CombinationTTable(Board board, Span<Entry> entrySpan, uint heapSizeMB)
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


	public readonly int LookupEval(int depth, int alpha, int beta)
	{
		if (Index < stackTTable.size)
			return stackTTable.LookupEval(depth, alpha, beta);

		return heapTTable.LookupEval(depth, alpha, beta);
	}


	public readonly void StoreEvaluation(int depth, int eval, int evalType)
	{
		if (Index < stackTTable.size)
		{
			stackTTable.StoreEvaluation(depth, eval, evalType);
			return;
		}

		heapTTable.StoreEvaluation(depth, eval, evalType);
	}


	public readonly void Clear()
	{
		stackTTable.Clear();
		heapTTable.Clear();
	}
}