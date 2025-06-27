@echo off
echo MySQL Debug Script
echo ==================
echo.

echo Stopping any existing containers...
docker-compose down
docker-compose -f docker-compose.debug.yml down

echo.
echo Cleaning up volumes...
docker volume rm docker-distributed_mysql_data_debug 2>nul
docker volume rm docker-distributed_redis_data_debug 2>nul

echo.
echo Starting MySQL only for debugging...
docker-compose -f docker-compose.debug.yml up -d mysql

echo.
echo Waiting for MySQL to start...
timeout /t 10 /nobreak

echo.
echo Checking MySQL container status...
docker-compose -f docker-compose.debug.yml ps

echo.
echo Checking MySQL logs...
docker-compose -f docker-compose.debug.yml logs mysql

echo.
echo Testing MySQL connection...
timeout /t 30 /nobreak
docker-compose -f docker-compose.debug.yml exec mysql mysqladmin ping -h localhost

echo.
echo If MySQL is healthy, starting Redis...
docker-compose -f docker-compose.debug.yml up -d redis

echo.
echo Testing Redis connection...
timeout /t 10 /nobreak
docker-compose -f docker-compose.debug.yml exec redis redis-cli ping

echo.
echo If both are healthy, starting application...
docker-compose -f docker-compose.debug.yml up -d urlshort-app-1

echo.
echo Final status check...
docker-compose -f docker-compose.debug.yml ps

echo.
echo Application logs...
docker-compose -f docker-compose.debug.yml logs urlshort-app-1

pause
