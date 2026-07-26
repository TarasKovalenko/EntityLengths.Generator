using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.InteropServices;
using EntityLengths.Generator.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityLengths.Generator.Tests.Infrastructure;

internal static class TestCompilation
{
    public static CSharpCompilation Create(string sourceCode, string assemblyName = "TestAssembly")
    {
        var runtimePath = Path.Combine(
            RuntimeEnvironment.GetRuntimeDirectory(),
            "System.Runtime.dll"
        );

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(runtimePath),
            MetadataReference.CreateFromFile(typeof(MaxLengthAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityTypeBuilder).Assembly.Location),
            // Needed so [assembly: EntityLengthsGenerator(...)] in test sources binds.
            MetadataReference.CreateFromFile(
                typeof(EntityLengthsGeneratorAttribute).Assembly.Location
            ),
        };

        var netstandardPath = Path.Combine(
            RuntimeEnvironment.GetRuntimeDirectory(),
            "netstandard.dll"
        );
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(sourceCode)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}

/// <summary>
/// Feeds MSBuild properties to the generator the same way the shipped .targets file does.
/// </summary>
internal sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GlobalOptions { get; } =
        new TestAnalyzerConfigOptions(globalOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    private sealed class TestAnalyzerConfigOptions(Dictionary<string, string> options)
        : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value) =>
            options.TryGetValue(key, out value!);

        public override IEnumerable<string> Keys => options.Keys;
    }
}

internal static class TestDriver
{
    public static GeneratorDriverRunResult Run(
        Compilation compilation,
        Dictionary<string, string>? msBuildProperties = null
    )
    {
        var driver = CSharpGeneratorDriver.Create(
            generators: ImmutableArray.Create(
                new EntityMaxLengthGenerator().AsSourceGenerator()
            ),
            additionalTexts: null,
            parseOptions: null,
            optionsProvider: msBuildProperties is null
                ? null
                : new TestAnalyzerConfigOptionsProvider(msBuildProperties)
        );

        return driver.RunGenerators(compilation).GetRunResult();
    }
}
