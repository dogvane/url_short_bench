#!/bin/bash

# URL Short Bench 性能测试脚本 (WSL版本)

set -e

echo "==========================================="
echo "URL Short Bench - 性能测试脚本"
echo "==========================================="

# 配置参数
BASE_URL="http://localhost:8080"
TEST_DURATION="60s"
WARMUP_DURATION="10s"

# 检查依赖
check_dependencies() {
    echo "检查依赖..."
    
    if ! command -v curl &> /dev/null; then
        echo "错误: 需要安装 curl"
        exit 1
    fi
    
    if ! command -v docker &> /dev/null; then
        echo "错误: 需要安装 docker"
        exit 1
    fi
    
    echo "依赖检查完成"
}

# 启动服务
start_services() {
    echo "启动服务..."
    
    # 进入 docker 目录
    cd "$(dirname "$0")"
    
    # 优化 MySQL 配置
    echo "优化 MySQL 配置..."
    chmod +x optimize_performance.sh
    ./optimize_performance.sh
    
    # 等待服务完全启动
    echo "等待服务启动..."
    sleep 15
    
    # 检查服务状态
    if curl -s "$BASE_URL/metrics" > /dev/null; then
        echo "✓ 应用服务已启动"
    else
        echo "✗ 应用服务启动失败"
        exit 1
    fi
}

# 预热测试
warmup_test() {
    echo "开始预热测试 (10秒)..."
    
    for i in {1..50}; do
        curl -s -X POST "$BASE_URL/create" \
            -H "Content-Type: application/json" \
            -d "{\"url\":\"https://example.com/warmup/$i\"}" > /dev/null
        
        if [ $((i % 10)) -eq 0 ]; then
            echo "预热进度: $i/50"
        fi
    done
    
    echo "预热完成"
}

# 性能测试函数
run_performance_test() {
    local users=$1
    local duration=$2
    
    echo "==================="
    echo "测试配置: $users 用户, $duration"
    echo "==================="
    
    # 创建测试脚本
    cat > /tmp/load_test.sh << EOF
#!/bin/bash
for i in \$(seq 1 \$1); do
    while true; do
        curl -s -X POST "$BASE_URL/create" \\
            -H "Content-Type: application/json" \\
            -d "{\"url\":\"https://example.com/test/\$RANDOM\"}" > /dev/null
        sleep 0.01  # 10ms 间隔
    done &
done
wait
EOF
    
    chmod +x /tmp/load_test.sh
    
    # 记录开始时间
    start_time=$(date +%s)
    
    # 启动负载测试
    timeout $duration /tmp/load_test.sh $users &
    TEST_PID=$!
    
    # 监控性能
    echo "开始监控..."
    sleep 5  # 等待负载稳定
    
    # 收集指标
    echo "收集性能指标..."
    
    # MySQL 状态
    mysql_connections=$(docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword -e "SHOW STATUS LIKE 'Threads_connected';" 2>/dev/null | tail -n 1 | awk '{print $2}')
    mysql_queries=$(docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword -e "SHOW STATUS LIKE 'Questions';" 2>/dev/null | tail -n 1 | awk '{print $2}')
    
    # 等待测试完成
    wait $TEST_PID 2>/dev/null || true
    
    # 计算测试结果
    end_time=$(date +%s)
    test_duration=$((end_time - start_time))
    
    # 获取最终指标
    final_queries=$(docker exec -i url_short_bench-mysql-1 mysql -uroot -ppassword -e "SHOW STATUS LIKE 'Questions';" 2>/dev/null | tail -n 1 | awk '{print $2}')
    total_queries=$((final_queries - mysql_queries))
    qps=$((total_queries / test_duration))
    
    echo "==================="
    echo "测试结果 ($users 用户):"
    echo "测试时长: ${test_duration}s"
    echo "总查询数: $total_queries"
    echo "平均 QPS: $qps"
    echo "活跃连接数: $mysql_connections"
    echo "==================="
    
    # 应用程序指标
    echo "应用程序指标:"
    curl -s "$BASE_URL/metrics" | grep -E "(http_requests_total|database_operations)" | head -5
    
    echo
}

# 主测试流程
main() {
    check_dependencies
    start_services
    warmup_test
    
    echo "开始性能测试..."
    
    # 测试不同用户数
    run_performance_test 50 "30s"
    sleep 5
    
    run_performance_test 100 "60s"
    sleep 5
    
    run_performance_test 200 "60s"
    sleep 5
    
    run_performance_test 300 "60s"
    
    echo "所有测试完成！"
    echo "查看详细监控: ./monitor_performance.sh"
}

# 清理函数
cleanup() {
    echo "清理测试环境..."
    pkill -f load_test.sh 2>/dev/null || true
    rm -f /tmp/load_test.sh
}

# 设置清理陷阱
trap cleanup EXIT

# 运行主函数
main "$@"
