using System.Reflection;
using BatchProgram = AspNetCoreSkeletonApplication.Batch.Program;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

internal static partial class ConventionScan
{
    static partial void AddBatchAssembly(List<Assembly> assemblies) =>
        assemblies.Add(typeof(BatchProgram).Assembly);
}
