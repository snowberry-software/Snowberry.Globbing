using BenchmarkDotNet.Running;

namespace Snowberry.Globbing.Benchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
    }
}
