namespace GianMarco;
class Program
{
	static int Main(string[] args)
	{
		if (args.Length == 3) // for genetic algorithm
		{
			Search.Utils.MoveOrdering.CaptureBonus = int.Parse(args[0]);
			Search.Utils.MoveOrdering.PromotionBonus = int.Parse(args[1]);
			Search.Utils.MoveOrdering.CastleBonus = int.Parse(args[2]);
		}

		UCI.PreBuiltInterpreter.RunAndDelegateCommands();

		return 0;
	}
}