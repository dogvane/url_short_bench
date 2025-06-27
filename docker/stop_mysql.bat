@echo off
echo 停止 MySQL Docker 容器...

cd /d "%~dp0"

docker-compose down

echo MySQL 容器已停止
pause
