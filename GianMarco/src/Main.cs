using ChessChallenge.API;

namespace GianMarco;
class Program
{
	static int Main(string[] args)
	{
		Console.Write("Board FEN string: ");

		string? fen = Console.ReadLine();

		if (fen is null) Environment.Exit(1);

		Move move = MoveAI.FromFen(fen);

		Console.WriteLine(move.ToString());

		return 0;
	}
}