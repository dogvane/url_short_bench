#!/usr/bin/env python3
"""
Monitoring Validation Test
验证监控系统是否正常工作的测试脚本
"""

import requests
import time
import json
import sys
from concurrent.futures import ThreadPoolExecutor
import threading

# 配置
BASE_URL = "http://localhost:10086"
PROMETHEUS_URL = "http://localhost:9090"
GRAFANA_URL = "http://localhost:3000"

# 测试统计
stats = {
    'requests_sent': 0,
    'successful_requests': 0,
    'failed_requests': 0,
    'created_urls': []
}
stats_lock = threading.Lock()

def test_create_url(url, test_id):
    """创建短链接"""
    try:
        response = requests.post(
            f"{BASE_URL}/create",
            json={"url": url},
            timeout=10
        )
        
        with stats_lock:
            stats['requests_sent'] += 1
            
        if response.status_code == 200:
            data = response.json()
            alias = data.get('alias')
            with stats_lock:
                stats['successful_requests'] += 1
                stats['created_urls'].append(alias)
            print(f"✓ Test {test_id}: Created alias '{alias}' for {url}")
            return alias
        else:
            with stats_lock:
                stats['failed_requests'] += 1
            print(f"✗ Test {test_id}: Failed to create URL - {response.status_code}")
            return None
            
    except Exception as e:
        with stats_lock:
            stats['requests_sent'] += 1
            stats['failed_requests'] += 1
        print(f"✗ Test {test_id}: Exception - {e}")
        return None

def test_redirect_url(alias, test_id):
    """测试重定向"""
    try:
        response = requests.get(
            f"{BASE_URL}/u/{alias}",
            allow_redirects=False,
            timeout=10
        )
        
        with stats_lock:
            stats['requests_sent'] += 1
            
        if response.status_code in [301, 302]:
            with stats_lock:
                stats['successful_requests'] += 1
            print(f"✓ Test {test_id}: Redirect for '{alias}' works")
            return True
        else:
            with stats_lock:
                stats['failed_requests'] += 1
            print(f"✗ Test {test_id}: Redirect failed - {response.status_code}")
            return False
            
    except Exception as e:
        with stats_lock:
            stats['requests_sent'] += 1
            stats['failed_requests'] += 1
        print(f"✗ Test {test_id}: Exception - {e}")
        return False

def check_prometheus_metrics():
    """检查 Prometheus 指标"""
    try:
        # 检查 HTTP 请求指标
        response = requests.get(
            f"{PROMETHEUS_URL}/api/v1/query",
            params={"query": "http_requests_total"},
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            if data['status'] == 'success' and data['data']['result']:
                print("✓ Prometheus: HTTP request metrics available")
                return True
        
        print("✗ Prometheus: No HTTP request metrics found")
        return False
        
    except Exception as e:
        print(f"✗ Prometheus: Connection failed - {e}")
        return False

def check_grafana():
    """检查 Grafana"""
    try:
        response = requests.get(f"{GRAFANA_URL}/api/health", timeout=10)
        if response.status_code == 200:
            print("✓ Grafana: Service is healthy")
            return True
        else:
            print(f"✗ Grafana: Unhealthy - {response.status_code}")
            return False
    except Exception as e:
        print(f"✗ Grafana: Connection failed - {e}")
        return False

def run_load_test(duration_seconds=30, concurrent_users=5):
    """运行负载测试"""
    print(f"\n🚀 Starting load test...")
    print(f"Duration: {duration_seconds} seconds")
    print(f"Concurrent users: {concurrent_users}")
    print("-" * 50)
    
    test_urls = [
        "https://www.google.com",
        "https://www.github.com",
        "https://www.stackoverflow.com",
        "https://www.microsoft.com",
        "https://www.python.org"
    ]
    
    start_time = time.time()
    test_id_counter = 0
    
    def worker():
        nonlocal test_id_counter
        while time.time() - start_time < duration_seconds:
            test_id_counter += 1
            current_test_id = test_id_counter
            
            # 创建短链接
            url = test_urls[current_test_id % len(test_urls)]
            alias = test_create_url(url, current_test_id)
            
            if alias:
                # 等待一下再测试重定向
                time.sleep(0.1)
                test_redirect_url(alias, current_test_id)
            
            # 控制请求频率
            time.sleep(0.5)
    
    # 启动并发测试
    with ThreadPoolExecutor(max_workers=concurrent_users) as executor:
        futures = [executor.submit(worker) for _ in range(concurrent_users)]
        
        # 等待所有任务完成或超时
        for future in futures:
            try:
                future.result(timeout=duration_seconds + 10)
            except Exception as e:
                print(f"Worker exception: {e}")

def main():
    print("=" * 60)
    print("     URL Shortener Monitoring Validation Test")
    print("=" * 60)
    
    # 1. 检查基础服务
    print("\n📋 Step 1: Checking basic services...")
    
    # 检查应用健康状态
    try:
        response = requests.get(f"{BASE_URL}/health", timeout=10)
        if response.status_code == 200:
            print("✓ Application: Healthy")
        else:
            print("✗ Application: Unhealthy")
            sys.exit(1)
    except Exception as e:
        print(f"✗ Application: Connection failed - {e}")
        sys.exit(1)
    
    # 检查 Prometheus
    prometheus_ok = check_prometheus_metrics()
    
    # 检查 Grafana
    grafana_ok = check_grafana()
    
    if not prometheus_ok or not grafana_ok:
        print("\n⚠️  Warning: Some monitoring services are not available")
        print("You can continue with the test, but monitoring data may be incomplete.")
        
        continue_test = input("\nContinue with the test? (y/n): ").lower()
        if continue_test != 'y':
            sys.exit(1)
    
    # 2. 基本功能测试
    print("\n📋 Step 2: Basic functionality test...")
    
    # 创建测试 URL
    test_alias = test_create_url("https://example.com", "basic")
    if not test_alias:
        print("✗ Basic functionality test failed")
        sys.exit(1)
    
    # 测试重定向
    time.sleep(1)  # 等待缓存同步
    if not test_redirect_url(test_alias, "basic"):
        print("✗ Basic functionality test failed")
        sys.exit(1)
    
    print("✓ Basic functionality test passed")
    
    # 3. 运行负载测试
    print("\n📋 Step 3: Load test for monitoring validation...")
    
    duration = 30  # 测试持续时间（秒）
    concurrent_users = 3  # 并发用户数
    
    run_load_test(duration, concurrent_users)
    
    # 4. 等待指标收集
    print("\n📋 Step 4: Waiting for metrics collection...")
    print("Waiting 10 seconds for Prometheus to scrape metrics...")
    time.sleep(10)
    
    # 5. 验证监控指标
    print("\n📋 Step 5: Validating monitoring metrics...")
    
    # 检查创建的 URL 数量指标
    try:
        response = requests.get(
            f"{PROMETHEUS_URL}/api/v1/query",
            params={"query": "sum(shorturl_created_total)"},
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            if data['status'] == 'success' and data['data']['result']:
                metric_value = float(data['data']['result'][0]['value'][1])
                print(f"✓ Prometheus: Found {metric_value} total URL creations")
            else:
                print("✗ Prometheus: No URL creation metrics found")
        else:
            print("✗ Prometheus: Query failed")
    except Exception as e:
        print(f"✗ Prometheus: Metrics validation failed - {e}")
    
    # 6. 测试结果
    print("\n" + "=" * 60)
    print("                    TEST RESULTS")
    print("=" * 60)
    
    print(f"📊 Test Statistics:")
    print(f"   Total requests sent: {stats['requests_sent']}")
    print(f"   Successful requests: {stats['successful_requests']}")
    print(f"   Failed requests: {stats['failed_requests']}")
    print(f"   Success rate: {(stats['successful_requests']/stats['requests_sent']*100):.1f}%")
    print(f"   URLs created: {len(stats['created_urls'])}")
    
    print(f"\n🔗 Access Points:")
    print(f"   Application: {BASE_URL}")
    print(f"   Prometheus: {PROMETHEUS_URL}")
    print(f"   Grafana: {GRAFANA_URL} (admin/admin123)")
    
    print(f"\n📈 Recommended Grafana Dashboards:")
    print(f"   - URL Shortener Performance Dashboard")
    print(f"   - 🔥 Stress Test Dashboard")
    
    print(f"\n🎯 Next Steps:")
    print(f"   1. Open Grafana dashboard")
    print(f"   2. Check metrics are being displayed")
    print(f"   3. Run longer performance tests")
    print(f"   4. Monitor system behavior under load")
    
    print("\n✅ Monitoring validation test completed!")

if __name__ == "__main__":
    main()
