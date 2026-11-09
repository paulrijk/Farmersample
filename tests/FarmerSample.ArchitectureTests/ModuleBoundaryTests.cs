using System.Reflection;
using FarmerSample.Core.Persistence;
using NetArchTest.Rules;

namespace FarmerSample.ArchitectureTests;

/// <summary>A module may use another module only through its Contracts namespace (architecture §2.1).</summary>
public class ModuleBoundaryTests
{
    private static readonly Assembly Core = typeof(AppDbContext).Assembly;
    private const string Root = "FarmerSample.Core.Modules";

    public static TheoryData<string> Modules()
    {
        var data = new TheoryData<string>();
        foreach (var m in Core.GetTypes().Select(t => t.Namespace).OfType<string>()
                     .Where(n => n.StartsWith(Root + ".")).Select(n => n.Split('.')[3]).Distinct())
            data.Add(m);
        return data;
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_only_depends_on_other_modules_contracts(string module)
    {
        var others = Core.GetTypes().Select(t => t.Namespace).OfType<string>()
            .Where(n => n.StartsWith(Root + ".") && n.Split('.')[3] != module && !n.Split('.').Contains("Contracts"))
            .Distinct().ToArray();
        if (others.Length == 0) return;

        var result = Types.InAssembly(Core)
            .That().ResideInNamespace($"{Root}.{module}")
            .ShouldNot().HaveDependencyOnAny(others)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"{module} reaches into other modules' internals: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Contracts_do_not_depend_on_persistence()
    {
        var result = Types.InAssembly(Core).That().ResideInNamespaceEndingWith(".Contracts")
            .ShouldNot().HaveDependencyOn("FarmerSample.Core.Persistence").GetResult();
        Assert.True(result.IsSuccessful);
    }
}
