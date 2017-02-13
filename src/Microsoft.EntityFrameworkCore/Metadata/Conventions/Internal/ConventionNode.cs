// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal
{
    public partial class ConventionDispatcher
    {
        private abstract class ConventionNode
        {
            public abstract ConventionNode Accept(ConventionVisitor visitor);
        }

        private class ConventionScope : ConventionNode
        {
            private readonly List<ConventionNode> _children;
            private bool _readonly;

            public ConventionScope(ConventionScope parent, List<ConventionNode> children)
            {
                Parent = parent;
                _children = children ?? new List<ConventionNode>();
            }

            public ConventionScope Parent { [DebuggerStepThrough] get; }

            public IReadOnlyList<ConventionNode> Children
            {
                [DebuggerStepThrough] get { return _children; }
            }

            public int GetLeafCount()
            {
                var scopesToVisit = new Queue<ConventionScope>();
                scopesToVisit.Enqueue(this);
                var leafCount = 0;
                while (scopesToVisit.Count > 0)
                {
                    var scope = scopesToVisit.Dequeue();
                    foreach (var conventionNode in scope.Children)
                    {
                        var nextScope = conventionNode as ConventionScope;
                        if (nextScope != null)
                        {
                            scopesToVisit.Enqueue(nextScope);
                        }
                        else
                        {
                            leafCount++;
                        }
                    }
                }

                return leafCount;
            }

            public void Add(ConventionNode node)
            {
                if (_readonly)
                {
                    throw new InvalidOperationException();
                }
                _children.Add(node);
            }

            public void MakeReadonly() => _readonly = true;

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitConventionScope(this);
        }

        private class OnEntityTypeAddedNode : ConventionNode
        {
            public OnEntityTypeAddedNode(InternalEntityTypeBuilder entityTypeBuilder)
            {
                EntityTypeBuilder = entityTypeBuilder;
            }

            public InternalEntityTypeBuilder EntityTypeBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnEntityTypeAdded(this);
        }

        private class OnEntityTypeIgnoredNode : ConventionNode
        {
            public OnEntityTypeIgnoredNode(InternalModelBuilder modelBuilder, string name, Type type)
            {
                ModelBuilder = modelBuilder;
                Name = name;
                Type = type;
            }

            public InternalModelBuilder ModelBuilder { get; }
            public string Name { get; }
            public Type Type { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnEntityTypeIgnored(this);
        }

        private class OnEntityTypeMemberIgnoredNode : ConventionNode
        {
            public OnEntityTypeMemberIgnoredNode(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName)
            {
                EntityTypeBuilder = entityTypeBuilder;
                IgnoredMemberName = ignoredMemberName;
            }

            public InternalEntityTypeBuilder EntityTypeBuilder { get; }
            public string IgnoredMemberName { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnEntityTypeMemberIgnored(this);
        }

        private class OnBaseEntityTypeSetNode : ConventionNode
        {
            public OnBaseEntityTypeSetNode(InternalEntityTypeBuilder entityTypeBuilder, EntityType previousBaseType)
            {
                EntityTypeBuilder = entityTypeBuilder;
                PreviousBaseType = previousBaseType;
            }

            public InternalEntityTypeBuilder EntityTypeBuilder { get; }
            public EntityType PreviousBaseType { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnBaseEntityTypeSet(this);
        }

        private class OnEntityTypeAnnotationSetNode : ConventionNode
        {
            public OnEntityTypeAnnotationSetNode(
                InternalEntityTypeBuilder entityTypeBuilder,
                string name,
                Annotation annotation,
                Annotation oldAnnotation)
            {
                EntityTypeBuilder = entityTypeBuilder;
                Name = name;
                Annotation = annotation;
                OldAnnotation = oldAnnotation;
            }

            public InternalEntityTypeBuilder EntityTypeBuilder { get; }
            public string Name { get; }
            public Annotation Annotation { get; }
            public Annotation OldAnnotation { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnEntityTypeAnnotationSet(this);
        }

        private class OnForeignKeyAddedNode : ConventionNode
        {
            public OnForeignKeyAddedNode(InternalRelationshipBuilder relationshipBuilder)
            {
                RelationshipBuilder = relationshipBuilder;
            }

            public InternalRelationshipBuilder RelationshipBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnForeignKeyAdded(this);
        }

        private class OnForeignKeyRemovedNode : ConventionNode
        {
            public OnForeignKeyRemovedNode(InternalEntityTypeBuilder entityTypeBuilder, ForeignKey foreignKey)
            {
                EntityTypeBuilder = entityTypeBuilder;
                ForeignKey = foreignKey;
            }

            public InternalEntityTypeBuilder EntityTypeBuilder { get; }
            public ForeignKey ForeignKey { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnForeignKeyRemoved(this);
        }

        private class OnKeyAddedNode : ConventionNode
        {
            public OnKeyAddedNode(InternalKeyBuilder keyBuilder)
            {
                KeyBuilder = keyBuilder;
            }

            public InternalKeyBuilder KeyBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnKeyAdded(this);
        }

        private class OnKeyRemovedNode : ConventionNode
        {
            public OnKeyRemovedNode(InternalEntityTypeBuilder entityTypeBuilder, Key key)
            {
                EntityTypeBuilder = entityTypeBuilder;
                Key = key;
            }

            public InternalEntityTypeBuilder EntityTypeBuilder { get; }
            public Key Key { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnKeyRemoved(this);
        }

        private class OnPrimaryKeySetNode : ConventionNode
        {
            public OnPrimaryKeySetNode(InternalKeyBuilder keyBuilder, Key previousPrimaryKey)
            {
                KeyBuilder = keyBuilder;
                PreviousPrimaryKey = previousPrimaryKey;
            }

            public InternalKeyBuilder KeyBuilder { get; }
            public Key PreviousPrimaryKey { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnPrimaryKeySet(this);
        }

        private class OnIndexAddedNode : ConventionNode
        {
            public OnIndexAddedNode(InternalIndexBuilder indexBuilder)
            {
                IndexBuilder = indexBuilder;
            }

            public InternalIndexBuilder IndexBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnIndexAdded(this);
        }

        private class OnIndexRemovedNode : ConventionNode
        {
            public OnIndexRemovedNode(InternalEntityTypeBuilder entityTypeBuilder, Index index)
            {
                EntityTypeBuilder = entityTypeBuilder;
                Index = index;
            }

            public InternalEntityTypeBuilder EntityTypeBuilder { get; }
            public Index Index { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnIndexRemoved(this);
        }

        private class OnIndexUniquenessChangedNode : ConventionNode
        {
            public OnIndexUniquenessChangedNode(InternalIndexBuilder indexBuilder)
            {
                IndexBuilder = indexBuilder;
            }

            public InternalIndexBuilder IndexBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnIndexUniquenessChanged(this);
        }

        private class OnIndexAnnotationSetNode : ConventionNode
        {
            public OnIndexAnnotationSetNode(InternalIndexBuilder indexBuilder, string name, Annotation annotation, Annotation oldAnnotation)
            {
                IndexBuilder = indexBuilder;
                Name = name;
                Annotation = annotation;
                OldAnnotation = oldAnnotation;
            }

            public InternalIndexBuilder IndexBuilder { get; }
            public string Name { get; }
            public Annotation Annotation { get; }
            public Annotation OldAnnotation { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnIndexAnnotationSet(this);
        }

        private class OnNavigationAddedNode : ConventionNode
        {
            public OnNavigationAddedNode(InternalRelationshipBuilder relationshipBuilder, Navigation navigation)
            {
                RelationshipBuilder = relationshipBuilder;
                Navigation = navigation;
            }

            public InternalRelationshipBuilder RelationshipBuilder { get; }
            public Navigation Navigation { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnNavigationAdded(this);
        }

        private class OnNavigationRemovedNode : ConventionNode
        {
            public OnNavigationRemovedNode(
                InternalEntityTypeBuilder sourceEntityTypeBuilder,
                InternalEntityTypeBuilder targetEntityTypeBuilder,
                string navigationName,
                PropertyInfo propertyInfo)
            {
                SourceEntityTypeBuilder = sourceEntityTypeBuilder;
                TargetEntityTypeBuilder = targetEntityTypeBuilder;
                NavigationName = navigationName;
                PropertyInfo = propertyInfo;
            }

            public InternalEntityTypeBuilder SourceEntityTypeBuilder { get; }
            public InternalEntityTypeBuilder TargetEntityTypeBuilder { get; }
            public string NavigationName { get; }
            public PropertyInfo PropertyInfo { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnNavigationRemoved(this);
        }

        private class OnForeignKeyUniquenessChangedNode : ConventionNode
        {
            public OnForeignKeyUniquenessChangedNode(InternalRelationshipBuilder relationshipBuilder)
            {
                RelationshipBuilder = relationshipBuilder;
            }

            public InternalRelationshipBuilder RelationshipBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnForeignKeyUniquenessChanged(this);
        }

        private class OnPrincipalEndSetNode : ConventionNode
        {
            public OnPrincipalEndSetNode(InternalRelationshipBuilder relationshipBuilder)
            {
                RelationshipBuilder = relationshipBuilder;
            }

            public InternalRelationshipBuilder RelationshipBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnPrincipalEndSet(this);
        }

        private class OnPropertyAddedNode : ConventionNode
        {
            public OnPropertyAddedNode(InternalPropertyBuilder propertyBuilder)
            {
                PropertyBuilder = propertyBuilder;
            }

            public InternalPropertyBuilder PropertyBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnPropertyAdded(this);
        }

        private class OnPropertyNullableChangedNode : ConventionNode
        {
            public OnPropertyNullableChangedNode(InternalPropertyBuilder propertyBuilder)
            {
                PropertyBuilder = propertyBuilder;
            }

            public InternalPropertyBuilder PropertyBuilder { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnPropertyNullableChanged(this);
        }

        private class OnPropertyFieldChangedNode : ConventionNode
        {
            public OnPropertyFieldChangedNode(InternalPropertyBuilder propertyBuilder, FieldInfo oldFieldInfo)
            {
                PropertyBuilder = propertyBuilder;
                OldFieldInfo = oldFieldInfo;
            }

            public InternalPropertyBuilder PropertyBuilder { get; }
            public FieldInfo OldFieldInfo { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnPropertyFieldChanged(this);
        }

        private class OnPropertyAnnotationSetNode : ConventionNode
        {
            public OnPropertyAnnotationSetNode(
                InternalPropertyBuilder propertyBuilder,
                string name,
                Annotation annotation,
                Annotation oldAnnotation)
            {
                PropertyBuilder = propertyBuilder;
                Name = name;
                Annotation = annotation;
                OldAnnotation = oldAnnotation;
            }

            public InternalPropertyBuilder PropertyBuilder { get; }
            public string Name { get; }
            public Annotation Annotation { get; }
            public Annotation OldAnnotation { get; }

            public override ConventionNode Accept(ConventionVisitor visitor) => visitor.VisitOnPropertyAnnotationSet(this);
        }
    }
}
