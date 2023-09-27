using System.Runtime.InteropServices;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;
using GianMarco.TTable;

namespace GianMarco.Search;

class IterDeepSearch
{
	public const ushort MAX_DEPTH = 40;
	public const ushort TTSizeMB = 256;
	private const ushort StartDepth = 1;
	private readonly Board board;
	private readonly ushort maxDepth;
	private bool endSearchFlag = false;

	private readonly List<BasicSearch> searches = new(20);

	private readonly List<Move> bestMoves = new(20);
	private readonly TranspositionTable sharedTT;

	public IterDeepSearch(Board board, ushort maxDepth)
	{
		if (maxDepth < StartDepth) maxDepth = StartDepth;
		
		this.board = board;
		this.maxDepth = maxDepth;
		
		sharedTT = new(board, (uint) Math.Floor((double) (TTSizeMB*1000000)/Marshal.SizeOf<Entry>()));
	}

	public void Search()
	{
		new Thread(() => {
			for (ushort i = StartDepth; i<=maxDepth; i++)
			{
				var search = new BasicSearch(
					board,
					sharedTT,
					bestMoves.ToList() // copy the list
				);

				searches.Add(search);

				Move bestMove = search.Execute(i);

				while (bestMoves.Contains(bestMove)) bestMoves.Remove(bestMove);
				
				bestMoves.Insert(0, bestMove);

				if (endSearchFlag) return;
			}

			EndSearch();
		}).Start();
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

		// BottleneckFinder.PrintResults();

		return lastSearch.BestMove;
	}
}