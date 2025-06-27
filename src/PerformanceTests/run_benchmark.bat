@echo off
echo DbRepository 性能测试工具
echo ========================
echo.

cd /d "%~dp0"

echo 正在构建性能测试项目...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo 构建失败！
    pause
    exit /b 1
)

echo.
echo 开始运行性能测试...
echo 注意：测试可能需要几分钟时间，请耐心等待...
echo.

dotnet run --configuration Release --no-build

echo.
echo 性能测试完成！
echo 查看结果文件：
echo   - BenchmarkDotNet.Artifacts\results\ 目录
echo   - 包含 HTML、Markdown、CSV 等格式的报告
echo.
pause
