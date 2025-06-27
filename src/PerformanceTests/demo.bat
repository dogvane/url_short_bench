@echo off
echo DbRepository 性能测试演示
echo =======================
echo.

cd /d "%~dp0"

echo 1. 检查项目构建状态...
dotnet build --configuration Release --verbosity quiet
if %errorlevel% neq 0 (
    echo 错误：构建失败！
    echo 请检查代码是否有编译错误
    pause
    exit /b 1
)
echo    ✓ 构建成功

echo.
echo 2. 运行简单的创建测试...
echo    这将创建一个临时数据库并测试几个基本操作
echo    预计运行时间：10-30秒
echo.

echo 开始测试...
dotnet run --configuration Release --no-build -- QuickDbRepositoryBenchmark

echo.
echo 测试完成！请查看上方的性能数据。
echo.
echo 主要关注指标：
echo - Mean：平均执行时间（越小越好）
echo - Allocated：内存分配（越小越好）
echo.
echo 如需运行完整测试，请执行：run_benchmark.bat
echo.
pause
