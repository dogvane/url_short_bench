#!/bin/bash

echo "启动 MySQL Docker 容器..."

# 切换到 docker 目录
cd "$(dirname "$0")"

# 强制停止并删除现有容器（如果存在）
echo "停止现有容器..."
docker stop url_short_mysql 2>/dev/null || true
docker rm url_short_mysql 2>/dev/null || true

# 清理并重新启动
echo "启动新的 MySQL 容器..."
docker-compose up -d

# 等待 MySQL 启动
echo "等待 MySQL 启动完成..."
sleep 30

# 检查容器状态
echo "检查容器状态："
docker-compose ps

# 检查日志
echo "最近的日志："
docker-compose logs --tail=20 mysql

echo ""
echo "MySQL 容器启动完成！"
echo "连接信息："
echo "  Host: localhost"
echo "  Port: 3306"
echo "  Database: urlshort"
echo "  Username: urlshort"
echo "  Password: urlshort123"
echo "  Root Password: root123456"
echo ""
echo "连接字符串: Server=localhost;Port=3306;Database=urlshort;Uid=urlshort;Pwd=urlshort123;CharSet=utf8mb4;"
