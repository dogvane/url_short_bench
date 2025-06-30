using System;
using Xunit;
using url_short.common;

namespace Tests
{
    /// <summary>
    /// Base62Converter 的单元测试
    /// </summary>
    public class Base62ConverterTests
    {
        private readonly Base62Converter _converter;
        private readonly Base62Converter _fixedLengthConverter;

        public Base62ConverterTests()
        {
            _converter = new Base62Converter();
            _fixedLengthConverter = new Base62Converter(6);
        }

        [Fact]
        public void Encode_Zero_ReturnsZero()
        {
            // Arrange
            long input = 0;
            
            // Act
            string result = _converter.Encode(input);
            
            // Assert
            Assert.Equal("0", result);
        }

        [Fact]
        public void Encode_PositiveNumber_ReturnsValidBase62String()
        {
            // Arrange
            long input = 123456;
            
            // Act
            string result = _converter.Encode(input);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // 验证字符串只包含 Base62 字符
            string validChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            foreach (char c in result)
            {
                Assert.Contains(c, validChars);
            }
        }

        [Fact]
        public void Encode_NegativeNumber_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            long input = -1;
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _converter.Encode(input));
        }

        [Fact]
        public void Decode_ValidBase62String_ReturnsCorrectNumber()
        {
            // Arrange
            string input = "1A";
            long expected = 72;
            
            // Act
            long result = _converter.Decode(input);
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decode_ZeroString_ReturnsZero()
        {
            // Arrange
            string input = "0";
            
            // Act
            long result = _converter.Decode(input);
            
            // Assert
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Decode_NullOrWhitespaceString_ThrowsArgumentNullException(string input)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _converter.Decode(input));
        }

        [Fact]
        public void Decode_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _converter.Decode(null!));
        }

        [Fact]
        public void Decode_InvalidCharacter_ThrowsArgumentException()
        {
            // Arrange
            string input = "123@";
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _converter.Decode(input));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(61)]
        [InlineData(62)]
        [InlineData(123)]
        [InlineData(3844)]
        [InlineData(123456)]
        [InlineData(999999999)]
        [InlineData(long.MaxValue)]
        public void EncodeAndDecode_RoundTrip_ReturnsOriginalValue(long originalValue)
        {
            // Act
            string encoded = _converter.Encode(originalValue);
            long decoded = _converter.Decode(encoded);
            
            // Assert
            Assert.Equal(originalValue, decoded);
        }

        [Fact]
        public void Encode_DifferentNumbers_ReturnsDifferentStrings()
        {
            // Arrange
            long input1 = 123;
            long input2 = 456;
            
            // Act
            string result1 = _converter.Encode(input1);
            string result2 = _converter.Encode(input2);
            
            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void Encode_SequentialNumbers_ReturnsExpectedPattern()
        {
            // Arrange & Act
            string result0 = _converter.Encode(0);
            string result1 = _converter.Encode(1);
            string result61 = _converter.Encode(61);
            string result62 = _converter.Encode(62);
            
            // Assert
            Assert.Equal("0", result0);
            Assert.Equal("1", result1);
            Assert.Equal("z", result61);
            Assert.Equal("10", result62);
        }

        [Fact]
        public void FixedLength_Constructor_WithNegativeLength_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new Base62Converter(-1));
        }

        [Fact]
        public void FixedLength_Encode_Zero_ReturnsFixedLengthString()
        {
            // Act
            string result = _fixedLengthConverter.Encode(0);
            
            // Assert
            Assert.Equal("000000", result);
            Assert.Equal(6, result.Length);
        }

        [Fact]
        public void FixedLength_Encode_SmallNumber_ReturnsPaddedString()
        {
            // Arrange
            long input = 123;
            
            // Act
            string result = _fixedLengthConverter.Encode(input);
            
            // Assert
            Assert.Equal(6, result.Length);
            Assert.StartsWith("00", result);
            
            // 验证解码后能得到原值
            long decoded = _fixedLengthConverter.Decode(result);
            Assert.Equal(input, decoded);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(61)]
        [InlineData(3844)] // 62^2
        [InlineData(238327)] // 62^3
        public void FixedLength_Encode_VariousNumbers_ReturnsFixedLengthStrings(long input)
        {
            // Act
            string result = _fixedLengthConverter.Encode(input);
            
            // Assert
            Assert.Equal(6, result.Length);
            
            // 验证解码后能得到原值
            long decoded = _fixedLengthConverter.Decode(result);
            Assert.Equal(input, decoded);
        }

        [Fact]
        public void FixedLength_Encode_MaxValue_ReturnsValidString()
        {
            // Arrange
            // 6位Base62的最大值: 62^6 - 1 = 56800235583
            long maxValue = (long)Math.Pow(62, 6) - 1;
            
            // Act
            string result = _fixedLengthConverter.Encode(maxValue);
            
            // Assert
            Assert.Equal(6, result.Length);
            Assert.Equal("zzzzzz", result);
            
            // 验证解码后能得到原值
            long decoded = _fixedLengthConverter.Decode(result);
            Assert.Equal(maxValue, decoded);
        }

        [Fact]
        public void FixedLength_Encode_ExceedsMaxValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            long maxValue = (long)Math.Pow(62, 6);
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _fixedLengthConverter.Encode(maxValue));
        }

        [Fact]
        public void FixedLength_vs_VariableLength_ProduceDifferentResults()
        {
            // Arrange
            long input = 123;
            
            // Act
            string variableResult = _converter.Encode(input);
            string fixedResult = _fixedLengthConverter.Encode(input);
            
            // Assert
            Assert.NotEqual(variableResult, fixedResult);
            Assert.True(variableResult.Length < fixedResult.Length);
            Assert.Equal(6, fixedResult.Length);
            
            // 但解码后都应该得到相同的原值
            Assert.Equal(input, _converter.Decode(variableResult));
            Assert.Equal(input, _fixedLengthConverter.Decode(fixedResult));
        }

        [Fact]
        public void FixedLength_AllEncodedStrings_HaveSameLength()
        {
            // Arrange
            var testValues = new long[] { 0, 1, 61, 62, 123, 3844, 238327 };
            
            // Act & Assert
            foreach (long value in testValues)
            {
                string encoded = _fixedLengthConverter.Encode(value);
                Assert.Equal(6, encoded.Length);
            }
        }

        [Fact]
        public void Encode_DifferentIds_ShouldNotProduceSameAlias()
        {
            // Arrange
            var converter = new Base62Converter(12); // 假设 alias 长度为 12
            long id1 = 1939547950170640387;
            long id2 = 1939547950170640411;
            long id3 = 1939547950170640385;
            long id4 = 1939547950170640384;
            long id5 = 1939547950170640410;

            // Act
            string alias1 = converter.Encode(id1);
            string alias2 = converter.Encode(id2);
            string alias3 = converter.Encode(id3);
            string alias4 = converter.Encode(id4);
            string alias5 = converter.Encode(id5);

            // Assert
            Assert.NotEqual(alias1, alias2);
            Assert.NotEqual(alias1, alias3);
            Assert.NotEqual(alias1, alias4);
            Assert.NotEqual(alias1, alias5);
            Assert.NotEqual(alias2, alias3);
            Assert.NotEqual(alias2, alias4);
            Assert.NotEqual(alias2, alias5);
            Assert.NotEqual(alias3, alias4);
            Assert.NotEqual(alias3, alias5);
            Assert.NotEqual(alias4, alias5);
        }

        [Fact]
        public void Encode_KnownConflictIds_ShouldProduceDifferentAlias()
        {
            // Arrange
            var converter = new Base62Converter(12);
            long idA = 1939547950170640412; // 日志中的 id
            long idB = 1939547950170640386; // 日志中的 id

            // Act
            string aliasA = converter.Encode(idA);
            string aliasB = converter.Encode(idB);

            // Assert
            Assert.NotEqual(aliasA, aliasB);
        }
    }
}
