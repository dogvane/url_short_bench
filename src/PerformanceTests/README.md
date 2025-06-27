# DbRepository 性能测试工具

这是一个专门针对 `DbRepository` 类的性能测试工具，使用 BenchmarkDotNet 进行精确的性能测量。

## 快速开始

### 方式一：使用批处理文件（推荐）

```cmd
# 运行快速测试（约1-2分钟）
run_quick_benchmark.bat

# 运行完整测试（约5-10分钟）
run_benchmark.bat
```

### 方式二：使用 dotnet 命令

```cmd
# 构建项目
dotnet build --configuration Release

# 运行快速测试
dotnet run --configuration Release QuickDbRepositoryBenchmark

# 运行完整测试
dotnet run --configuration Release

# 查看帮助
dotnet run --help
```

## 测试内容

### 快速测试（QuickDbRepositoryBenchmark）
- **单次创建操作**：测试创建单个短链接的性能（作为基准）
- **单次查询操作**：测试查询单个短链接的性能
- **批量创建（10次）**：测试批量创建10个短链接的性能
- **批量查询（10次）**：测试批量查询10个短链接的性能

### 完整测试（DbRepositoryBenchmark）
- **单次操作测试**：创建和查询单个短链接
- **批量操作测试**：不同规模的批量操作（10/100/1000）
- **并发操作测试**：不同并发度的操作（10/50）
- **混合操作测试**：创建和查询操作混合执行

## 性能指标说明

### 主要指标
- **Mean**：平均执行时间，这是最重要的指标
- **Error**：标准误差，表示测量的不确定性
- **StdDev**：标准差，表示结果的稳定性
- **Median**：中位数，50% 的操作在此时间内完成
- **Allocated**：内存分配量，用于评估内存效率

### 性能等级参考
- **优秀**：单次操作 < 1ms，批量操作 < 10ms/100条
- **良好**：单次操作 < 5ms，批量操作 < 50ms/100条
- **一般**：单次操作 < 10ms，批量操作 < 100ms/100条
- **需优化**：超过上述标准

## 输出文件

测试完成后，会在 `BenchmarkDotNet.Artifacts` 目录生成以下文件：

```
BenchmarkDotNet.Artifacts/
├── results/
│   ├── PerformanceTests.DbRepositoryBenchmark-report.html     # 图表报告
│   ├── PerformanceTests.DbRepositoryBenchmark-report.csv      # CSV 数据
│   ├── PerformanceTests.DbRepositoryBenchmark-report.md       # Markdown 报告
│   └── PerformanceTests.DbRepositoryBenchmark-measurements.csv # 原始测量数据
└── logs/
    └── *.log                                                  # 运行日志
```

## 使用建议

### 测试环境准备
1. **关闭不必要的程序**：确保系统资源充足
2. **使用 Release 模式**：始终使用 Release 配置进行测试
3. **多次运行**：运行多次测试以获得稳定结果
4. **环境一致性**：在相同环境下比较不同版本的性能

### 结果分析
1. **关注 Mean 值**：这是最重要的性能指标
2. **检查 StdDev**：较小的标准差表示结果更稳定
3. **对比基准**：使用快速测试的单次创建作为基准
4. **内存分析**：关注 Allocated 指标，避免过度内存分配

### 性能优化建议
1. **数据库配置**：WAL 模式、同步关闭、内存临时表
2. **连接管理**：使用连接池，避免频繁创建连接
3. **批量操作**：尽可能使用批量插入而非单条插入
4. **索引优化**：为 alias 字段创建唯一索引
5. **并发控制**：合理使用锁，避免过度串行化

## 故障排除

### 常见问题

**问题：测试运行时间过长**
- 解决：使用快速测试版本，或减少测试参数

**问题：内存不足**
- 解决：减少批量操作的数量，或增加系统内存

**问题：结果不稳定**
- 解决：关闭其他程序，确保系统负载稳定

**问题：数据库锁定**
- 解决：确保没有其他程序访问测试数据库

### 环境要求
- .NET 9.0 或更高版本
- Windows/Linux/macOS
- 至少 2GB 可用内存
- 足够的磁盘空间（用于临时数据库文件）

## 扩展测试

如需添加新的测试场景，可以在 `DbRepositoryBenchmark.cs` 中添加新的 `[Benchmark]` 方法：

```csharp
[Benchmark]
[Arguments(100)]
public void YourCustomTest(int count)
{
    // 你的测试代码
}
```

## 技术实现

- **测试框架**：BenchmarkDotNet 0.14.0
- **数据库**：SQLite（临时文件）
- **ORM**：Dapper
- **运行时**：.NET 9.0
- **测试策略**：多次迭代，统计分析
