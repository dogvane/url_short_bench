@echo off
echo 正在验证性能测试工具...

cd /d "%~dp0"

echo 1. 构建项目...
dotnet build --configuration Release --verbosity quiet
if %errorlevel% neq 0 goto :error

echo 2. 检查帮助功能...
dotnet run --configuration Release --no-build -- --help
if %errorlevel% neq 0 goto :error

echo.
echo ✅ 验证成功！性能测试工具已就绪
echo.
echo 可用命令：
echo   demo_improved.bat    - 运行演示和快速测试
echo   run_quick_benchmark.bat - 仅运行快速测试
echo   run_benchmark.bat    - 运行完整测试
echo.
pause
exit /b 0

:error
echo ❌ 验证失败，请检查错误信息
pause
exit /b 1
