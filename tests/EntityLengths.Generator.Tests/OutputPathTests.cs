using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EntityLengths.Generator.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Xunit;

namespace EntityLengths.Generator.Tests;

public sealed class OutputPathTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"entitylengths-{Guid.NewGuid():N}"
    );

    private string OutputPath => Path.Combine(_directory, "Generated", "EntityLengths.cs");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Writes_Constants_To_Configured_Path()
    {
        var result = Run(
            new Dictionary<string, string>
            {
                ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
                ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
            }
        );

        Assert.Empty(result.Diagnostics);
        Assert.True(File.Exists(OutputPath));

        var content = File.ReadAllText(OutputPath);
        Assert.Contains("namespace MyApp.Domain;", content);
        Assert.Contains("public const int NameLength = 50;", content);
    }

    [Fact]
    public void Does_Not_Add_Source_To_Current_Compilation_When_Path_Configured()
    {
        var result = Run(
            new Dictionary<string, string>
            {
                ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
                ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
            }
        );

        // The other project compiles the written file, so emitting the same type here too would
        // produce it in two assemblies.
        Assert.Empty(result.GeneratedTrees);
    }

    [Fact]
    public void Adds_Source_And_Writes_File_When_In_Project_Output_Requested()
    {
        var result = Run(
            new Dictionary<string, string>
            {
                ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
                ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
                ["build_property.EntityLengthsEmitInProjectOutput"] = "true",
            }
        );

        Assert.Single(result.GeneratedTrees);
        Assert.True(File.Exists(OutputPath));
    }

    [Fact]
    public void Adds_Source_Only_When_No_Path_Configured()
    {
        var result = Run(new Dictionary<string, string>());

        Assert.Single(result.GeneratedTrees);
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void Resolves_Relative_Path_Against_Project_Directory()
    {
        var result = Run(
            new Dictionary<string, string>
            {
                ["build_property.EntityLengthsOutputPath"] = Path.Combine(
                    "Generated",
                    "EntityLengths.cs"
                ),
                ["build_property.ProjectDir"] = _directory,
                ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
            }
        );

        Assert.Empty(result.Diagnostics);
        Assert.True(File.Exists(OutputPath));
    }

    [Fact]
    public void Reports_ELG0002_And_Falls_Back_To_Assembly_Name_When_Namespace_Missing()
    {
        var result = Run(
            new Dictionary<string, string>
            {
                ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
            }
        );

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("ELG0002", diagnostic.Id);
        Assert.Contains("namespace TestAssembly;", File.ReadAllText(OutputPath));
    }

    [Fact]
    public void Uses_Attribute_Namespace_When_Output_Namespace_Missing()
    {
        var source =
            @"
using System.ComponentModel.DataAnnotations;
using EntityLengths.Generator.Configuration;

[assembly: EntityLengthsGenerator(Namespace = ""MyApp.Domain"")]

namespace TestNamespace;

public class User
{
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}";

        var result = TestDriver.Run(
            TestCompilation.Create(source),
            new Dictionary<string, string>
            {
                ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
            }
        );

        Assert.Empty(result.Diagnostics);
        Assert.Contains("namespace MyApp.Domain;", File.ReadAllText(OutputPath));
    }

    [Fact]
    public void Leaves_File_Untouched_When_Content_Is_Unchanged()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
            ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
        };

        Run(properties);
        var firstWrite = File.GetLastWriteTimeUtc(OutputPath);

        // The IDE runs the generator on every keystroke, so an unchanged result must not touch the file.
        File.SetLastWriteTimeUtc(OutputPath, firstWrite.AddDays(-1));
        var stamped = File.GetLastWriteTimeUtc(OutputPath);

        Run(properties);

        Assert.Equal(stamped, File.GetLastWriteTimeUtc(OutputPath));
    }

    [Fact]
    public void Rewrites_File_When_Content_Changes()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
            ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
        };

        Run(properties);
        Assert.Contains("NameLength = 50", File.ReadAllText(OutputPath));

        TestDriver.Run(
            TestCompilation.Create(EntityWithLength(120)),
            properties
        );

        Assert.Contains("NameLength = 120", File.ReadAllText(OutputPath));
    }

    [Fact]
    public void Reports_ELG0001_Instead_Of_Throwing_When_Path_Is_Unusable()
    {
        // A file where a directory is expected makes directory creation fail.
        Directory.CreateDirectory(_directory);
        var blocker = Path.Combine(_directory, "Generated");
        File.WriteAllText(blocker, "not a directory");

        var result = Run(
            new Dictionary<string, string>
            {
                ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
                ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
            }
        );

        Assert.Contains(result.Diagnostics, d => d.Id == "ELG0001");
        Assert.Contains(
            result.Diagnostics,
            d => d.Severity == DiagnosticSeverity.Warning && d.Id == "ELG0001"
        );
    }

    [Fact]
    public void Concurrent_Writes_To_The_Same_Path_Succeed()
    {
        // What a multi-targeted project does: one inner build per target framework, in parallel, all
        // writing the same output file.
        var results = new GeneratorDriverRunResult[8];

        Parallel.For(
            0,
            results.Length,
            i =>
                results[i] = TestDriver.Run(
                    TestCompilation.Create(EntityWithLength(50)),
                    new Dictionary<string, string>
                    {
                        ["build_property.EntityLengthsResolvedOutputPath"] = OutputPath,
                        ["build_property.EntityLengthsOutputNamespace"] = "MyApp.Domain",
                    }
                )
        );

        Assert.All(results, result => Assert.Empty(result.Diagnostics));
        Assert.Contains("NameLength = 50", File.ReadAllText(OutputPath));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(OutputPath)!, "*.tmp"));
    }

    private GeneratorDriverRunResult Run(Dictionary<string, string> properties) =>
        TestDriver.Run(TestCompilation.Create(EntityWithLength(50)), properties);

    private static string EntityWithLength(int length) =>
        $@"
using System.ComponentModel.DataAnnotations;

namespace TestNamespace;

public class User
{{
    [MaxLength({length})]
    public string Name {{ get; set; }} = string.Empty;
}}";
}
