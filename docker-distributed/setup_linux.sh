#!/bin/bash

# 为脚本添加执行权限
chmod +x deploy_optimized.sh
chmod +x validate_performance.sh

echo "============================================"
echo "Linux/Unix 部署脚本设置完成"
echo "============================================"
echo
echo "可用脚本："
echo "1. ./deploy_optimized.sh    - 性能优化部署脚本"
echo "2. ./validate_performance.sh - 性能验证脚本"
echo
echo "快速开始："
echo "  ./deploy_optimized.sh"
echo
echo "============================================"

# 检查必要的依赖
echo "检查系统依赖..."
echo

if command -v docker >/dev/null 2>&1; then
    echo "[OK] Docker已安装"
    docker --version
else
    echo "[ERROR] Docker未安装，请先安装Docker"
    echo "       Ubuntu/Debian: sudo apt-get install docker.io"
    echo "       CentOS/RHEL: sudo yum install docker"
    exit 1
fi

if command -v docker-compose >/dev/null 2>&1; then
    echo "[OK] Docker Compose已安装"
    docker-compose --version
else
    echo "[ERROR] Docker Compose未安装，请先安装"
    echo "       sudo curl -L \"https://github.com/docker/compose/releases/download/1.29.2/docker-compose-\$(uname -s)-\$(uname -m)\" -o /usr/local/bin/docker-compose"
    echo "       sudo chmod +x /usr/local/bin/docker-compose"
    exit 1
fi

if command -v curl >/dev/null 2>&1; then
    echo "[OK] curl已安装"
else
    echo "[WARNING] curl未安装，部分功能可能受限"
    echo "          Ubuntu/Debian: sudo apt-get install curl"
    echo "          CentOS/RHEL: sudo yum install curl"
fi

echo
echo "系统检查完成！可以开始部署。"
echo

read -p "是否立即开始优化部署？(y/n): " start_deploy
if [[ "$start_deploy" =~ ^[Yy]$ ]]; then
    echo
    ./deploy_optimized.sh
fi
