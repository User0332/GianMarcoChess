namespace GianMarco.Optimization;

public enum EvalFunc : byte
{
	KingSafety,
	Pawn,
	Outpost,
	Positional,
	MaterialCount
}

public static class BottleneckFinder
{
	static readonly ulong[] calls = new ulong[5];
	static readonly ulong[] runtimes = new ulong[5];	
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