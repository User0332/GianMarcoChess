using BenchmarkDotNet.Running;
using GianMarco.Testing;

namespace GianMarco;
class Program
{
	static int Main(string[] args)
	{
		if (args.Length == 1 && args[0] == "benchmark")
		{
			BenchmarkRunner.Run<EvaluationBenchmarks>();
			return 0;
		}

		UCI.PreBuiltInterpreter.RunAndDelegateCommands();

		return 0;
	}
}