using BenchmarkDotNet.Attributes;
using ChessChallenge.API;
using GianMarco.Evaluation;

namespace GianMarco.Testing;

[MemoryDiagnoser]
public class EvaluationBenchmarks
{
	Board board = Board.CreateBoardFromFEN(ChessChallenge.Chess.FenUtility.StartPositionFEN);
	ulong dummyBitboard = (ulong) Random.Shared.Next();

	[Benchmark]
	public void EvaluateGame()
	{
		Evaluator.EvalPositionWithPerspective(board, false);
	}
}