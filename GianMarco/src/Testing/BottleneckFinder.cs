namespace GianMarco.Optimization;

public enum EvalFunc : byte
{
	KingSafety,
	Pawn,
	Outpost,
	Positional,
	MaterialCount,
	CheckEndgame
}

public static class BottleneckFinder
{
	static readonly ulong[] calls = new ulong[6];
	static readonly ulong[] runtimes = new ulong[6];	
	public static void LogRuntime(EvalFunc func, ulong runtime)
	{
		calls[(int) func]+=1;
		runtimes[(int) func]+=runtime;
	}

	public static void PrintResults()
	{
		for (int i = 0; i < 5; i++)
		{
			Console.WriteLine($"Func: {(EvalFunc) i}");
			Console.WriteLine($"Calls: {calls[i]} Total Runtime: {runtimes[i]} ticks");
			Console.WriteLine($"Average Runtime: {runtimes[i]/calls[i]} ticks");
		}
	}
}