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

        if (typeArgument == null)
        {
            return false;
        }

        var typeInfo = semanticModel.GetTypeInfo(typeArgument);
        if (typeInfo.Type == null)
        {
            return false;
        }

        entityType = typeInfo.Type;

        // Get the chain of method calls after Entity<T>()
        var chainedCalls = new List<InvocationExpressionSyntax>();
        var current = entityCall.Parent;
        while (current != null)
        {
            if (current is InvocationExpressionSyntax inv)
            {
                chainedCalls.Add(inv);
            }

            current = current.Parent;
        }

        // Find Property() calls
        foreach (var call in chainedCalls)
        {
            if (call.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                continue;
            }

            if (
                !string.Equals(
                    memberAccess.Name.Identifier.Text,
                    Constants.Property,
                    StringComparison.Ordinal
                )
            )
            {
                continue;
            }

            if (
                call.ArgumentList.Arguments.FirstOrDefault()?.Expression
                is not LambdaExpressionSyntax lambda
            )
            {
                continue;
            }

            // Get property name from lambda
            var propertyAccess = lambda.Body as MemberAccessExpressionSyntax;
            var propertyName = propertyAccess?.Name.Identifier.Text ?? string.Empty;

            if (!string.IsNullOrEmpty(propertyName))
            {
                // Look for HasMaxLength in the following chain
                var nextInChain = call.Parent;
                while (nextInChain != null)
                {
                    if (
                        nextInChain
                            is InvocationExpressionSyntax
                            {
                                Expression: MemberAccessExpressionSyntax nextMember
                            } nextInv
                        && string.Equals(
                            nextMember.Name.Identifier.Text,
                            Constants.HasMaxLengthMethod,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        var lengthArg = nextInv.ArgumentList.Arguments.FirstOrDefault()?.ToString();
                        if (int.TryParse(lengthArg, out var length))
                        {
                            configurations.Add(new PropertyMaxLength(propertyName, length));
                        }

                        break;
                    }

                    nextInChain = nextInChain.Parent;
                }
            }
        }

        return configurations.Any();
    }
}
