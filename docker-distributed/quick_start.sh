#!/bin/bash

echo "============================================"
echo "URL短链系统 - 跨平台脚本使用说明"
echo "============================================"

echo
echo "📁 可用脚本文件："
echo
echo "Windows版本："
echo "  deploy_optimized.bat     - 优化部署脚本"
echo "  validate_performance.bat - 性能验证脚本"
echo
echo "Linux/Unix版本："
echo "  deploy_optimized.sh      - 优化部署脚本"  
echo "  validate_performance.sh  - 性能验证脚本"
echo "  setup_linux.sh          - 环境初始化脚本"
echo

echo "🚀 快速开始："
echo
echo "Windows用户："
echo "  双击运行: deploy_optimized.bat"
echo
echo "Linux/macOS用户："
echo "  chmod +x *.sh"
echo "  ./deploy_optimized.sh"
echo

echo "📊 部署选项："
echo "  1. 基础优化版本 (3个应用实例)"
echo "  2. 增强优化版本 (4个应用实例 + 读写分离)"
echo

echo "⚙️ 系统要求："
echo "  最低: 4核CPU + 8GB内存"
echo "  推荐: 8核CPU + 16GB内存"
echo

echo "🎯 预期性能提升："
echo "  响应时间: 1091ms -> 300-500ms"
echo "  吞吐量: 197 RPS -> 400-800 RPS"
echo

echo "🔍 监控面板："
echo "  Grafana: http://localhost:3000 (admin/admin123)"
echo "  Prometheus: http://localhost:9090"
echo "  应用访问: http://localhost:10086"
echo

echo "============================================"

if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
    echo "检测到Windows环境，建议使用 .bat 脚本"
    read -p "是否查看详细文档？(y/n): " show_doc
    if [[ "$show_doc" =~ ^[Yy]$ ]]; then
        echo "请查看 DEPLOYMENT_GUIDE.md 获取详细说明"
    fi
else
    echo "检测到Unix/Linux环境"
    read -p "是否立即开始环境检查和部署？(y/n): " start_setup
    if [[ "$start_setup" =~ ^[Yy]$ ]]; then
        if [ -f "setup_linux.sh" ]; then
            chmod +x setup_linux.sh
            ./setup_linux.sh
        else
            chmod +x deploy_optimized.sh
            ./deploy_optimized.sh
        fi
    fi
fi
