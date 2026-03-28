using ChessChallenge.API;

namespace GianMarco.TranspositionTable;

public sealed class HeapTranspositionTable(Board board, uint size)
{
	public const int LookupFailed = int.MinValue;
	public const byte Exact = 0;
	public const byte LowerBound = 1;
	public const byte UpperBound = 2;

	public readonly Entry[] Entries = new Entry[size];
	public readonly uint size = size;
	public readonly bool Enabled = true;
	readonly Board board = board;

	public void Clear()
	{
		for (int i = 0; i < Entries.Length; i++)
			Entries[i] = new Entry();
	}

	public ulong Index {
		get => board.ZobristKey % size;
	}


	public int LookupEval(int depth, int alpha, int beta)
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


	public int LookupEvalWithIndex(int depth, int alpha, int beta)
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


	public void StoreEvaluation(int depth, int eval, int evalType)
	{
		if (!Enabled) return;

		Entries[Index] = new Entry(board.ZobristKey, eval, depth, evalType);
	}
}

public readonly struct Entry
{
	public readonly ulong Key;
	public readonly int Value;
	public readonly int Depth;
	public readonly int NodeType;

	public Entry(ulong key, int value, int depth, int nodeType)
	{
		Key = key;
		Value = value;
		Depth = depth;
		NodeType = nodeType;
	}
}