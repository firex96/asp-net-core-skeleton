using System.Xml.Linq;

namespace AspNetCoreSkeletonApplication.E2ETests.Support;

/// <summary>
/// 레포의 SPA 하나 — 폴더와 정적 빌드 출력. 목록은 <c>src/spa</c> 아래의 esproj 로 정하고, 출력 폴더는 esproj 의 <c>BuildOutputFolder</c> 를 읽는다.
/// 템플릿에서 SPA 하나만 고르면 그 하나만 돈다.
/// </summary>
public sealed record SpaClient(string Name, string Directory, string OutputDirectory)
{
    public static IReadOnlyList<SpaClient> All { get; } = Discover();

    /// <summary><c>[MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]</c> — 시나리오를 SPA 마다 돌린다.</summary>
    public static TheoryData<string> Names => new(All.Select(client => client.Name));

    public static SpaClient Named(string name) => All.Single(client => client.Name == name);

    private static List<SpaClient> Discover()
    {
        var spaRoot = Path.Combine(RepositoryRoot(), "src", "spa");
        return System.IO.Directory.EnumerateFiles(spaRoot, "*.esproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains("node_modules", StringComparison.Ordinal))
            .Select(path =>
            {
                var directory = Path.GetDirectoryName(path)!;
                var output = XDocument.Load(path).Descendants("BuildOutputFolder").Single().Value
                    .Replace("$(MSBuildProjectDirectory)", directory, StringComparison.Ordinal)
                    .Replace('\\', Path.DirectorySeparatorChar);
                // 폴더 이름의 마지막 조각 — asp-net-core-skeleton-application.client.angular → angular
                return new SpaClient(Path.GetFileName(directory).Split('.')[^1], directory, Path.TrimEndingDirectorySeparator(output));
            })
            .OrderBy(client => client.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (System.IO.Directory.Exists(Path.Combine(directory.FullName, "src", "spa")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"{AppContext.BaseDirectory} 위로 src/spa 가 있는 폴더를 찾지 못했다");
    }
}
