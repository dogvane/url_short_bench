#!/bin/bash

echo "==========================================="
echo "URL Short Bench - 性能监控面板"
echo "==========================================="

monitor_loop() {
    while true; do
        clear
        echo "[$(date)] 性能监控数据:"
        echo

        echo "=== MySQL 连接状态 ==="
        docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword -e "SHOW STATUS LIKE 'Threads_connected'; SHOW STATUS LIKE 'Max_used_connections'; SHOW STATUS LIKE 'Slow_queries';" 2>/dev/null

        echo
        echo "=== MySQL 进程列表 (前10个) ==="
        docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword -e "SHOW PROCESSLIST;" 2>/dev/null | head -n 10

        echo
        echo "=== InnoDB 状态 ==="
        docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword -e "SHOW STATUS LIKE 'Innodb_rows%';" 2>/dev/null

        echo
        echo "=== 应用程序监控 (Prometheus 指标) ==="
        curl -s http://localhost:8080/metrics 2>/dev/null | grep -E "(http_requests_total|database_operations|active_connections)" || echo "无法获取应用指标"

        echo
        echo "=== 系统资源 ==="
        echo "CPU 使用率:"
        top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1
        echo "内存使用情况:"
        free -h | grep "Mem:"

        echo
        echo "=== Docker 容器状态 ==="
        docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" | head -n 5

        echo
        echo "按 Ctrl+C 退出监控，或等待 5 秒自动刷新..."
        sleep 5
    done
}

# 捕获 Ctrl+C 信号
trap 'echo "监控已停止"; exit 0' INT

monitor_loop
