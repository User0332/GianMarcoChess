using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search.Utils;
using GianMarco.TranspositionTable;

namespace GianMarco.Search;

public sealed class IterDeepSearch
{
	public const uint MaxDepth = 40;
	const uint TTHeapSizeMB = 200;
	const int StartDepth = 1;
	const int AspirationWindowBaseDelta = 30;

	readonly Board board;
	readonly uint maxDepth;
	bool endSearchFlag = false;
	readonly List<BasicSearch> searches = new(20);
	Move[] lastPV = [];
	int lastScore = Constants.ImpossibleEval;

	public IterDeepSearch(Board board, uint maxDepth)
	{
		if (maxDepth < StartDepth) maxDepth = StartDepth;

		this.board = board;
		this.maxDepth = maxDepth;
	}

	public void Search()
	{
		new Thread(() => {
			// Span<Entry> ttSpan = stackalloc Entry[(int) Math.Floor((double) (TTStackSizeMB*1000000)/Marshal.SizeOf<Entry>())];

			// CombinationTTable sharedTT = new(board, ttSpan, TTHeapSizeMB);

			HeapTranspositionTable sharedTT = new(board, TTHeapSizeMB);

			for (int i = StartDepth; i <= maxDepth; i++)
			{
				// give +10 as the projected max depth to allow for extensions/quiescence search to utilize killer move heuristics
				BasicSearch search;

				int aspirationWindowDelta = AspirationWindowBaseDelta;
				int aspirationAlpha;
				int aspirationBeta;

				if (lastScore != Constants.ImpossibleEval)
				{
					aspirationAlpha = lastScore - aspirationWindowDelta;
					aspirationBeta = lastScore + aspirationWindowDelta;
				}
				else
				{
					aspirationAlpha = Constants.MinEval;
					aspirationBeta = Constants.MaxEval;
				}

				while (true)
				{
					search = new(board, i+10);

					searches.Add(search);

					Move bestMove = search.Execute(i, sharedTT, lastPV);

					int score = search.BestScore;

					if (score > aspirationAlpha && score < aspirationBeta)
					{
						// score is within the aspiration window, we are good
						break;
					}

					aspirationWindowDelta*=2; // exponential window widening

					if (score <= aspirationAlpha)
					{
						aspirationAlpha-=aspirationWindowDelta;
					}
					else if (score >= aspirationBeta)
					{
						aspirationBeta+=aspirationWindowDelta;
					}

					break;
				}

				lastPV = [..search.BestLine]; // copy best line
				lastScore = search.BestScore;

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