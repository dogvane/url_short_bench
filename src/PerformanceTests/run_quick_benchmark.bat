@echo off
echo 快速性能测试工具
echo ================
echo.

cd /d "%~dp0"

echo 正在构建项目...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo 构建失败！
    pause
    exit /b 1
)

echo.
echo 运行快速性能测试...
echo 注意：这是简化版测试，运行时间约1-2分钟
echo.

dotnet run --configuration Release --no-build -- QuickDbRepositoryBenchmark

echo.
echo 快速测试完成！
pause
