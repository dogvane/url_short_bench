@echo off
echo Starting Create-Only Performance Test...
echo.

REM 设置测试参数
set HOST=http://localhost:10086
set USERS=50
set SPAWN_RATE=10
set RUN_TIME=2m

echo Testing Configuration:
echo Host: %HOST%
echo Users: %USERS%
echo Spawn Rate: %SPAWN_RATE%
echo Run Time: %RUN_TIME%
echo.

REM 启动测试
locust -f create_only_test.py --host=%HOST% -u %USERS% -r %SPAWN_RATE% --headless --csv=create_report --run-time %RUN_TIME%

echo.
echo Test completed! Check create_report files for results.
pause
