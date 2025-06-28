# 性能测试脚本说明

## 可用的 Bat 脚本

### 主要测试脚本

1. **demo_improved.bat** - 推荐的演示脚本
   - 功能最全面，包含构建检查、演示和快速测试
   - 用户友好的界面和详细的反馈
   - 适合首次使用或演示用途

2. **run_simple.bat** - 最快的测试脚本
   - 运行最简化的性能对比测试
   - 测试时间约30秒
   - 适合快速验证性能

3. **run_benchmark.bat** - 完整的性能测试
   - 运行全面的性能基准测试
   - 测试时间较长（几分钟）
   - 生成详细的性能报告

### 工具脚本

1. **verify.bat** - 验证脚本
   - 验证性能测试工具是否正常工作
   - 检查项目构建状态
   - 列出所有可用命令

## 推荐使用顺序

1. 首次使用：运行 `verify.bat` 验证环境
2. 快速测试：运行 `run_simple.bat`
3. 演示用途：运行 `demo_improved.bat`
4. 完整分析：运行 `run_benchmark.bat`

## 已清理的文件

以下空文件或重复文件已被删除：

- `demo.bat` （由 `demo_improved.bat` 替代）
- `run_createshortlink_benchmark.bat` （空文件）
- `run_createshortlink_quick.bat` （空文件）
- `run_simple_createshortlink.bat` （空文件）
- `run_compare.bat` （空文件）
- `run_quick_benchmark.bat` （功能重复）
