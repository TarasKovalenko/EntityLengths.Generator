using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EntityLengths.Generator.Configuration;
using EntityLengths.Generator.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace EntityLengths.Generator.Tests.Configuration;

public class EntityLengthsOptionsProviderTests
{
    [Fact]
    public void GetOptions_Should_Return_Default_Options_When_No_Attribute_Present()
    {
        // Arrange
        var provider = new EntityLengthsOptionsProvider();
        var compilation = CreateCompilation("");

        // Act
        var options = provider.GetOptions(compilation);

        // Assert
        Assert.Equal("EntityLengths", options.GeneratedClassName);
        Assert.Equal("Length", options.LengthSuffix);
        Assert.False(options.GenerateDocumentation);
        Assert.Null(options.Namespace);
    }

    [Fact]
    public void GetOptions_Should_Use_Custom_Default_Options()
    {
        // Arrange
        var defaultOptions = new EntityLengthsGeneratorOptions
        {
            GeneratedClassName = "CustomClass",
            LengthSuffix = "MaxLength",
            GenerateDocumentation = true,
            Namespace = "Custom.Namespace",
        };

        var provider = new EntityLengthsOptionsProvider(defaultOptions);
        var compilation = CreateCompilation("");

        // Act
        var options = provider.GetOptions(compilation);

        // Assert
        Assert.Equal("CustomClass", options.GeneratedClassName);
        Assert.Equal("MaxLength", options.LengthSuffix);
        Assert.True(options.GenerateDocumentation);
        Assert.Equal("Custom.Namespace", options.Namespace);
    }

    [Fact]
    public void GetOptions_Should_Read_Options_From_Attribute()
    {
        // Arrange
        var provider = new EntityLengthsOptionsProvider();
        var compilation = CreateCompilation(
            @"
using EntityLengths.Generator.Configuration;

[assembly: EntityLengthsGenerator(
    GeneratedClassName = ""CustomName"",
    LengthSuffix = ""Max"",
    GenerateDocumentation = true,
    Namespace = ""My.Namespace"")]
"
        );

        // Act
        var options = provider.GetOptions(compilation);

        // Assert
        Assert.Equal("CustomName", options.GeneratedClassName);
        Assert.Equal("Max", options.LengthSuffix);
        Assert.True(options.GenerateDocumentation);
        Assert.Equal("My.Namespace", options.Namespace);
    }

    [Fact]
    public void GetOptions_Should_Use_Default_When_Attribute_Value_Not_Set()
    {
        // Arrange
        var provider = new EntityLengthsOptionsProvider();
        var compilation = CreateCompilation(
            @"
    using EntityLengths.Generator.Configuration;
    
    [assembly: EntityLengthsGenerator(GeneratedClassName = ""Constants"")]
    "
        );

        // Act
        var options = provider.GetOptions(compilation);

        // Assert
        Assert.Equal("Constants", options.GeneratedClassName);
        // Default values
        Assert.Equal("Length", options.LengthSuffix);
        Assert.False(options.GenerateDocumentation);
        Assert.Null(options.Namespace);
        Assert.Empty(options.ScanningOptions.IncludeNamespaces);
        Assert.Empty(options.ScanningOptions.ExcludeNamespaces);
        Assert.True(options.ScanningOptions.ScanNestedNamespaces);
        Assert.Null(options.ScanningOptions.EntitySuffix);
    }

    [Fact]
    public void GetOptions_Should_Read_All_Set_Values()
    {
        // Arrange
        var provider = new EntityLengthsOptionsProvider();
        var compilation = CreateCompilation(
            @"
    using EntityLengths.Generator.Configuration;
    
    [assembly: EntityLengthsGenerator(
        GeneratedClassName = ""Constants"",
        GenerateDocumentation = true,
        LengthSuffix = ""MaxLength"",
        Namespace = ""MyApp.Constants"",
        IncludeNamespaces = new[] { ""MyApp.Domain"", ""MyApp.Core"" },
        ExcludeNamespaces = new[] { ""MyApp.Tests"" },
        ScanNestedNamespaces = false,
        ScanEntitySuffix = ""Entity""
    )]"
        );

        // Act
        var options = provider.GetOptions(compilation);

        // Assert
        Assert.Equal("Constants", options.GeneratedClassName);
        Assert.Equal("MaxLength", options.LengthSuffix);
        Assert.True(options.GenerateDocumentation);
        Assert.Equal("MyApp.Constants", options.Namespace);
        Assert.Equal(2, options.ScanningOptions.IncludeNamespaces.Count);
        Assert.Contains("MyApp.Domain", options.ScanningOptions.IncludeNamespaces);
        Assert.Contains("MyApp.Core", options.ScanningOptions.IncludeNamespaces);
        Assert.Single(options.ScanningOptions.ExcludeNamespaces);
        Assert.Contains("MyApp.Tests", options.ScanningOptions.ExcludeNamespaces);
        Assert.False(options.ScanningOptions.ScanNestedNamespaces);
        Assert.Equal("Entity", options.ScanningOptions.EntitySuffix);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Get runtime directory
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();

        // Find netstandard.dll
        var netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (!File.Exists(netstandardPath))
        {
            // Try to find it in the test assembly's directory
            var assemblyPath = typeof(EntityLengthsOptionsProviderTests).Assembly.Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
            netstandardPath = Path.Combine(assemblyDirectory, "netstandard.dll");
        }

        var references = new List<MetadataReference>
        {
            // Core runtime assemblies
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(netstandardPath),
            // Our attribute assembly
            MetadataReference.CreateFromFile(
                typeof(EntityLengthsGeneratorAttribute).Assembly.Location
            ),
        };

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable
        );

        var compilation = CSharpCompilation.Create(
            assemblyName: nameof(EntityLengthsOptionsProviderTests),
            syntaxTrees: [syntaxTree],
            references: references,
            options: compilationOptions
        );

        // Check for compilation errors
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Any())
        {
            // Print full error details for debugging
            var errorMessage = string.Join(
                Environment.NewLine,
                errors.Select(e => $"{e.GetMessage()} at {e.Location}")
            );

            throw new Exception($"Compilation errors: {errorMessage}");
        }

        return compilation;
    }
}
