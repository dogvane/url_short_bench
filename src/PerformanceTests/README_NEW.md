# DbRepository 性能测试工具

这是一个专门针对 `DbRepository` 类的性能测试工具，使用 BenchmarkDotNet 进行精确的性能测量。

## 快速开始

### 使用批处理文件（推荐）

```cmd
# 运行演示和快速测试（推荐首次使用）
demo_improved.bat

# 运行最简化测试（约30秒）
run_simple.bat

# 运行完整测试（约5-10分钟）
run_benchmark.bat

# 验证工具是否正常工作
verify.bat
```

### 使用 dotnet 命令

```cmd
# 构建项目
dotnet build --configuration Release

# 运行最简化测试
dotnet run --configuration Release simple

# 运行快速测试
dotnet run --configuration Release quick

# 运行完整测试
dotnet run --configuration Release

# 查看帮助
dotnet run --help
```

## 测试内容

### 最简化测试（simple）- 推荐

对比两种 `CreateShortLink` 实现的性能差异：

- **DbRepository（传统方式）**：3次数据库操作（插入临时GUID → 查询ID → 更新alias）
- **DbRepositoryAutoIncrement（优化方式）**：1次数据库操作（直接插入完整记录）

测试场景：
- 单次创建对比
- 批量创建对比（10/100次）

### 快速测试（quick）

- 单次创建和查询操作
- 小规模批量操作（10次）

### 完整测试（默认）

- 单次操作测试
- 批量操作测试（10/100/1000次）
- 并发操作测试（10/50并发）
- 混合操作测试

## 预期结果

AutoIncrement 版本应该有 **2-3倍** 的性能提升：

- **响应时间**：从 ~170μs 降低到 ~80μs
- **内存分配**：减少 ~40%
- **数据库操作**：从 3次 减少到 1次

## 结果解读

### 关键指标

- **Mean**：平均执行时间（最重要）
- **Ratio**：相对于基准的倍数
- **Allocated**：内存分配量

### 性能等级参考

- **优秀**：单次操作 < 1ms，批量操作 < 10ms/100条
- **良好**：单次操作 < 5ms，批量操作 < 50ms/100条
- **需优化**：超过上述标准

## 故障排除

### 常见问题

**问题：构建失败**
- 解决：确保安装了 .NET 9.0 或更高版本

**问题：数据库文件锁定**
- 解决：关闭所有数据库连接，重启测试

**问题：测试时间过长**
- 解决：使用 `run_simple.bat` 进行快速测试

## 技术详情

### 测试环境

- 使用 SQLite 内存数据库
- WAL 模式，同步关闭
- 自动清理测试数据

### 性能优化

- 数据库连接池
- 预编译SQL语句
- 内存中维护ID计数器（AutoIncrement版本）

## 文件说明

- `DbRepositoryBenchmark.cs` - 完整性能测试类
- `QuickBenchmark.cs` - 快速测试类
- `SimpleCreateShortLinkBenchmark.cs` - 简化对比测试类
- `Program.cs` - 程序入口和命令解析
