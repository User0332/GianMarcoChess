namespace GianMarco;
class Program
{
	static int Main(string[] args)
	{
		if (args.Length == 3)
		{
			Search.Utils.MoveOrdering.CaptureBonus = short.Parse(args[0]);
			Search.Utils.MoveOrdering.PromotionBonus = short.Parse(args[1]);
			Search.Utils.MoveOrdering.CastleBonus = short.Parse(args[2]);
		}

		UCI.PreBuiltInterpreter.RunAndDelegateCommands();

		return 0;
	}
}