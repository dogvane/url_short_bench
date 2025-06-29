#!/bin/bash
echo "正在运行 MySQL 性能优化..."

echo "1. 停止当前 MySQL 服务"
./stop_mysql.sh

echo "2. 启动优化后的 MySQL 服务"
./start_mysql.sh

echo "3. 等待 MySQL 启动..."
sleep 10

echo "4. 执行 MySQL 优化脚本"
docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword url_short < optimize_mysql.sql

echo "5. 显示优化结果"
docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword -e "SHOW STATUS LIKE 'Threads_connected'; SHOW STATUS LIKE 'Max_used_connections';"

echo "MySQL 优化完成！"
