using EntityFrameworkCore.OpenEdge.Extensions;
using EFCore.OpenEdge.Tests.TestUtilities;
using FluentAssertions;
using System;
using System.Data.Common;
using Moq;
using Xunit;

namespace EFCore.OpenEdge.Tests.Unit.Extensions
{
    [Trait("Category", TestCategories.Unit)]
    [Trait("Category", TestCategories.Extensions)]
    public class OpenEdgeDataReaderExtensionsTests
    {
        [Fact]
        public void GetValueOrDefault_WithValidIntValue_ShouldReturnConvertedValue()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.GetOrdinal("TestColumn")).Returns(0);
            mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
            mockReader.Setup(r => r.GetValue(0)).Returns("123");

            // Act
            var result = mockReader.Object.GetValueOrDefault<int>("TestColumn");

            // Assert
            result.Should().Be(123);
        }

        [Fact]
        public void GetValueOrDefault_WithDBNull_ShouldReturnDefault()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.GetOrdinal("TestColumn")).Returns(0);
            mockReader.Setup(r => r.IsDBNull(0)).Returns(true);

            // Act
            var result = mockReader.Object.GetValueOrDefault<int>("TestColumn");

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void GetValueOrDefault_WithValidBoolValue_ShouldReturnConvertedValue()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.GetOrdinal("TestColumn")).Returns(0);
            mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
            mockReader.Setup(r => r.GetValue(0)).Returns("true");

            // Act
            var result = mockReader.Object.GetValueOrDefault<bool>("TestColumn");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValueOrDefault_WithInvalidIntValue_ShouldThrowFormatException()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.GetOrdinal("TestColumn")).Returns(0);
            mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
            mockReader.Setup(r => r.GetValue(0)).Returns("not-a-number");

            // Act & Assert
            Assert.Throws<FormatException>(() => 
                mockReader.Object.GetValueOrDefault<int>("TestColumn"));
        }

        [Fact]
        public void GetValueOrDefault_WithStringType_ShouldReturnOriginalValue()
        {
            // Arrange
            var mockReader = new Mock<DbDataReader>();
            mockReader.Setup(r => r.GetOrdinal("TestColumn")).Returns(0);
            mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
            mockReader.Setup(r => r.GetValue(0)).Returns("test-string");

            // Act
            var result = mockReader.Object.GetValueOrDefault<string>("TestColumn");

            // Assert
            result.Should().Be("test-string");
        }
    }
}