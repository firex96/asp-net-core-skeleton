using System.Reflection;
using InternalApiProgram = AspNetCoreSkeletonApplication.InternalApi.Program;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

internal static partial class ConventionScan
{
    static partial void AddInternalApiAssembly(List<Assembly> assemblies) =>
        assemblies.Add(typeof(InternalApiProgram).Assembly);
}
