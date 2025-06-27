@echo off
echo Rebuilding and restarting URL Short Service...
echo.

echo Stopping services...
docker-compose down

echo.
echo Removing old images (optional)...
docker-compose build --no-cache urlshort-app-1 urlshort-app-2 urlshort-app-3

echo.
echo Starting services with fresh build...
docker-compose up -d

echo.
echo Waiting for services to be healthy...
timeout /t 60 /nobreak

echo.
echo Service status:
docker-compose ps

echo.
echo Testing health endpoints...
echo Application Status:
curl -s http://localhost/health || echo Application not ready

echo.
echo Rebuild completed. Check logs with monitor.bat if needed.
pause
