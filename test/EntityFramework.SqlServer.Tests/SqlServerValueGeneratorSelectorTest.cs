// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Identity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Tests;
using Moq;
using Xunit;

namespace Microsoft.Data.Entity.SqlServer.Tests
{
    public class SqlServerValueGeneratorSelectorTest
    {
        [Fact]
        public void Returns_in_temp_generator_for_all_integer_types_except_byte_setup_for_value_generation()
        {
            var tempFactory = new SimpleValueGeneratorFactory<TemporaryValueGenerator>();

            var selector = new SqlServerValueGeneratorSelector(
                new SimpleValueGeneratorFactory<GuidValueGenerator>(),
                tempFactory,
                new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor()),
                new SimpleValueGeneratorFactory<SequentialGuidValueGenerator>());

            Assert.Same(tempFactory, selector.Select(CreateProperty(typeof(long), ValueGeneration.OnAdd)));
            Assert.Same(tempFactory, selector.Select(CreateProperty(typeof(int), ValueGeneration.OnAdd)));
            Assert.Same(tempFactory, selector.Select(CreateProperty(typeof(short), ValueGeneration.OnAdd)));
        }

        [Fact] // TODO: This will change when sequence becomes the default
        public void Returns_sequence_generator_for_bytes()
        {
            var sequenceFactory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor());

            var selector = new SqlServerValueGeneratorSelector(
                new SimpleValueGeneratorFactory<GuidValueGenerator>(),
                new SimpleValueGeneratorFactory<TemporaryValueGenerator>(),
                sequenceFactory,
                new SimpleValueGeneratorFactory<SequentialGuidValueGenerator>());

            Assert.Same(sequenceFactory, selector.Select(CreateProperty(typeof(byte), ValueGeneration.OnAdd)));
        }

        [Fact]
        public void Returns_sequential_GUID_generator_for_GUID_types_setup_for_client_values()
        {
            var sequentialGuidFactory = new SimpleValueGeneratorFactory<SequentialGuidValueGenerator>();

            var selector = new SqlServerValueGeneratorSelector(
                new SimpleValueGeneratorFactory<GuidValueGenerator>(),
                new SimpleValueGeneratorFactory<TemporaryValueGenerator>(),
                new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor()),
                sequentialGuidFactory);

            Assert.Same(sequentialGuidFactory, selector.Select(CreateProperty(typeof(Guid), ValueGeneration.OnAdd)));
        }

        [Fact]
        public void Returns_null_when_no_value_generation_configured()
        {
            var selector = new SqlServerValueGeneratorSelector(
                new SimpleValueGeneratorFactory<GuidValueGenerator>(),
                new SimpleValueGeneratorFactory<TemporaryValueGenerator>(),
                new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor()),
                new SimpleValueGeneratorFactory<SequentialGuidValueGenerator>());

            Assert.Null(selector.Select(CreateProperty(typeof(int), ValueGeneration.None)));
            Assert.Null(selector.Select(CreateProperty(typeof(int), ValueGeneration.OnAddAndUpdate)));
        }

        [Fact]
        public void Throws_for_unsupported_combinations()
        {
            var selector = new SqlServerValueGeneratorSelector(
                new SimpleValueGeneratorFactory<GuidValueGenerator>(),
                new SimpleValueGeneratorFactory<TemporaryValueGenerator>(),
                new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor()),
                new SimpleValueGeneratorFactory<SequentialGuidValueGenerator>());

            var typeMock = new Mock<IEntityType>();
            typeMock.Setup(m => m.Name).Returns("AnEntity");

            var property = CreateProperty(typeof(double), ValueGeneration.OnAdd);

            Assert.Equal(
                TestHelpers.GetCoreString("FormatNoValueGenerator", "MyProperty", "MyType", "Double"),
                Assert.Throws<NotSupportedException>(() => selector.Select(property)).Message);
        }

        private static Property CreateProperty(Type propertyType, ValueGeneration valueGeneration)
        {
            var entityType = new EntityType("MyType");
            var property = entityType.GetOrAddProperty("MyProperty", propertyType, shadowProperty: true);
            property.ValueGeneration = valueGeneration;
            entityType.SetTableName("MyTable");

            new Model().AddEntityType(entityType);

            return property;
        }
    }
}
