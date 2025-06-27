@echo off
echo Starting URL Short Service with distributed architecture...
echo.

echo Pulling latest images...
docker-compose pull

echo.
echo Starting services...
docker-compose up -d

echo.
echo Waiting for MySQL to be healthy...
:wait_mysql
docker-compose exec mysql mysqladmin ping -h localhost -u urlshort -purlshort123 > nul 2>&1
if errorlevel 1 (
    echo MySQL not ready yet, waiting...
    timeout /t 5 /nobreak > nul
    goto wait_mysql
)
echo MySQL is ready!

echo.
echo Waiting for Redis to be healthy...
:wait_redis
docker-compose exec redis redis-cli ping > nul 2>&1
if errorlevel 1 (
    echo Redis not ready yet, waiting...
    timeout /t 2 /nobreak > nul
    goto wait_redis
)
echo Redis is ready!

echo.
echo Waiting for applications to be healthy...
timeout /t 30 /nobreak

echo.
echo Service status:
docker-compose ps

echo.
echo Nginx is running on port 80
echo MySQL is running on port 3306
echo Redis is running on port 6379
echo App instances are running on ports 8081, 8082, 8083

echo.
echo Testing service health...
curl -s http://localhost/health || echo Application health check failed

echo.
echo Distributed URL Short Service is now running!
echo Create short URL: POST http://localhost/create
echo Access short URL: GET http://localhost/u/{alias}
echo Use monitor.bat for troubleshooting

pause
