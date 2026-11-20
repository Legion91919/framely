using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace Framely.Tests;

/// <summary>Module boundaries: modules never reference each other (project or assembly) and keep internals internal.</summary>
public class ArchitectureTests
{
    private static readonly Assembly[] Modules =
    [
        typeof(Framely.Media.IPhotoIngest).Assembly,
        typeof(Framely.Graph.ISocialGraph).Assembly,
        typeof(Framely.Feed.IFeedService).Assembly,
        typeof(Framely.Notifications.INotificationSender).Assembly,
    ];

    private static readonly HashSet<string> ModuleNames = Modules.Select(m => m.GetName().Name!).ToHashSet();

    [Fact]
    public void There_are_four_modules_matching_the_adr()
    {
        Assert.Equal(new[] { "Framely.Feed", "Framely.Graph", "Framely.Media", "Framely.Notifications" }, ModuleNames.Order().ToArray());
    }

    [Fact]
    public void No_module_assembly_references_another_module_or_the_api()
    {
        Assert.All(Modules, module =>
        {
            var refs = module.GetReferencedAssemblies().Select(r => r.Name!).ToList();
            var self = module.GetName().Name!;
            Assert.DoesNotContain(refs, r => r != self && (ModuleNames.Contains(r) || r == "Framely.Api"));
        });
    }

    [Fact]
    public void No_module_project_file_references_another_project()
    {
        var root = RepoRoot();
        var projects = Directory.GetFiles(Path.Combine(root, "src", "Modules"), "*.csproj", SearchOption.AllDirectories);

        Assert.Equal(4, projects.Length);
        Assert.All(projects, csproj =>
        {
            var refs = XDocument.Load(csproj).Descendants("ProjectReference").ToList();
            Assert.True(refs.Count == 0, $"{Path.GetFileName(csproj)} must not reference other projects");
        });
    }

    [Fact]
    public void Modules_only_expose_internals_to_the_test_assembly()
    {
        Assert.All(Modules, module =>
        {
            var friends = module.GetCustomAttributes<InternalsVisibleToAttribute>().Select(a => a.AssemblyName).ToList();
            Assert.All(friends, f => Assert.Equal("Framely.Tests", f));
        });
    }

    [Fact]
    public void Types_in_internal_namespaces_are_not_public()
    {
        Assert.All(Modules, module =>
        {
            var leaked = module.GetTypes()
                .Where(t => t.Namespace?.EndsWith(".Internal", StringComparison.Ordinal) == true)
                .Where(t => t.IsPublic || t.IsNestedPublic)
                .Select(t => t.FullName)
                .ToList();
            Assert.Empty(leaked);
        });
    }

    [Fact]
    public void Each_module_keeps_its_implementation_in_an_internal_namespace()
    {
        Assert.All(Modules, module =>
            Assert.Contains(module.GetTypes(), t => t.Namespace?.EndsWith(".Internal", StringComparison.Ordinal) == true && !t.IsPublic));
    }

    private static string RepoRoot([CallerFilePath] string here = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(here)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Framely.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Framely.sln not found above " + here);
    }
}
