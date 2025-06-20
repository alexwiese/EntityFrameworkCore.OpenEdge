using EntityFrameworkCore.OpenEdge.Storage.Internal.Mapping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Moq;
using Xunit;

namespace EFCore.OpenEdge.Tests.Unit.TypeMapping
{
    public class OpenEdgeTypeMappingSourceTests
    {
        private readonly OpenEdgeTypeMappingSource _typeMappingSource;

        public OpenEdgeTypeMappingSourceTests()
        {
            var valueConverterSelector = CreateValueConverterSelector();
            var plugins = Enumerable.Empty<ITypeMappingSourcePlugin>();
            var relationalPlugins = Enumerable.Empty<IRelationalTypeMappingSourcePlugin>();
            
            var dependencies = new TypeMappingSourceDependencies(valueConverterSelector, plugins);
            var relationalDependencies = new RelationalTypeMappingSourceDependencies(relationalPlugins);
            
            _typeMappingSource = new OpenEdgeTypeMappingSource(dependencies, relationalDependencies);
        }
        
        private static IValueConverterSelector CreateValueConverterSelector()
        {
            var mock = new Mock<IValueConverterSelector>();
            mock.Setup(x => x.Select(It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(Enumerable.Empty<ValueConverterInfo>());
            return mock.Object;
        }

        public static IEnumerable<object[]> ClrTypeMappingData =>
            new List<object[]>
            {
                new object[] { typeof(int), "integer", DbType.Int32 },
                new object[] { typeof(long), "bigint", null },
                new object[] { typeof(short), "smallint", DbType.Int16 },
                new object[] { typeof(byte), "tinyint", DbType.Byte },
                new object[] { typeof(bool), "bit", null },
                new object[] { typeof(DateTime), "datetime", DbType.DateTime },
                new object[] { typeof(DateTimeOffset), "datetime-tz", DbType.DateTimeOffset },
                new object[] { typeof(TimeSpan), "time", DbType.Time },
                new object[] { typeof(decimal), "decimal", null },
                new object[] { typeof(double), "double precision", null },
                new object[] { typeof(float), "real", null },
                new object[] { typeof(byte[]), "binary", DbType.Binary }
            };

        [Theory]
        [MemberData(nameof(ClrTypeMappingData))]
        public void FindMapping_WithClrType_ShouldReturnCorrectMapping(Type clrType, string expectedStoreType, DbType? expectedDbType)
        {
            // Act
            var result = (RelationalTypeMapping) _typeMappingSource.FindMapping(clrType);

            // Assert
            result.Should().NotBeNull();
            result.ClrType.Should().Be(clrType);
            result.StoreType.Should().Be(expectedStoreType);
            
            if (expectedDbType.HasValue)
            {
                result.DbType.Should().Be(expectedDbType.Value);
            }
        }

        public static IEnumerable<object[]> StoreTypeMappingData =>
            new List<object[]>
            {
                // Integer types
                new object[] { "bigint", typeof(long) },
                new object[] { "int64", typeof(long) },
                new object[] { "integer", typeof(int) },
                new object[] { "int", typeof(int) },
                new object[] { "smallint", typeof(short) },
                new object[] { "short", typeof(short) },
                new object[] { "tinyint", typeof(byte) },
                
                // Boolean types
                new object[] { "bit", typeof(bool) },
                new object[] { "logical", typeof(bool) },
                
                // String types
                new object[] { "char", typeof(string) },
                new object[] { "character", typeof(string) },
                new object[] { "varchar", typeof(string) },
                new object[] { "char varying", typeof(string) },
                new object[] { "character varying", typeof(string) },
                new object[] { "text", typeof(string) },
                new object[] { "clob", typeof(string) },
                new object[] { "recid", typeof(string) },
                
                // Date/Time types
                new object[] { "date", typeof(DateTime) },
                new object[] { "datetime", typeof(DateTime) },
                new object[] { "datetime2", typeof(DateTime) },
                new object[] { "smalldatetime", typeof(DateTime) },
                new object[] { "timestamp", typeof(DateTime) },
                new object[] { "time", typeof(TimeSpan) },
                new object[] { "datetime-tz", typeof(DateTimeOffset) },
                new object[] { "datetimeoffset", typeof(DateTimeOffset) },
                
                // Numeric types
                new object[] { "decimal", typeof(decimal) },
                new object[] { "dec", typeof(decimal) },
                new object[] { "numeric", typeof(decimal) },
                new object[] { "money", typeof(decimal) },
                new object[] { "smallmoney", typeof(decimal) },
                new object[] { "real", typeof(float) },
                new object[] { "float", typeof(double) },
                new object[] { "double", typeof(double) },
                new object[] { "double precision", typeof(double) },
                
                // Binary types
                new object[] { "binary", typeof(byte[]) },
                new object[] { "varbinary", typeof(byte[]) },
                new object[] { "binary varying", typeof(byte[]) },
                new object[] { "raw", typeof(byte[]) },
                new object[] { "blob", typeof(byte[]) },
                new object[] { "image", typeof(byte[]) }
            };

        [Theory]
        [MemberData(nameof(StoreTypeMappingData))]
        public void FindMapping_WithStoreTypeName_ShouldReturnCorrectClrType(string storeTypeName, Type expectedClrType)
        {
            // Act
            var result = _typeMappingSource.FindMapping(storeTypeName);

            // Assert
            result.Should().NotBeNull();
            result.ClrType.Should().Be(expectedClrType);
        }

        [Theory]
        [InlineData("BIGINT")] // Test case insensitivity
        [InlineData("VARCHAR")]
        [InlineData("Datetime")]
        public void FindMapping_WithStoreTypeName_ShouldBeCaseInsensitive(string storeTypeName)
        {
            // Act
            var result = _typeMappingSource.FindMapping(storeTypeName);

            // Assert
            result.Should().NotBeNull("mapping should be case insensitive");
        }
        
        [Theory]
        [InlineData("float(15)", typeof(double))] // Should map to double by default
        [InlineData("real", typeof(float))] // Real should map to float
        [InlineData("double precision", typeof(double))] // Should map to double
        public void FindMapping_WithFloatTypes_ShouldSelectCorrectType(string storeType, Type expectedClrType)
        {
            // Act
            var result = _typeMappingSource.FindMapping(storeType);

            // Assert
            result.Should().NotBeNull();
            result.ClrType.Should().Be(expectedClrType);
        }

        [Fact]
        public void FindMapping_WithUnknownStoreType_ShouldReturnNull()
        {
            // Act
            var result = _typeMappingSource.FindMapping("unknowntype");

            // Assert
            result.Should().BeNull();
        }
    }
}