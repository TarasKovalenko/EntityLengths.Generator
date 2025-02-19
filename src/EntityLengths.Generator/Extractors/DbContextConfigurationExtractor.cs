using System;
using System.Collections.Generic;
using System.Linq;
using EntityLengths.Generator.Core;
using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Extractors;

internal class DbContextConfigurationExtractor : IPropertyLengthExtractor
{
    public EntityTypeInfo? ExtractPropertyLengths(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax);

        if (!IsDbContextClass(classSymbol))
        {
            return null;
        }

        // Find OnModelCreating method
        var onModelCreating = classSyntax
            .Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m =>
                string.Equals(
                    m.Identifier.Text,
                    Constants.OnModelCreatingMethod,
                    StringComparison.Ordinal
                )
            );

        if (onModelCreating == null)
        {
            return null;
        }

        // Find Entity<T> calls and their subsequent configurations
        var entityCalls = onModelCreating
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsEntityCall)
            .ToList();

        foreach (var entityCall in entityCalls)
        {
            if (
                TryGetEntityTypeAndConfigurations(
                    entityCall,
                    semanticModel,
                    onModelCreating,
                    out var entityType,
                    out var configs
                )
            )
            {
                // Return the EntityTypeInfo for this entity type
                return new EntityTypeInfo(entityType, configs);
            }
        }

        return null;
    }

    private static bool IsDbContextClass(ISymbol? classSymbol)
    {
        if (classSymbol is not ITypeSymbol typeSymbol)
        {
            return false;
        }

        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            if (
                string.Equals(baseType.Name, Constants.DbContextClass, StringComparison.Ordinal)
                && string.Equals(
                    baseType.ContainingNamespace.ToString(),
                    Constants.EfNamespace,
                    StringComparison.Ordinal
                )
            )
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsEntityCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        return string.Equals(
            memberAccess.Name.Identifier.Text,
            Constants.Entity,
            StringComparison.Ordinal
        );
    }

    private static bool TryGetEntityTypeAndConfigurations(
        InvocationExpressionSyntax entityCall,
        SemanticModel semanticModel,
        MethodDeclarationSyntax methodDeclaration,
        out ITypeSymbol entityType,
        out List<PropertyMaxLength> configurations
    )
    {
        entityType = null!;
        configurations = new List<PropertyMaxLength>();

        // Get the type argument from Entity<T>
        var typeArgument = entityCall
            .DescendantNodes()
            .OfType<TypeArgumentListSyntax>()
            .FirstOrDefault()
            ?.Arguments.FirstOrDefault();

        if (typeArgument is null)
        {
            return false;
        }

        var typeInfo = semanticModel.GetTypeInfo(typeArgument);
        if (typeInfo.Type is null)
        {
            return false;
        }

        entityType = typeInfo.Type;

        // Handle lambda style configuration
        HandleLambdaConfiguration(entityCall, configurations);

        // Find all Property() configurations for this entity
        HandleFluentConfiguration(semanticModel, methodDeclaration, entityType, configurations);

        return configurations.Any();
    }

    private static void HandleFluentConfiguration(
        SemanticModel semanticModel,
        MethodDeclarationSyntax methodDeclaration,
        ITypeSymbol entityType,
        List<PropertyMaxLength> configurations
    )
    {
        var allPropertyConfigs = methodDeclaration
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i =>
                i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(
                    ma.Name.Identifier.Text,
                    Constants.Property,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        foreach (var propertyConfig in allPropertyConfigs)
        {
            // Check if this Property() belongs to our entity
            var relatedEntity = FindRelatedEntityCall(propertyConfig);
            if (relatedEntity is null)
                continue;

            var relatedTypeArgument = relatedEntity
                .DescendantNodes()
                .OfType<TypeArgumentListSyntax>()
                .FirstOrDefault()
                ?.Arguments.FirstOrDefault();

            if (relatedTypeArgument is null)
                continue;

            var relatedTypeInfo = semanticModel.GetTypeInfo(relatedTypeArgument);
            if (relatedTypeInfo.Type is null)
                continue;

            if (SymbolEqualityComparer.Default.Equals(relatedTypeInfo.Type, entityType))
            {
                ProcessPropertyConfiguration(propertyConfig, configurations);
            }
        }
    }

    private static void HandleLambdaConfiguration(
        InvocationExpressionSyntax entityCall,
        List<PropertyMaxLength> configurations
    )
    {
        if (
            entityCall.ArgumentList.Arguments.FirstOrDefault()?.Expression is LambdaExpressionSyntax
            {
                Body: BlockSyntax block
            }
        )
        {
            foreach (var statement in block.Statements)
            {
                if (
                    statement is ExpressionStatementSyntax
                    {
                        Expression: InvocationExpressionSyntax invocation
                    }
                )
                {
                    ProcessPropertyConfiguration(invocation, configurations);
                }
            }
        }
    }

    private static InvocationExpressionSyntax? FindRelatedEntityCall(
        InvocationExpressionSyntax propertyCall
    )
    {
        // First check ancestors (works for lambda style)
        var ancestor = propertyCall
            .Ancestors()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(IsEntityCall);

        if (ancestor != null)
        {
            return ancestor;
        }

        // For fluent style, look in the same expression statement
        var statement = propertyCall
            .Ancestors()
            .OfType<ExpressionStatementSyntax>()
            .FirstOrDefault();
        if (statement is not null)
        {
            return statement
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(IsEntityCall);
        }

        return null;
    }

    private static void ProcessPropertyConfiguration(
        InvocationExpressionSyntax propertyCall,
        List<PropertyMaxLength> configurations
    )
    {
        if (
            propertyCall.ArgumentList.Arguments.FirstOrDefault()?.Expression
            is not LambdaExpressionSyntax lambda
        )
        {
            return;
        }

        var propertyAccess = lambda.Body as MemberAccessExpressionSyntax;
        var propertyName = propertyAccess?.Name.Identifier.Text ?? string.Empty;

        if (string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        var current = propertyCall;
        while (current is not null)
        {
            if (
                current.Parent
                    is MemberAccessExpressionSyntax
                    {
                        Parent: InvocationExpressionSyntax parentInvocation
                    } parentAccess
                && string.Equals(
                    parentAccess.Name.Identifier.Text,
                    Constants.HasMaxLengthMethod,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var lengthArg = parentInvocation
                    .ArgumentList.Arguments.FirstOrDefault()
                    ?.Expression.ToString();
                if (int.TryParse(lengthArg, out var length))
                {
                    configurations.Add(new PropertyMaxLength(propertyName, length));
                }

                break;
            }

            current = current.Parent?.Parent as InvocationExpressionSyntax;
        }
    }
}
