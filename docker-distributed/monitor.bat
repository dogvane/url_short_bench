@echo off
echo URL Short Service - Monitoring Dashboard
echo =====================================
echo.

:MENU
echo Choose an option:
echo 1. View service status
echo 2. View logs (all services)
echo 3. View nginx logs
echo 4. View app logs
echo 5. View mysql logs
echo 6. View redis logs
echo 7. View resource usage
echo 8. Test service endpoints
echo 9. Exit
echo.
set /p choice=Enter your choice (1-9): 

if "%choice%"=="1" goto STATUS
if "%choice%"=="2" goto LOGS_ALL
if "%choice%"=="3" goto LOGS_NGINX
if "%choice%"=="4" goto LOGS_APP
if "%choice%"=="5" goto LOGS_MYSQL
if "%choice%"=="6" goto LOGS_REDIS
if "%choice%"=="7" goto RESOURCES
if "%choice%"=="8" goto TEST
if "%choice%"=="9" goto EXIT

echo Invalid choice. Please try again.
goto MENU

:STATUS
echo.
echo === Service Status ===
docker-compose ps
echo.
pause
goto MENU

:LOGS_ALL
echo.
echo === All Service Logs (last 50 lines) ===
docker-compose logs --tail=50
echo.
pause
goto MENU

:LOGS_NGINX
echo.
echo === Nginx Logs (last 50 lines) ===
docker-compose logs --tail=50 nginx
echo.
pause
goto MENU

:LOGS_APP
echo.
echo === Application Logs (last 50 lines) ===
docker-compose logs --tail=50 urlshort-app-1 urlshort-app-2 urlshort-app-3
echo.
pause
goto MENU

:LOGS_MYSQL
echo.
echo === MySQL Logs (last 50 lines) ===
docker-compose logs --tail=50 mysql
echo.
pause
goto MENU

:LOGS_REDIS
echo.
echo === Redis Logs (last 50 lines) ===
docker-compose logs --tail=50 redis
echo.
pause
goto MENU

:RESOURCES
echo.
echo === Resource Usage ===
docker stats --no-stream
echo.
pause
goto MENU

:TEST
echo.
echo === Testing Service Endpoints ===
echo.
echo Testing health endpoint...
curl -s http://localhost/health
echo.
echo.
echo Testing create endpoint...
curl -X POST http://localhost/create -H "Content-Type: application/json" -d "{\"url\":\"https://www.example.com\"}"
echo.
echo.
pause
goto MENU

:EXIT
echo Goodbye!
exit
