@echo off
echo Restarting URL Short Service...
echo.

echo Stopping services...
docker-compose down

echo.
echo Rebuilding application services...
docker-compose build urlshort-app-1 urlshort-app-2 urlshort-app-3

echo.
echo Starting services with health checks...
docker-compose up -d

echo.
echo Waiting for services to be healthy...
timeout /t 60 /nobreak

echo.
echo Service status:
docker-compose ps

echo.
echo Testing health endpoints...
echo MySQL Status:
docker-compose exec mysql mysqladmin ping -h localhost -u urlshort -purlshort123 || echo MySQL not ready

echo.
echo Redis Status:
docker-compose exec redis redis-cli ping || echo Redis not ready

echo.
echo Application Status:
curl -s http://localhost/health || echo Application not ready

echo.
echo Restart completed. Check logs with monitor.bat if needed.
pause
