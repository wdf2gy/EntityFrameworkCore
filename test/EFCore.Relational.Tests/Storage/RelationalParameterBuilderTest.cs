﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class RelationalParameterBuilderTest
    {
        [Fact]
        public void Can_add_dynamic_parameter()
        {
            var typeMapper = new FallbackRelationalCoreTypeMapper(
                TestServiceFactory.Instance.Create<CoreTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<FakeRelationalTypeMapper>());

            var parameterBuilder = new RelationalParameterBuilder(typeMapper);

            parameterBuilder.AddParameter(
                "InvariantName",
                "Name");

            Assert.Equal(1, parameterBuilder.Parameters.Count);

            var parameter = parameterBuilder.Parameters[0] as DynamicRelationalParameter;

            Assert.NotNull(parameter);
            Assert.Equal("InvariantName", parameter.InvariantName);
            Assert.Equal("Name", parameter.Name);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Can_add_type_mapped_parameter_by_type(bool nullable)
        {
            var typeMapper = (IRelationalCoreTypeMapper)new FallbackRelationalCoreTypeMapper(
                TestServiceFactory.Instance.Create<CoreTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<FakeRelationalTypeMapper>());
            var typeMapping = typeMapper.FindMapping(nullable ? typeof(int?) : typeof(int));
            var parameterBuilder = new RelationalParameterBuilder(typeMapper);

            parameterBuilder.AddParameter(
                "InvariantName",
                "Name",
                typeMapping,
                nullable);

            Assert.Equal(1, parameterBuilder.Parameters.Count);

            var parameter = parameterBuilder.Parameters[0] as TypeMappedRelationalParameter;

            Assert.NotNull(parameter);
            Assert.Equal("InvariantName", parameter.InvariantName);
            Assert.Equal("Name", parameter.Name);
            Assert.Equal(typeMapping, parameter.RelationalTypeMapping);
            Assert.Equal(nullable, parameter.IsNullable);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Can_add_type_mapped_parameter_by_property(bool nullable)
        {
            var typeMapper = new FallbackRelationalCoreTypeMapper(
                TestServiceFactory.Instance.Create<CoreTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<FakeRelationalTypeMapper>());

            var property = new Model().AddEntityType("MyType").AddProperty("MyProp", typeof(string));
            property.IsNullable = nullable;
            property[CoreAnnotationNames.TypeMapping] = GetMapping(typeMapper, property);

            var parameterBuilder = new RelationalParameterBuilder(typeMapper);

            parameterBuilder.AddParameter(
                "InvariantName",
                "Name",
                property);

            Assert.Equal(1, parameterBuilder.Parameters.Count);

            var parameter = parameterBuilder.Parameters[0] as TypeMappedRelationalParameter;

            Assert.NotNull(parameter);
            Assert.Equal("InvariantName", parameter.InvariantName);
            Assert.Equal("Name", parameter.Name);
            Assert.Equal(GetMapping(typeMapper, property), parameter.RelationalTypeMapping);
            Assert.Equal(nullable, parameter.IsNullable);
        }

        [Fact]
        public void Can_add_composite_parameter()
        {
            var typeMapper = new FallbackRelationalCoreTypeMapper(
                TestServiceFactory.Instance.Create<CoreTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<FakeRelationalTypeMapper>());

            var parameterBuilder = new RelationalParameterBuilder(typeMapper);

            parameterBuilder.AddCompositeParameter(
                "CompositeInvariant",
                builder =>
                    {
                        builder.AddParameter(
                            "FirstInvariant",
                            "FirstName",
                            new IntTypeMapping("int", DbType.Int32),
                            nullable: false);

                        builder.AddParameter(
                            "SecondInvariant",
                            "SecondName",
                            new StringTypeMapping("nvarchar(max)"),
                            nullable: true);
                    });

            Assert.Equal(1, parameterBuilder.Parameters.Count);

            var parameter = parameterBuilder.Parameters[0] as CompositeRelationalParameter;

            Assert.NotNull(parameter);
            Assert.Equal("CompositeInvariant", parameter.InvariantName);
            Assert.Equal(2, parameter.RelationalParameters.Count);
        }

        [Fact]
        public void Does_not_add_empty_composite_parameter()
        {
            var typeMapper = new FallbackRelationalCoreTypeMapper(
                TestServiceFactory.Instance.Create<CoreTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMapperDependencies>(),
                TestServiceFactory.Instance.Create<FakeRelationalTypeMapper>());

            var parameterBuilder = new RelationalParameterBuilder(typeMapper);

            parameterBuilder.AddCompositeParameter(
                "CompositeInvariant",
                builder => { });

            Assert.Equal(0, parameterBuilder.Parameters.Count);
        }

        public static RelationalTypeMapping GetMapping(
            IRelationalCoreTypeMapper typeMapper,
            IProperty property)
            => typeMapper.FindMapping(property);
    }
}
