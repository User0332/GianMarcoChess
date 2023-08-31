using ChessChallenge.API;

namespace GianMarco;

class MoveAI
{
	public static List<Move> FromFen(string fen)
	{
		Board? board = null;

		try { board = Board.CreateBoardFromFEN(fen); }
		catch (IndexOutOfRangeException)
		{
			Console.WriteLine("Invalid FEN!");
			
			Environment.Exit(1);
		}

		var search = new Search.BasicSearch(board);

		List<Move> bestMoves = search.Execute(5);

		return bestMoves;	
	}
}