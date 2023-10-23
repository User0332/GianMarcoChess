using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;
using GianMarco.TTable;

namespace GianMarco.Search;

class IterDeepSearch
{
	public const ushort MAX_DEPTH = 40;
	public const ushort TTStackSizeMB = 2;
	public const ushort TTHeapSizeMB = 115;
	private const ushort StartDepth = 1;
	private readonly Board board;
	private readonly ushort maxDepth;
	private bool endSearchFlag = false;

	private readonly List<BasicSearch> searches = new(20);

	private readonly List<Move> bestMoves = new(20);
	public IterDeepSearch(Board board, ushort maxDepth)
	{
		if (maxDepth < StartDepth) maxDepth = StartDepth;
		
		this.board = board;
		this.maxDepth = maxDepth;
	}

	public void Search()
	{
		new Thread(() => {
			Span<Entry> ttSpan = stackalloc Entry[(int) Math.Floor((double) (TTStackSizeMB*1000000)/Marshal.SizeOf<Entry>())];
			
			CombinationTTable sharedTT = new(board, ttSpan, TTHeapSizeMB);

			for (ushort i = StartDepth; i<=maxDepth; i++)
			{
				var search = new BasicSearch(
					board,
					bestMoves.ToList() // copy the list
				);

				searches.Add(search);

				Move bestMove = search.Execute(i, sharedTT);

				while (bestMoves.Contains(bestMove)) bestMoves.Remove(bestMove);
				
				bestMoves.Insert(0, bestMove);

				if (endSearchFlag) return;
			}

			EndSearch();
		}, int.MaxValue).Start();
	}

	public Move EndSearch()
	{
		endSearchFlag = true;

		foreach (var search in searches) search.KillSearch = true;

		BasicSearch lastSearch;

		try { lastSearch = searches.Last(search => Math.Abs(search.BestScore) != Constants.MaxEval); }
		catch (InvalidOperationException) { lastSearch = searches.Last(); }

		string moveName = MoveUtils.GetUCI(lastSearch.BestMove);

		string scoreString = Evaluator.IsMateScore(lastSearch.BestScore) ? $"mate {Evaluator.ExtractMateInNMoves(lastSearch.BestScore)}" : $"cp {lastSearch.BestScore}";
		string lineString = string.Join(' ', lastSearch.BestLine.Select(MoveUtils.GetUCI));

		Console.WriteLine($"info depth {searches.Count} seldepth {lastSearch.MaxDepthSearched} time {lastSearch.TimeSearchedMs} nodes {lastSearch.NodesSearched} score {scoreString} pv {lineString}");

		Console.WriteLine($"bestmove {moveName}");

		return lastSearch.BestMove;
	}
}