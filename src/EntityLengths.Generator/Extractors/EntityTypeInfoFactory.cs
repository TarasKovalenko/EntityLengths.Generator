using System.Collections.Generic;
using System.Collections.Immutable;
using EntityLengths.Generator.Core;
using EntityLengths.Generator.Extensions;
using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Extractors;

internal static class EntityTypeInfoFactory
{
    /// <summary>
    /// Builds the symbol-free pipeline model for a single entity. Yields nothing when no lengths were
    /// found, so the entry drops out of the pipeline.
    /// </summary>
    public static EquatableArray<EntityTypeInfo> Create(
        ITypeSymbol entityType,
        TypeDeclarationSyntax declaringNode,
        SemanticModel semanticModel,
        IReadOnlyCollection<PropertyMaxLength> properties
    ) =>
        properties.Count == 0
            ? EquatableArray<EntityTypeInfo>.Empty
            : new EquatableArray<EntityTypeInfo>(
                ImmutableArray.Create(
                    CreateSingle(entityType, declaringNode, semanticModel, properties)
                )
            );

    /// <summary>
    /// Builds the pipeline model for a node that configures several entities, such as a DbContext.
    /// </summary>
    public static EquatableArray<EntityTypeInfo> CreateMany(
        IEnumerable<KeyValuePair<ITypeSymbol, IReadOnlyCollection<PropertyMaxLength>>> entities,
        TypeDeclarationSyntax declaringNode,
        SemanticModel semanticModel
    )
    {
        var builder = ImmutableArray.CreateBuilder<EntityTypeInfo>();

        foreach (var entity in entities)
        {
            if (entity.Value.Count == 0)
            {
                continue;
            }

            builder.Add(CreateSingle(entity.Key, declaringNode, semanticModel, entity.Value));
        }

        return new EquatableArray<EntityTypeInfo>(builder.ToImmutable());
    }

    private static EntityTypeInfo CreateSingle(
        ITypeSymbol entityType,
        TypeDeclarationSyntax declaringNode,
        SemanticModel semanticModel,
        IReadOnlyCollection<PropertyMaxLength> properties
    )
    {
        var declaringSymbol = semanticModel.GetDeclaredSymbol(declaringNode);

        return new EntityTypeInfo(
            EntityName: entityType.Name,
            EntityNamespace: entityType.GetNamespaceName(),
            DeclaringNamespace: declaringSymbol?.GetNamespaceName() ?? string.Empty,
            DeclaringTypeName: declaringNode.Identifier.Text,
            StringProperties: properties.ToEquatableArray()
        );
    }
}
