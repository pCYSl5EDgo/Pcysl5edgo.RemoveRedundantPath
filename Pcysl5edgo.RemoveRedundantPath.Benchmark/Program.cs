using BenchmarkDotNet.Running;

namespace Pcysl5edgo.RemoveRedundantPath;

public class Program
{
    static void Main(string[] args)
    {
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
    }
}
