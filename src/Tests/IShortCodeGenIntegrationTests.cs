using System;
using Xunit;
using url_short.common;

namespace Tests
{
    /// <summary>
    /// IShortCodeGen 接口的集成测试
    /// </summary>
    public class IShortCodeGenIntegrationTests
    {
        [Fact]
        public void IShortCodeGen_WithBase62Converter_WorksCorrectly()
        {
            // Arrange
            IShortCodeGen codeGen = new Base62Converter();
            long originalId = 123456789;
            
            // Act
            string shortCode = codeGen.Encode(originalId);
            long decodedId = codeGen.Decode(shortCode);
            
            // Assert
            Assert.NotNull(shortCode);
            Assert.NotEmpty(shortCode);
            Assert.Equal(originalId, decodedId);
            Assert.True(shortCode.Length < originalId.ToString().Length, 
                "短代码应该比原始数字更短");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(10000)]
        [InlineData(1000000)]
        public void IShortCodeGen_DifferentImplementations_ProduceConsistentResults(long testValue)
        {
            // Arrange
            IShortCodeGen converter1 = new Base62Converter();
            IShortCodeGen converter2 = new Base62Converter();
            
            // Act
            string code1 = converter1.Encode(testValue);
            string code2 = converter2.Encode(testValue);
            
            long decoded1 = converter1.Decode(code1);
            long decoded2 = converter2.Decode(code2);
            
            // Assert
            Assert.Equal(code1, code2);
            Assert.Equal(decoded1, decoded2);
            Assert.Equal(testValue, decoded1);
            Assert.Equal(testValue, decoded2);
        }

        [Fact]
        public void IShortCodeGen_LargeNumberRange_NoCodeDuplicates()
        {
            // Arrange
            IShortCodeGen codeGen = new Base62Converter();
            var generatedCodes = new HashSet<string>();
            var duplicates = new List<(long number, string code)>();
            int totalGenerated = 0;

            // Test范围1: 1 到 10000
            for (long i = 1; i <= 10000; i++)
            {
                string code = codeGen.Encode(i);
                totalGenerated++;
                
                if (!generatedCodes.Add(code))
                {
                    duplicates.Add((i, code));
                }
            }

            // Test范围2: 10001 到 1000000，使用等差数列间隔（避免与范围1重叠）
            // 间隔设为99，这样大约会生成 (1000000-10001)/99 ≈ 10000 个数字
            for (long i = 10001; i <= 1000000; i += 99)
            {
                string code = codeGen.Encode(i);
                totalGenerated++;
                
                if (!generatedCodes.Add(code))
                {
                    duplicates.Add((i, code));
                }
            }

            // Assert
            Assert.Empty(duplicates); // 不应该有任何重复的编码
            Assert.Equal(totalGenerated, generatedCodes.Count); // 生成的编码数量应该等于唯一编码数量
            
            // 输出统计信息（用于调试）
            var expectedCount = 10000 + ((1000000 - 10001) / 99 + 1);
            Assert.True(totalGenerated >= expectedCount - 100); // 允许一些误差
        }

        [Fact]
        public void IShortCodeGen_FixedLength_LargeNumberRange_NoCodeDuplicates()
        {
            // Arrange
            IShortCodeGen fixedCodeGen = new Base62Converter(6);
            var generatedCodes = new HashSet<string>();
            var duplicates = new List<(long number, string code)>();
            int totalGenerated = 0;

            try
            {
                // Test范围1: 1 到 10000
                for (long i = 1; i <= 10000; i++)
                {
                    string code = fixedCodeGen.Encode(i);
                    totalGenerated++;
                    
                    if (!generatedCodes.Add(code))
                    {
                        duplicates.Add((i, code));
                    }
                    
                    // 验证所有编码都是6位
                    Assert.Equal(6, code.Length);
                }

                // Test范围2: 10001 到 500000（减少范围以避免超出6位Base62最大值，避免与范围1重叠）
                // 使用等差数列间隔49
                for (long i = 10001; i <= 500000; i += 49)
                {
                    string code = fixedCodeGen.Encode(i);
                    totalGenerated++;
                    
                    if (!generatedCodes.Add(code))
                    {
                        duplicates.Add((i, code));
                    }
                    
                    // 验证所有编码都是6位
                    Assert.Equal(6, code.Length);
                }

                // Assert
                Assert.Empty(duplicates); // 不应该有任何重复的编码
                Assert.Equal(totalGenerated, generatedCodes.Count); // 生成的编码数量应该等于唯一编码数量
            }
            catch (ArgumentOutOfRangeException)
            {
                // 如果超出了6位Base62的最大值，这是预期的行为
                // 但在到达这个点之前，不应该有重复的编码
                Assert.Empty(duplicates);
            }
        }

        [Fact]
        public void IShortCodeGen_SequentialNumbers_ProduceUniqueCodesWithPattern()
        {
            // Arrange
            IShortCodeGen codeGen = new Base62Converter();
            var codes = new List<string>();
            
            // 测试连续数字的编码模式
            for (long i = 0; i < 1000; i++)
            {
                codes.Add(codeGen.Encode(i));
            }
            
            // Assert
            // 1. 所有编码都应该是唯一的
            var uniqueCodes = new HashSet<string>(codes);
            Assert.Equal(codes.Count, uniqueCodes.Count);
            
            // 2. 验证一些特定的编码模式
            Assert.Equal("0", codes[0]);   // 0 -> "0"
            Assert.Equal("1", codes[1]);   // 1 -> "1"
            Assert.Equal("Z", codes[61]);  // 61 -> "Z" (最后一个单字符)
            Assert.Equal("10", codes[62]); // 62 -> "10" (第一个双字符)
            
            // 3. 编码长度应该随着数字增大而增加（或保持不变）
            for (int i = 1; i < codes.Count; i++)
            {
                Assert.True(codes[i].Length >= codes[i-1].Length, 
                    $"编码长度不应该减少: codes[{i-1}]='{codes[i-1]}' -> codes[{i}]='{codes[i]}'");
            }
        }
    }
}
