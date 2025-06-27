@echo off
echo.
echo ╔═══════════════════════════════════════════════════════════════════════╗
echo ║                    DbRepository 性能测试工具                          ║
echo ║                         演示 & 快速测试                               ║
echo ╚═══════════════════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

echo [1/4] 检查构建环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误：未找到 .NET SDK
    echo 请安装 .NET 9.0 或更高版本
    pause
    exit /b 1
)
echo     ✓ .NET SDK 已安装

echo.
echo [2/4] 构建性能测试项目...
dotnet build --configuration Release --verbosity quiet --no-restore
if %errorlevel% neq 0 (
    echo ❌ 错误：构建失败
    echo.
    echo 详细错误信息：
    dotnet build --configuration Release
    pause
    exit /b 1
)
echo     ✓ 构建成功

echo.
echo [3/4] 开始性能测试...
echo     📊 测试内容：
echo        • 单次创建操作（基准测试）
echo        • 单次查询操作
echo        • 批量创建10个短链接
echo        • 批量查询10个短链接
echo.
echo     ⏱️  预计运行时间：1-3分钟
echo     💡 请耐心等待，BenchmarkDotNet 需要多次运行以获得准确结果
echo.

echo ════════════════════════════════════════════════════════════════════════
dotnet run --configuration Release --no-build -- QuickDbRepositoryBenchmark
echo ════════════════════════════════════════════════════════════════════════

echo.
echo [4/4] 测试完成分析
if %errorlevel% equ 0 (
    echo     ✓ 性能测试执行成功！
    echo.
    echo 📈 如何解读结果：
    echo    • Mean：平均执行时间（数值越小越好）
    echo    • Allocated：内存分配量（数值越小越好）
    echo    • Ratio：相对于基准的倍数（CreateSingleShortLink = 1.00）
    echo.
    echo 📁 详细报告位置：
    echo    BenchmarkDotNet.Artifacts\results\
    echo.
    echo 🚀 下一步：
    echo    • 运行 run_benchmark.bat 进行完整测试
    echo    • 查看 README.md 了解更多用法
) else (
    echo ❌ 测试执行失败
    echo 请检查错误信息或联系开发者
)

echo.
echo 按任意键退出...
pause >nul
