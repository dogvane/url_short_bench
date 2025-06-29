#!/bin/bash

# URL Short Bench 一键部署脚本 (WSL版本)

set -e

echo "==========================================="
echo "URL Short Bench - 一键部署脚本"
echo "==========================================="

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

# 打印彩色消息
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查依赖
check_dependencies() {
    print_status "检查系统依赖..."
    
    # 检查 Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker 未安装，请先安装 Docker"
        exit 1
    fi
    
    # 检查 Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose 未安装，请先安装 Docker Compose"
        exit 1
    fi
    
    # 检查 .NET
    if ! command -v dotnet &> /dev/null; then
        print_warning ".NET SDK 未安装，正在安装..."
        # 安装 .NET SDK (Ubuntu/Debian)
        if command -v apt-get &> /dev/null; then
            sudo apt-get update
            sudo apt-get install -y dotnet-sdk-8.0
        else
            print_error "请手动安装 .NET 8.0 SDK"
            exit 1
        fi
    fi
    
    print_status "依赖检查完成"
}

# 构建应用程序
build_application() {
    print_status "构建应用程序..."
    
    cd ../src/v3
    
    # 清理之前的构建
    dotnet clean
    
    # 恢复包
    dotnet restore
    
    # 构建项目
    dotnet build -c Release
    
    print_status "应用程序构建完成"
    
    cd ../../docker
}

# 启动数据库服务
start_database() {
    print_status "启动数据库服务..."
    
    # 停止现有服务
    docker-compose down 2>/dev/null || true
    
    # 启动 MySQL
    docker-compose up -d mysql
    
    # 等待 MySQL 启动
    print_status "等待 MySQL 启动..."
    local attempt=0
    local max_attempts=30
    
    while [ $attempt -lt $max_attempts ]; do
        if docker exec url_short_mysql mysql -uroot -proot123456 -e "SELECT 1" >/dev/null 2>&1; then
            print_status "MySQL 启动成功"
            break
        fi
        
        attempt=$((attempt + 1))
        echo "等待 MySQL... ($attempt/$max_attempts)"
        sleep 2
    done
    
    if [ $attempt -eq $max_attempts ]; then
        print_error "MySQL 启动超时"
        exit 1
    fi
}

# 优化数据库
optimize_database() {
    print_status "优化数据库配置..."
    
    # 执行优化脚本
    docker exec -i url_short_mysql mysql -uroot -proot123456 urlshort < optimize_mysql.sql
    
    print_status "数据库优化完成"
}

# 启动应用程序
start_application() {
    print_status "启动应用程序..."
    
    cd ../src/v3
    
    # 设置环境变量
    export ASPNETCORE_ENVIRONMENT=Production
    export ASPNETCORE_URLS="http://0.0.0.0:8080"
    
    # 启动应用 (后台运行)
    nohup dotnet run -c Release > ../../docker/app.log 2>&1 &
    APP_PID=$!
    
    echo $APP_PID > ../../docker/app.pid
    
    # 等待应用启动
    print_status "等待应用程序启动..."
    local attempt=0
    local max_attempts=20
    
    while [ $attempt -lt $max_attempts ]; do
        if curl -s http://localhost:8080/metrics > /dev/null 2>&1; then
            print_status "应用程序启动成功 (PID: $APP_PID)"
            break
        fi
        
        attempt=$((attempt + 1))
        echo "等待应用程序... ($attempt/$max_attempts)"
        sleep 3
    done
    
    if [ $attempt -eq $max_attempts ]; then
        print_error "应用程序启动超时"
        exit 1
    fi
    
    cd ../../docker
}

# 验证部署
verify_deployment() {
    print_status "验证部署..."
    
    # 测试创建短链接
    response=$(curl -s -X POST http://localhost:8080/create \
        -H "Content-Type: application/json" \
        -d '{"url":"https://example.com/test"}')
    
    if echo "$response" | grep -q "alias"; then
        print_status "✓ 短链接创建测试通过"
    else
        print_error "✗ 短链接创建测试失败"
        echo "响应: $response"
        exit 1
    fi
    
    # 检查指标端点
    if curl -s http://localhost:8080/metrics | grep -q "http_requests_total"; then
        print_status "✓ 指标端点正常"
    else
        print_warning "指标端点可能有问题"
    fi
}

# 显示状态信息
show_status() {
    echo
    echo "==========================================="
    echo "部署完成！"
    echo "==========================================="
    echo "应用程序地址: http://localhost:8080"
    echo "指标监控地址: http://localhost:8080/metrics"
    echo
    echo "常用命令:"
    echo "  查看应用日志: tail -f app.log"
    echo "  性能监控: ./monitor_performance.sh"
    echo "  性能测试: ./performance_test.sh"
    echo "  停止应用: kill \$(cat app.pid)"
    echo "  停止数据库: docker-compose down"
    echo
    echo "数据库连接信息:"
    echo "  主机: localhost:3306"
    echo "  数据库: urlshort"
    echo "  用户名: urlshort"
    echo "  密码: urlshort123"
    echo "==========================================="
}

# 清理函数
cleanup() {
    if [ -f app.pid ]; then
        local pid=$(cat app.pid)
        if kill -0 $pid 2>/dev/null; then
            print_warning "清理应用进程 $pid"
            kill $pid
        fi
        rm -f app.pid
    fi
}

# 主函数
main() {
    # 进入脚本目录
    cd "$(dirname "$0")"
    
    # 设置陷阱
    trap cleanup EXIT
    
    check_dependencies
    build_application
    start_database
    optimize_database
    start_application
    verify_deployment
    show_status
}

# 如果脚本被直接执行
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
