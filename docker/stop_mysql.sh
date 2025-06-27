#!/bin/bash

echo "停止 MySQL Docker 容器..."

cd "$(dirname "$0")"

docker-compose down

echo "MySQL 容器已停止"
