@echo off
echo =================================
echo 简化性能测试 (最快)
echo =================================
echo.

cd /d "%~dp0"

echo ⚡ 正在运行最简化的对比测试...
echo 测试时间：约 30 秒
echo.

dotnet run -c Release simple

if %ERRORLEVEL% equ 0 (
    echo.
    echo ✅ 简化测试完成！
    echo 📊 查看 Ratio 列了解性能差异
    echo.
) else (
    echo ❌ 测试失败！
    pause
)
