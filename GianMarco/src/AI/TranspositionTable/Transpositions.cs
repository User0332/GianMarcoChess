using System.Runtime.CompilerServices;
using ChessChallenge.API;

namespace GianMarco.TTable;

public readonly ref struct TranspositionTable
{
	public const int LookupFailed = int.MinValue;
	public const byte Exact = 0;
	public const byte LowerBound = 1;
	public const byte UpperBound = 2;

	public readonly Entry[] Entries;
	public readonly uint size;
	public readonly bool Enabled = true;
	readonly Board board;

	public TranspositionTable(Board board, uint size)
	{
		this.board = board;
		this.size = size;
		Entries = new Entry[size];
	}

	public readonly void Clear()
	{
		for (int i = 0; i < Entries.Length; i++)
			Entries[i] = new Entry();
	}

	public ulong Index {
		get => board.ZobristKey % size;
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
	public readonly int LookupEvalWithIndex(int depth, int alpha, int beta)
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

public readonly struct Entry
{
	public readonly ulong Key;
	public readonly int Value;
	public readonly Move Move;
	public readonly ushort Depth;
	public readonly byte NodeType;

	public Entry(ulong key, int value, Move move, ushort depth, byte nodeType)
	{
		Key = key;
		Value = value;
		Move = move;
		Depth = depth;
		NodeType = nodeType;
	}
}