using BenchmarkDotNet.Attributes;
using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Evaluation.Pawn;

namespace GianMarco;

[MemoryDiagnoser]
public class EvaluationBenchmarks
{
	Board board = Board.CreateBoardFromFEN(ChessChallenge.Chess.FenUtility.StartPositionFEN);
	ulong dummyBitboard = (ulong) Random.Shared.Next();

	[Benchmark]
	public void EvaluateGame()
	{
		Evaluator.EvalPositionWithPerspective(board);
	}

	[Benchmark]
	public void EvalSpecificPawnEval()
	{
		PawnEval.Evaluate(board);
		// PawnEval.EvaluatePassedPawnsForColor(board.allPieceLists[ChessChallenge.Chess.PieceHelper.WhitePawn], dummyBitboard, false);
	}
}