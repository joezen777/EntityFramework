// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public partial class ConventionDispatcher
    {
        private readonly ConventionSet _conventionSet;
        private ConventionScope _scope;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public ConventionDispatcher([NotNull] ConventionSet conventionSet)
        {
            Check.NotNull(conventionSet, nameof(conventionSet));

            _conventionSet = conventionSet;
            Tracker = new MetadataTracker();
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual MetadataTracker Tracker { [DebuggerStepThrough] get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalEntityTypeBuilder OnEntityTypeAdded([NotNull] InternalEntityTypeBuilder entityTypeBuilder)
        {
            Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

            if (_scope == null)
            {
                return RunOnEntityTypeAdded(entityTypeBuilder);
            }

            _scope.Add(new OnEntityTypeAddedNode(entityTypeBuilder));
            return entityTypeBuilder;
        }

        private InternalEntityTypeBuilder RunOnEntityTypeAdded(InternalEntityTypeBuilder entityTypeBuilder)
        {
            if (entityTypeBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var entityTypeConvention in _conventionSet.EntityTypeAddedConventions)
            {
                entityTypeBuilder = entityTypeConvention.Apply(entityTypeBuilder);
                if (entityTypeBuilder?.Metadata.Builder == null)
                {
                    return null;
                }
            }

            return entityTypeBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual bool OnEntityTypeIgnored([NotNull] InternalModelBuilder modelBuilder, [NotNull] string name, [CanBeNull] Type type)
        {
            Check.NotNull(modelBuilder, nameof(modelBuilder));
            Check.NotNull(name, nameof(name));

            if (_scope == null)
            {
                return RunOnEntityTypeIgnored(modelBuilder, name, type);
            }

            _scope.Add(new OnEntityTypeIgnoredNode(modelBuilder, name, type));
            return true;
        }

        private bool RunOnEntityTypeIgnored(InternalModelBuilder modelBuilder, string name, Type type)
        {
            foreach (var entityTypeIgnoredConvention in _conventionSet.EntityTypeIgnoredConventions)
            {
                if (!entityTypeIgnoredConvention.Apply(modelBuilder, name, type))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalEntityTypeBuilder OnEntityTypeMemberIgnored(
            [NotNull] InternalEntityTypeBuilder entityTypeBuilder,
            [NotNull] string ignoredMemberName)
        {
            Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            Check.NotEmpty(ignoredMemberName, nameof(ignoredMemberName));

            if (_scope == null)
            {
                return RunOnEntityTypeMemberIgnored(entityTypeBuilder, ignoredMemberName);
            }

            _scope.Add(new OnEntityTypeMemberIgnoredNode(entityTypeBuilder, ignoredMemberName));
            return entityTypeBuilder;
        }

        private InternalEntityTypeBuilder RunOnEntityTypeMemberIgnored(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName)
        {
            if (entityTypeBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var entityType in entityTypeBuilder.Metadata.GetDerivedTypesInclusive())
            {
                foreach (var entityTypeMemberIgnoredConvention in _conventionSet.EntityTypeMemberIgnoredConventions)
                {
                    if (!entityTypeMemberIgnoredConvention.Apply(entityType.Builder, ignoredMemberName))
                    {
                        return null;
                    }
                }
            }

            return entityTypeBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalEntityTypeBuilder OnBaseEntityTypeSet(
            [NotNull] InternalEntityTypeBuilder entityTypeBuilder,
            [CanBeNull] EntityType previousBaseType)
        {
            Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

            if (_scope == null)
            {
                return RunOnBaseEntityTypeSet(entityTypeBuilder, previousBaseType);
            }

            _scope.Add(new OnBaseEntityTypeSetNode(entityTypeBuilder, previousBaseType));
            return entityTypeBuilder;
        }

        private InternalEntityTypeBuilder RunOnBaseEntityTypeSet(InternalEntityTypeBuilder entityTypeBuilder, EntityType previousBaseType)
        {
            if (entityTypeBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var entityTypeConvention in _conventionSet.BaseEntityTypeSetConventions)
            {
                if (!entityTypeConvention.Apply(entityTypeBuilder, previousBaseType))
                {
                    return null;
                }
            }

            return entityTypeBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual Annotation OnEntityTypeAnnotationSet(
            [NotNull] InternalEntityTypeBuilder entityTypeBuilder,
            [NotNull] string name,
            [CanBeNull] Annotation annotation,
            [CanBeNull] Annotation oldAnnotation)
        {
            Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            Check.NotNull(name, nameof(name));

            if (_scope == null)
            {
                return RunOnEntityTypeAnnotationSet(entityTypeBuilder, name, annotation, oldAnnotation);
            }

            _scope.Add(new OnEntityTypeAnnotationSetNode(entityTypeBuilder, name, annotation, oldAnnotation));
            return annotation;
        }

        private Annotation RunOnEntityTypeAnnotationSet(
            InternalEntityTypeBuilder entityTypeBuilder, string name, Annotation annotation, Annotation oldAnnotation)
        {
            if (entityTypeBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var entityTypeAnnotationSetConvention in _conventionSet.EntityTypeAnnotationSetConventions)
            {
                var newAnnotation = entityTypeAnnotationSetConvention.Apply(entityTypeBuilder, name, annotation, oldAnnotation);
                if (newAnnotation != annotation)
                {
                    return newAnnotation;
                }
            }

            return annotation;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalRelationshipBuilder OnForeignKeyAdded([NotNull] InternalRelationshipBuilder relationshipBuilder)
        {
            Check.NotNull(relationshipBuilder, nameof(relationshipBuilder));

            if (_scope == null)
            {
                return RunOnForeignKeyAdded(relationshipBuilder);
            }

            _scope.Add(new OnForeignKeyAddedNode(relationshipBuilder));
            return relationshipBuilder;
        }

        private InternalRelationshipBuilder RunOnForeignKeyAdded(InternalRelationshipBuilder relationshipBuilder)
        {
            if (relationshipBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var relationshipConvention in _conventionSet.ForeignKeyAddedConventions)
            {
                relationshipBuilder = relationshipConvention.Apply(relationshipBuilder);
                if (relationshipBuilder?.Metadata.Builder == null)
                {
                    return null;
                }
            }

            return relationshipBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual void OnForeignKeyRemoved([NotNull] InternalEntityTypeBuilder entityTypeBuilder, [NotNull] ForeignKey foreignKey)
        {
            Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            Check.NotNull(foreignKey, nameof(foreignKey));

            if (_scope == null)
            {
                RunOnForeignKeyRemoved(entityTypeBuilder, foreignKey);
                return;
            }

            _scope.Add(new OnForeignKeyRemovedNode(entityTypeBuilder, foreignKey));
        }

        private void RunOnForeignKeyRemoved(InternalEntityTypeBuilder entityTypeBuilder, ForeignKey foreignKey)
        {
            if (entityTypeBuilder.Metadata.Builder == null)
            {
                return;
            }

            foreach (var foreignKeyConvention in _conventionSet.ForeignKeyRemovedConventions)
            {
                foreignKeyConvention.Apply(entityTypeBuilder, foreignKey);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalKeyBuilder OnKeyAdded([NotNull] InternalKeyBuilder keyBuilder)
        {
            Check.NotNull(keyBuilder, nameof(keyBuilder));

            if (_scope == null)
            {
                return RunOnKeyAdded(keyBuilder);
            }

            _scope.Add(new OnKeyAddedNode(keyBuilder));
            return keyBuilder;
        }

        private InternalKeyBuilder RunOnKeyAdded(InternalKeyBuilder keyBuilder)
        {
            if (keyBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var keyConvention in _conventionSet.KeyAddedConventions)
            {
                keyBuilder = keyConvention.Apply(keyBuilder);
                if (keyBuilder?.Metadata.Builder == null)
                {
                    return null;
                }
            }

            return keyBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual void OnKeyRemoved([NotNull] InternalEntityTypeBuilder entityTypeBuilder, [NotNull] Key key)
        {
            Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            Check.NotNull(key, nameof(key));

            if (_scope == null)
            {
                RunOnKeyRemoved(entityTypeBuilder, key);
                return;
            }

            _scope.Add(new OnKeyRemovedNode(entityTypeBuilder, key));
        }

        private void RunOnKeyRemoved(InternalEntityTypeBuilder entityTypeBuilder, Key key)
        {
            if (entityTypeBuilder.Metadata.Builder == null)
            {
                return;
            }

            foreach (var keyConvention in _conventionSet.KeyRemovedConventions)
            {
                keyConvention.Apply(entityTypeBuilder, key);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalKeyBuilder OnPrimaryKeySet([NotNull] InternalKeyBuilder keyBuilder, [CanBeNull] Key previousPrimaryKey)
        {
            Check.NotNull(keyBuilder, nameof(keyBuilder));

            if (_scope == null)
            {
                return RunOnPrimaryKeySet(keyBuilder, previousPrimaryKey);
            }

            _scope.Add(new OnPrimaryKeySetNode(keyBuilder, previousPrimaryKey));
            return keyBuilder;
        }

        private InternalKeyBuilder RunOnPrimaryKeySet(InternalKeyBuilder keyBuilder, Key previousPrimaryKey)
        {
            if (keyBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var keyConvention in _conventionSet.PrimaryKeySetConventions)
            {
                if (!keyConvention.Apply(keyBuilder, previousPrimaryKey))
                {
                    return null;
                }
            }

            return keyBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalIndexBuilder OnIndexAdded([NotNull] InternalIndexBuilder indexBuilder)
        {
            Check.NotNull(indexBuilder, nameof(indexBuilder));

            if (_scope == null)
            {
                return RunOnIndexAdded(indexBuilder);
            }

            _scope.Add(new OnIndexAddedNode(indexBuilder));
            return indexBuilder;
        }

        private InternalIndexBuilder RunOnIndexAdded(InternalIndexBuilder indexBuilder)
        {
            if (indexBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var indexConvention in _conventionSet.IndexAddedConventions)
            {
                indexBuilder = indexConvention.Apply(indexBuilder);
                if (indexBuilder?.Metadata.Builder == null)
                {
                    return null;
                }
            }

            return indexBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual void OnIndexRemoved([NotNull] InternalEntityTypeBuilder entityTypeBuilder, [NotNull] Index index)
        {
            Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            Check.NotNull(index, nameof(index));

            if (_scope == null)
            {
                RunOnIndexRemoved(entityTypeBuilder, index);
                return;
            }

            _scope.Add(new OnIndexRemovedNode(entityTypeBuilder, index));
        }

        private void RunOnIndexRemoved(InternalEntityTypeBuilder entityTypeBuilder, Index index)
        {
            if (entityTypeBuilder.Metadata.Builder == null)
            {
                return;
            }

            foreach (var indexConvention in _conventionSet.IndexRemovedConventions)
            {
                indexConvention.Apply(entityTypeBuilder, index);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual bool OnIndexUniquenessChanged([NotNull] InternalIndexBuilder indexBuilder)
        {
            Check.NotNull(indexBuilder, nameof(indexBuilder));

            if (_scope == null)
            {
                return RunOnIndexUniquenessChanged(indexBuilder);
            }

            _scope.Add(new OnIndexUniquenessChangedNode(indexBuilder));
            return true;
        }

        private bool RunOnIndexUniquenessChanged(InternalIndexBuilder indexBuilder)
        {
            if (indexBuilder.Metadata.Builder == null)
            {
                return false;
            }

            foreach (var indexUniquenessConvention in _conventionSet.IndexUniquenessConventions)
            {
                if (!indexUniquenessConvention.Apply(indexBuilder))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual Annotation OnIndexAnnotationSet(
            [NotNull] InternalIndexBuilder indexBuilder,
            [NotNull] string name,
            [CanBeNull] Annotation annotation,
            [CanBeNull] Annotation oldAnnotation)
        {
            Check.NotNull(indexBuilder, nameof(indexBuilder));
            Check.NotNull(name, nameof(name));

            if (_scope == null)
            {
                return RunOnIndexAnnotationSet(indexBuilder, name, annotation, oldAnnotation);
            }

            _scope.Add(new OnIndexAnnotationSetNode(indexBuilder, name, annotation, oldAnnotation));
            return annotation;
        }

        private Annotation RunOnIndexAnnotationSet(
            InternalIndexBuilder indexBuilder, string name, Annotation annotation, Annotation oldAnnotation)
        {
            if (indexBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var indexAnnotationSetConvention in _conventionSet.IndexAnnotationSetConventions)
            {
                var newAnnotation = indexAnnotationSetConvention.Apply(indexBuilder, name, annotation, oldAnnotation);
                if (newAnnotation != annotation)
                {
                    return newAnnotation;
                }
            }

            return annotation;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalModelBuilder OnModelBuilt([NotNull] InternalModelBuilder modelBuilder)
        {
            Check.NotNull(modelBuilder, nameof(modelBuilder));

            foreach (var modelConvention in _conventionSet.ModelBuiltConventions)
            {
                modelBuilder = modelConvention.Apply(modelBuilder);
                if (modelBuilder == null)
                {
                    break;
                }
            }

            return modelBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalModelBuilder OnModelInitialized([NotNull] InternalModelBuilder modelBuilder)
        {
            Check.NotNull(modelBuilder, nameof(modelBuilder));

            foreach (var modelConvention in _conventionSet.ModelInitializedConventions)
            {
                modelBuilder = modelConvention.Apply(modelBuilder);
                if (modelBuilder == null)
                {
                    break;
                }
            }

            return modelBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalRelationshipBuilder OnNavigationAdded(
            [NotNull] InternalRelationshipBuilder relationshipBuilder, [NotNull] Navigation navigation)
        {
            Check.NotNull(relationshipBuilder, nameof(relationshipBuilder));
            Check.NotNull(navigation, nameof(navigation));

            if (_scope == null)
            {
                return RunOnNavigationAdded(relationshipBuilder, navigation);
            }

            _scope.Add(new OnNavigationAddedNode(relationshipBuilder, navigation));
            return relationshipBuilder;
        }

        private InternalRelationshipBuilder RunOnNavigationAdded(InternalRelationshipBuilder relationshipBuilder, Navigation navigation)
        {
            if (relationshipBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var navigationConvention in _conventionSet.NavigationAddedConventions)
            {
                relationshipBuilder = navigationConvention.Apply(relationshipBuilder, navigation);
                if (relationshipBuilder?.Metadata.Builder == null)
                {
                    return null;
                }
            }

            return relationshipBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual void OnNavigationRemoved(
            [NotNull] InternalEntityTypeBuilder sourceEntityTypeBuilder,
            [NotNull] InternalEntityTypeBuilder targetEntityTypeBuilder,
            [NotNull] string navigationName,
            [CanBeNull] PropertyInfo propertyInfo)
        {
            Check.NotNull(sourceEntityTypeBuilder, nameof(sourceEntityTypeBuilder));
            Check.NotNull(targetEntityTypeBuilder, nameof(targetEntityTypeBuilder));
            Check.NotNull(navigationName, nameof(navigationName));

            if (_scope == null)
            {
                RunOnNavigationRemoved(sourceEntityTypeBuilder, targetEntityTypeBuilder, navigationName, propertyInfo);
                return;
            }

            _scope.Add(new OnNavigationRemovedNode(sourceEntityTypeBuilder, targetEntityTypeBuilder, navigationName, propertyInfo));
        }

        private void RunOnNavigationRemoved(
            InternalEntityTypeBuilder sourceEntityTypeBuilder,
            InternalEntityTypeBuilder targetEntityTypeBuilder,
            string navigationName,
            PropertyInfo propertyInfo)
        {
            if (sourceEntityTypeBuilder.Metadata.Builder == null)
            {
                return;
            }

            foreach (var navigationConvention in _conventionSet.NavigationRemovedConventions)
            {
                if (!navigationConvention.Apply(sourceEntityTypeBuilder, targetEntityTypeBuilder, navigationName, propertyInfo))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalRelationshipBuilder OnForeignKeyUniquenessChanged([NotNull] InternalRelationshipBuilder relationshipBuilder)
        {
            Check.NotNull(relationshipBuilder, nameof(relationshipBuilder));

            if (_scope == null)
            {
                return RunOnForeignKeyUniquenessChanged(relationshipBuilder);
            }

            _scope.Add(new OnForeignKeyUniquenessChangedNode(relationshipBuilder));
            return relationshipBuilder;
        }

        private InternalRelationshipBuilder RunOnForeignKeyUniquenessChanged(InternalRelationshipBuilder relationshipBuilder)
        {
            if (relationshipBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var uniquenessConvention in _conventionSet.ForeignKeyUniquenessConventions)
            {
                relationshipBuilder = uniquenessConvention.Apply(relationshipBuilder);
                if (relationshipBuilder?.Metadata.Builder == null)
                {
                    return null;
                }
            }

            return relationshipBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalRelationshipBuilder OnPrincipalEndSet([NotNull] InternalRelationshipBuilder relationshipBuilder)
        {
            Check.NotNull(relationshipBuilder, nameof(relationshipBuilder));

            if (_scope == null)
            {
                return RunOnPrincipalEndSet(relationshipBuilder);
            }

            _scope.Add(new OnPrincipalEndSetNode(relationshipBuilder));
            return relationshipBuilder;
        }

        private InternalRelationshipBuilder RunOnPrincipalEndSet(InternalRelationshipBuilder relationshipBuilder)
        {
            if (relationshipBuilder.Metadata.Builder == null)
            {
                return null;
            }

            foreach (var relationshipConvention in _conventionSet.PrincipalEndSetConventions)
            {
                relationshipBuilder = relationshipConvention.Apply(relationshipBuilder);
                if (relationshipBuilder == null)
                {
                    break;
                }
            }

            return relationshipBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual InternalPropertyBuilder OnPropertyAdded([NotNull] InternalPropertyBuilder propertyBuilder)
        {
            Check.NotNull(propertyBuilder, nameof(propertyBuilder));

            if (_scope == null)
            {
                return RunOnPropertyAdded(propertyBuilder);
            }

            _scope.Add(new OnPropertyAddedNode(propertyBuilder));
            return propertyBuilder;
        }

        private InternalPropertyBuilder RunOnPropertyAdded(InternalPropertyBuilder propertyBuilder)
        {
            if (propertyBuilder.Metadata.Builder == null
                || propertyBuilder.Metadata.DeclaringEntityType.Builder == null)
            {
                return null;
            }

            foreach (var propertyConvention in _conventionSet.PropertyAddedConventions)
            {
                propertyBuilder = propertyConvention.Apply(propertyBuilder);
                if (propertyBuilder?.Metadata.Builder == null
                    || propertyBuilder.Metadata.DeclaringEntityType.Builder == null)
                {
                    return null;
                }
            }

            return propertyBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual bool OnPropertyNullableChanged([NotNull] InternalPropertyBuilder propertyBuilder)
        {
            Check.NotNull(propertyBuilder, nameof(propertyBuilder));

            if (_scope == null)
            {
                return RunOnPropertyNullableChanged(propertyBuilder);
            }

            _scope.Add(new OnPropertyNullableChangedNode(propertyBuilder));
            return true;
        }

        private bool RunOnPropertyNullableChanged(InternalPropertyBuilder propertyBuilder)
        {
            if (propertyBuilder.Metadata.Builder == null
                || propertyBuilder.Metadata.DeclaringEntityType.Builder == null)
            {
                return false;
            }

            foreach (var propertyConvention in _conventionSet.PropertyNullableChangedConventions)
            {
                if (!propertyConvention.Apply(propertyBuilder))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual bool OnPropertyFieldChanged(
            [NotNull] InternalPropertyBuilder propertyBuilder, [CanBeNull] FieldInfo oldFieldInfo)
        {
            Check.NotNull(propertyBuilder, nameof(propertyBuilder));

            if (_scope == null)
            {
                return RunOnPropertyFieldChanged(propertyBuilder, oldFieldInfo);
            }

            _scope.Add(new OnPropertyFieldChangedNode(propertyBuilder, oldFieldInfo));
            return true;
        }

        private bool RunOnPropertyFieldChanged(InternalPropertyBuilder propertyBuilder, FieldInfo oldFieldInfo)
        {
            if (propertyBuilder.Metadata.Builder == null
                || propertyBuilder.Metadata.DeclaringEntityType.Builder == null)
            {
                return false;
            }

            foreach (var propertyConvention in _conventionSet.PropertyFieldChangedConventions)
            {
                if (!propertyConvention.Apply(propertyBuilder, oldFieldInfo))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual Annotation OnPropertyAnnotationSet(
            [NotNull] InternalPropertyBuilder propertyBuilder,
            [NotNull] string name,
            [CanBeNull] Annotation annotation,
            [CanBeNull] Annotation oldAnnotation)
        {
            Check.NotNull(propertyBuilder, nameof(propertyBuilder));
            Check.NotNull(name, nameof(name));

            if (_scope == null)
            {
                return RunOnPropertyAnnotationSet(propertyBuilder, name, annotation, oldAnnotation);
            }

            _scope.Add(new OnPropertyAnnotationSetNode(propertyBuilder, name, annotation, oldAnnotation));
            return annotation;
        }

        private Annotation RunOnPropertyAnnotationSet(
            InternalPropertyBuilder propertyBuilder, string name, Annotation annotation, Annotation oldAnnotation)
        {
            if (propertyBuilder.Metadata.Builder == null
                || propertyBuilder.Metadata.DeclaringEntityType.Builder == null)
            {
                return null;
            }

            foreach (var propertyAnnotationSetConvention in _conventionSet.PropertyAnnotationSetConventions)
            {
                var newAnnotation = propertyAnnotationSetConvention.Apply(propertyBuilder, name, annotation, oldAnnotation);
                if (newAnnotation != annotation)
                {
                    return newAnnotation;
                }
            }

            return annotation;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IConventionBatch StartBatch() => new ConventionBatch(this);

        private class ConventionBatch : IConventionBatch
        {
            private readonly ConventionDispatcher _dispatcher;
            private bool _ran;

            public ConventionBatch(ConventionDispatcher dispatcher)
            {
                _dispatcher = dispatcher;
                var currentScope = _dispatcher._scope;
                dispatcher._scope = new ConventionScope(currentScope, children: null);
                currentScope?.Add(dispatcher._scope);
            }

            private void Run()
            {
                _ran = true;
                while (true)
                {
                    var currentScope = _dispatcher._scope;
                    if (currentScope == null)
                    {
                        return;
                    }

                    _dispatcher._scope = currentScope.Parent;
                    currentScope.MakeReadonly();

                    if (currentScope.Parent != null
                        || currentScope.GetLeafCount() == 0)
                    {
                        return;
                    }

                    // Capture all nested convention invocations to unwind the stack
                    _dispatcher._scope = new ConventionScope(null, children: null);
                    new RunVisitor(_dispatcher).VisitConventionScope(currentScope);
                }
            }

            public ForeignKey Run(ForeignKey foreignKey)
            {
                using (var foreignKeyReference = _dispatcher.Tracker.Track(foreignKey))
                {
                    Run();
                    return foreignKeyReference.Object?.Builder == null ? null : foreignKeyReference.Object;
                }
            }

            public void Dispose()
            {
                if (!_ran)
                {
                    Run();
                }
            }
        }
    }
}
