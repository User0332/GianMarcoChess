using ChessChallenge.API;
using GianMarco.Evaluation;
using GianMarco.Search;

namespace GianMarco.UCI;

public static class PreBuiltInterpreter
{
	static Board currBoard = Board.CreateBoardFromFEN(ChessChallenge.Chess.FenUtility.StartPositionFEN);
	static IterDeepSearch? searcher = null;

	static void LoadPosition(string[] cmdArgs)
	{
		if (cmdArgs[1] == "startpos")
		{			
			currBoard.board.LoadPosition(ChessChallenge.Chess.FenUtility.StartPositionFEN);

			if (cmdArgs.Length > 2 && cmdArgs[2] == "moves")
			{
				foreach (string move in cmdArgs.Skip(3))
				{
					currBoard.MakeMove(
						new Move(move, currBoard)
					);
				}
			}
			
			return;
		}
		if (cmdArgs[1] == "fen")
		{
			var fenStringArr = cmdArgs.Skip(2).TakeWhile(arg => arg != "moves");
			string fenString = string.Join(' ', fenStringArr);

			currBoard = Board.CreateBoardFromFEN(fenString);
			

			if (cmdArgs.Length > fenStringArr.Count()+2)
			{
				foreach (string move in cmdArgs.Skip(fenStringArr.Count()+3))
				{
					currBoard.MakeMove(
						new Move(move, currBoard)
					);
				}
			}
		}
	}

	static void Go(string[] cmdArgs)
	{
		int depthIdx = -1;
		ushort searchDepth = IterDeepSearch.MAX_DEPTH;

		if ((depthIdx = Array.IndexOf(cmdArgs, "depth")) != -1)
		{
			string depthString = cmdArgs[depthIdx+1];

			if (depthString != "infinite")
				searchDepth = ushort.Parse(depthString);
		}

		int wtimeIdx = -1;
		int wTimeMs = 0;

		if ((wtimeIdx = Array.IndexOf(cmdArgs, "wtime")) != -1)
		{
			string wtimeString = cmdArgs[wtimeIdx+1];

			wTimeMs = int.Parse(wtimeString);
		}

		int btimeIdx = -1;
		int bTimeMs = 0;

		if ((btimeIdx = Array.IndexOf(cmdArgs, "btime")) != -1)
		{
			string btimeString = cmdArgs[btimeIdx+1];
			
			bTimeMs = int.Parse(btimeString);
		}

		int searchTimeMs = (currBoard.IsWhiteToMove ? wTimeMs : bTimeMs)/20;


		int moveTimeIdx = -1;

		if ((moveTimeIdx = Array.IndexOf(cmdArgs, "movetime")) != -1)
		{
			string moveTimeString = cmdArgs[moveTimeIdx+1];

			searchTimeMs = int.Parse(moveTimeString);
		}

		searchTimeMs = Math.Min(searchTimeMs, 15000); // have the bot take no more than 15 sec for every move

		searcher = new(currBoard, searchDepth);
		searcher.Search();

		if (searchTimeMs != 0)
			Task.Delay(searchTimeMs).ContinueWith(task => searcher.EndSearch());
	}

	static void Stop()
	{
		searcher.EndSearch();

		searcher = null;
	}

	public static void RunAndDelegateCommands()
	{
		while (true)
		{	
			string? cmd = Console.ReadLine();

			if (cmd is null) return;

			string[] cmdArgs = cmd.Split();

			switch (cmdArgs[0])
			{
				case "uci":
					Console.WriteLine("id name GianMarco Chess Engine\nid author Carl Furtado\n\nuciok");
					break;
				case "debug":
					// debug mode not supported; do nothing
					break;
				case "isready":
					Console.WriteLine("readyok");
					break;
				case "setoption":
					// options currently unsupported; do nothing
					break;
				case "ucinewgame":
					currBoard = Board.CreateBoardFromFEN(ChessChallenge.Chess.FenUtility.StartPositionFEN);
					break;
				case "position":
					LoadPosition(cmdArgs);
					break;
				case "go":
					Go(cmdArgs);
					break;
				case "staticeval": // not uci, just for debugging evals
					Console.WriteLine(Evaluator.EvalPosition(currBoard));
					break;
				case "stop":
					Stop();
					break;
				case "quit":
					return;

			}
		}
	}
}