using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.TTable;

public readonly ref struct RefStructTTable
{
	public const int LookupFailed = int.MinValue;
	public const byte Exact = 0;
	public const byte LowerBound = 1;
	public const byte UpperBound = 2;
	public readonly Span<Entry> Entries;
	public readonly int size;
	public readonly bool Enabled = true;
	readonly Board board;

	public RefStructTTable(Board board, Span<Entry> entrySpan)
	{
		this.board = board;
		size = entrySpan.Length;
		Entries = entrySpan;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Clear()
	{
		for (int i = 0; i < Entries.Length; i++)
			Entries[i] = new Entry();
	}

	public int Index {
		get =>  (int) (board.ZobristKey % (ulong) size);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly int LookupEval(int depth, int alpha, int beta)
	{
		if (!Enabled) return LookupFailed;

		Entry entry = Entries[Index];

		if ((entry.Key != board.ZobristKey) || (entry.Depth < depth)) return LookupFailed;

		if (
			(entry.NodeType == Exact) ||
			(entry.NodeType == UpperBound && entry.Value <= alpha) ||
			(entry.NodeType == LowerBound && entry.Value >= beta)
		)
			return entry.Value;

		return LookupFailed;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void StoreEvaluation(ushort depth, int eval, byte evalType, Move move)
	{
		if (!Enabled) return;

		Entries[Index] = new Entry(board.ZobristKey, eval, move, depth, evalType);
	}
}