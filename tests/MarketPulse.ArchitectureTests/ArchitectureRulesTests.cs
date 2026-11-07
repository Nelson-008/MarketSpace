using System.Reflection;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Modules.Administration;
using MarketPulse.Modules.Identity;
using MarketPulse.Modules.Instruments;
using MarketPulse.Modules.Workspace;
using MarketPulse.SharedKernel.Results;
using Shouldly;

namespace MarketPulse.ArchitectureTests;

/// <summary>Enforces ADR-0001/0002 boundaries by inspecting compiled assemblies.</summary>
public class ArchitectureRulesTests
{
    private static readonly Assembly SharedKernel = typeof(Result).Assembly;
    private static readonly Assembly Abstractions = typeof(IUserContext).Assembly;
    private static readonly Assembly InfrastructureCommon = typeof(ModuleDbContext).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;
    private static readonly Assembly Workers = Assembly.Load("MarketPulse.Workers");

    private static readonly Assembly[] Modules =
    [
        typeof(IdentityModule).Assembly,
        typeof(InstrumentsModule).Assembly,
        typeof(WorkspaceModule).Assembly,
        typeof(AdministrationModule).Assembly,
    ];

    /// <summary>Identity's domain intentionally derives from ASP.NET Core Identity types (ADR-0004).</summary>
    private static readonly Assembly[] ModulesWithFrameworkDomain = [typeof(IdentityModule).Assembly];

    /// <summary>
    /// Spec §3.3/§14: no live order execution in MVP. Any broker/trading SDK appearing as a dependency fails the build.
    /// Extend this list when evaluating vendor SDKs.
    /// </summary>
    private static readonly string[] ForbiddenExecutionSdkFragments = ["KiteConnect", "SmartApi", "AngelOne", "Zerodha", "BrokerExecution", "OrderRouting"];

    public static TheoryData<string> AllMarketPulseAssemblies() =>
        new([.. new[] { SharedKernel, Abstractions, InfrastructureCommon, Api, Workers }.Concat(Modules).Select(a => a.GetName().Name!)]);

    [Fact]
    public void SharedKernel_depends_on_nothing_but_the_base_library()
    {
        References(SharedKernel).ShouldAllBe(n => !n.StartsWith("MarketPulse", StringComparison.Ordinal) && !n.StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_abstractions_depend_only_on_the_shared_kernel()
    {
        var refs = References(Abstractions);

        refs.Where(n => n.StartsWith("MarketPulse", StringComparison.Ordinal)).ShouldBe(["MarketPulse.SharedKernel"]);
        refs.ShouldNotContain(n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) || n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Modules_do_not_reference_each_other_except_through_contracts()
    {
        foreach (var module in Modules)
        {
            var self = module.GetName().Name!;
            References(module)
                .Where(n => n.StartsWith("MarketPulse.Modules.", StringComparison.Ordinal) && n != self && !n.EndsWith(".Contracts", StringComparison.Ordinal))
                .ShouldBeEmpty($"{self} references another module directly.");
        }
    }

    [Fact]
    public void Building_blocks_do_not_reference_modules_or_hosts()
    {
        foreach (var assembly in new[] { SharedKernel, Abstractions, InfrastructureCommon })
        {
            References(assembly).ShouldNotContain(n => n.StartsWith("MarketPulse.Modules", StringComparison.Ordinal) || n == "MarketPulse.Api" || n == "MarketPulse.Workers");
        }
    }

    [Fact]
    public void Workers_host_does_not_reference_the_api_host()
    {
        References(Workers).ShouldNotContain("MarketPulse.Api");
    }

    [Fact]
    public void Domain_types_do_not_depend_on_persistence_web_or_infrastructure()
    {
        string[] forbidden = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "MarketPulse.Infrastructure"];
        var violations = new List<string>();

        foreach (var module in Modules.Except(ModulesWithFrameworkDomain))
        {
            foreach (var type in module.GetTypes().Where(t => t.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true))
            {
                violations.AddRange(DependenciesOf(type)
                    .Where(d => forbidden.Any(f => d.Namespace?.StartsWith(f, StringComparison.Ordinal) == true))
                    .Select(d => $"{type.FullName} -> {d.FullName}"));
            }
        }

        violations.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(AllMarketPulseAssemblies))]
    public void No_assembly_references_a_broker_or_order_execution_sdk(string assemblyName)
    {
        References(Assembly.Load(assemblyName))
            .ShouldNotContain(n => ForbiddenExecutionSdkFragments.Any(f => n.Contains(f, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

    private static IEnumerable<Type> DependenciesOf(Type type)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var direct = new List<Type?> { type.BaseType };
        direct.AddRange(type.GetInterfaces());
        direct.AddRange(type.GetFields(all).Select(f => f.FieldType));
        direct.AddRange(type.GetProperties(all).Select(p => p.PropertyType));
        direct.AddRange(type.GetMethods(all).SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType)));
        return direct.Where(t => t is not null).SelectMany(t => Expand(t!)).Distinct();
    }

    private static IEnumerable<Type> Expand(Type type)
    {
        yield return type;
        if (type.HasElementType)
        {
            foreach (var t in Expand(type.GetElementType()!))
            {
                yield return t;
            }
        }

        if (type.IsGenericType)
        {
            foreach (var t in type.GetGenericArguments().SelectMany(Expand))
            {
                yield return t;
            }
        }
    }
}
