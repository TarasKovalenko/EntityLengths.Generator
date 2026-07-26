using System.Collections.Immutable;
using System.Linq;
using EntityLengths.Generator.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace EntityLengths.Generator.Tests;

public class IncrementalCachingTests
{
    private const string Source =
        @"
using System.ComponentModel.DataAnnotations;

namespace TestNamespace;

public class User
{
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}";

    [Fact]
    public void Reuses_Cached_Output_When_An_Unrelated_File_Changes()
    {
        var compilation = TestCompilation.Create(Source);

        var driver = CSharpGeneratorDriver
            .Create(
                generators: ImmutableArray.Create(
                    new EntityMaxLengthGenerator().AsSourceGenerator()
                ),
                additionalTexts: null,
                parseOptions: null,
                optionsProvider: null,
                driverOptions: new GeneratorDriverOptions(
                    IncrementalGeneratorOutputKind.None,
                    trackIncrementalGeneratorSteps: true
                )
            )
            .RunGenerators(compilation);

        // An edit that cannot affect any length must not re-run the emit stage.
        var edited = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("// nothing to do with entities")
        );

        var result = driver.RunGenerators(edited).GetRunResult().Results.Single();

        var outputReasons = result
            .TrackedOutputSteps.SelectMany(step => step.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToList();

        Assert.NotEmpty(outputReasons);
        Assert.All(
            outputReasons,
            reason =>
                Assert.True(
                    reason
                        is IncrementalStepRunReason.Cached
                            or IncrementalStepRunReason.Unchanged,
                    $"Expected the output to be cached but it was {reason}"
                )
        );
    }
}
