using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;

namespace EntityLengths.Generator.Tests;

public class EntityMaxLengthGeneratorTests
{
    private readonly CSharpGeneratorDriver _driver;

    public EntityMaxLengthGeneratorTests()
    {
        var generator = new EntityMaxLengthGenerator();
        _driver = CSharpGeneratorDriver.Create(generator);
    }

    private static class CompilationBuilder
    {
        public static CSharpCompilation CreateCompilation(
            string sourceCode,
            bool includeDataAnnotations = false,
            bool includeEntityFrameworkCore = false
        )
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            };

            if (includeDataAnnotations)
            {
                var runtimePath = Path.Combine(
                    RuntimeEnvironment.GetRuntimeDirectory(),
                    "System.Runtime.dll"
                );
                references.Add(MetadataReference.CreateFromFile(runtimePath));
                references.Add(
                    MetadataReference.CreateFromFile(typeof(MaxLengthAttribute).Assembly.Location)
                );
            }

            if (includeEntityFrameworkCore)
            {
                references.Add(
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                );
                references.Add(
                    MetadataReference.CreateFromFile(typeof(EntityTypeBuilder).Assembly.Location)
                );
            }

            return CSharpCompilation.Create(
                nameof(EntityMaxLengthGeneratorTests),
                [CSharpSyntaxTree.ParseText(sourceCode)],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }
    }

    private static class GeneratorTestHelper
    {
        private static string GetGeneratedOutput(GeneratorDriverRunResult runResult)
        {
            var syntaxTree = runResult.GeneratedTrees.Single(t =>
                t.FilePath.EndsWith("EntityLengths.g.cs")
            );

            return syntaxTree.GetText().ToString();
        }

        public static void AssertGeneratedOutput(
            GeneratorDriverRunResult runResult,
            string expectedOutput
        )
        {
            var actualOutput = GetGeneratedOutput(runResult);

            Assert.Equal(expectedOutput.ReplaceLineEndings(), actualOutput.ReplaceLineEndings());
        }
    }

    [Fact]
    public void Generate_EntityLength_With_FluentApi()
    {
        // Arrange
        var compilation = CompilationBuilder.CreateCompilation(TestSources.FluentApiEntity);

        // Act
        var result = _driver.RunGenerators(compilation).GetRunResult();

        // Assert
        GeneratorTestHelper.AssertGeneratedOutput(result, TestSources.ExpectedOutput);
    }

    [Fact]
    public void Generate_EntityLength_With_DataAnnotations()
    {
        // Arrange
        var compilation = CompilationBuilder.CreateCompilation(
            TestSources.DataAnnotationsEntity,
            includeDataAnnotations: true
        );

        // Act
        var result = _driver.RunGenerators(compilation).GetRunResult();

        // Assert
        GeneratorTestHelper.AssertGeneratedOutput(result, TestSources.ExpectedOutput);
    }

    [Fact]
    public void Generate_EntityLength_With_ColumnTypeDefinition()
    {
        // Arrange
        var compilation = CompilationBuilder.CreateCompilation(
            TestSources.ColumnTypeDefinitionEntity,
            includeDataAnnotations: true,
            includeEntityFrameworkCore: true
        );

        // Act
        var result = _driver.RunGenerators(compilation).GetRunResult();

        // Assert
        GeneratorTestHelper.AssertGeneratedOutput(result, TestSources.ExpectedOutput);
    }

    [Fact]
    public void Generate_EntityLength_With_DbContext()
    {
        // Arrange
        var compilation = CompilationBuilder.CreateCompilation(
            TestSources.DbContextEntity,
            includeDataAnnotations: true,
            includeEntityFrameworkCore: true
        );

        // Act
        var result = _driver.RunGenerators(compilation).GetRunResult();

        // Assert
        GeneratorTestHelper.AssertGeneratedOutput(result, TestSources.ExpectedOutput);
    }

    [Fact]
    public void Generate_EntityLength_With_DataAnnotations_StringLength()
    {
        // Arrange
        var compilation = CompilationBuilder.CreateCompilation(
            TestSources.DataAnnotationsStringLengthEntity,
            includeDataAnnotations: true
        );

        // Act
        var result = _driver.RunGenerators(compilation).GetRunResult();

        // Assert
        GeneratorTestHelper.AssertGeneratedOutput(result, TestSources.ExpectedOutput);
    }
}
