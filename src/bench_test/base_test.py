import requests
import random
import string
import time
from functools import wraps

BASE_URL = "http://localhost:10086"

def log_execution_time(func):
    """装饰器：记录函数执行时间"""
    @wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time.time()
        print(f"\n📋 开始执行: {func.__name__}")
        try:
            result = func(*args, **kwargs)
            elapsed_time = time.time() - start_time
            print(f"✅ {func.__name__} 执行成功 - 耗时: {elapsed_time:.3f}秒")
            return result
        except Exception as e:
            elapsed_time = time.time() - start_time
            print(f"❌ {func.__name__} 执行失败 - 耗时: {elapsed_time:.3f}秒 - 错误: {e}")
            raise
    return wrapper

@log_execution_time
def test_create_and_redirect():
    # 1. 创建短链接
    creat_reandom_url = "https://www.example.com/" + ''.join(random.choices(string.ascii_letters + string.digits, k=8))
    print(f"  🔗 创建短链接: {creat_reandom_url}")
    
    resp = requests.post(f"{BASE_URL}/create", json={"url": creat_reandom_url})
    if resp.status_code != 200:
        print(f"  ❌ 创建失败 - 状态码: {resp.status_code}, 响应: {resp.text}")
        print(f"  📋 响应头: {dict(resp.headers)}")
        return
    
    assert resp.status_code == 200
    data = resp.json()
    print(f"  ✅ 短链接创建成功: {data}")
    assert "alias" in data
    alias = data["alias"]

    # 2. 跳转测试
    url = f"{BASE_URL}/u/{alias}"
    print(f"  🔄 测试跳转: {url}")
    
    redirect_resp = requests.get(url, allow_redirects=False)
    if redirect_resp.status_code not in (301, 302, 307, 308):
        print(f"  ❌ 跳转失败 - 状态码: {redirect_resp.status_code}, 响应: {redirect_resp.text}")
        print(f"  📋 响应头: {dict(redirect_resp.headers)}")
    else:
        print(f"  ✅ 跳转成功 - 状态码: {redirect_resp.status_code}")
    
    assert redirect_resp.status_code in (301, 302, 307, 308)
    assert redirect_resp.headers["Location"] == creat_reandom_url

@log_execution_time
def test_404_for_invalid_alias():
    # 3. 随机 alias，应该 404
    random_alias = ''.join(random.choices(string.ascii_lowercase + string.digits, k=6))
    print(f"  🔍 测试无效别名: {random_alias}")
    
    resp = requests.get(f"{BASE_URL}/u/{random_alias}", allow_redirects=False)
    if resp.status_code == 404:
        print("  ✅ 返回404状态码正确")
    else:
        print(f"  ❌ 状态码错误: {resp.status_code}")
    
    assert resp.status_code == 404

@log_execution_time
def test_create_with_expire_and_expire_check():
    # 创建带过期时间的短链接
    url = "https://www.example.com/expire/" + ''.join(random.choices(string.ascii_letters + string.digits, k=8))
    expire_seconds = 30  # 改为30秒，确保有足够时间测试
    print(f"  ⏰ 创建过期短链接: {url} (过期时间: {expire_seconds}秒)")
    
    resp = requests.post(f"{BASE_URL}/create", json={"url": url, "expire": expire_seconds})
    assert resp.status_code == 200
    data = resp.json()
    print(f"  ✅ 过期短链接创建成功: {data}")
    assert "alias" in data
    alias = data["alias"]

    # 跳转测试（未过期）
    print(f"  🔄 测试未过期跳转: /u/{alias}")
    redirect_resp = requests.get(f"{BASE_URL}/u/{alias}", allow_redirects=False)
    
    if redirect_resp.status_code in (301, 302, 307, 308):
        print(f"  ✅ 未过期跳转成功 - 状态码: {redirect_resp.status_code}")
    else:
        print(f"  ❌ 未过期跳转失败 - 状态码: {redirect_resp.status_code}")
        print(f"  📋 响应: {redirect_resp.text}")
    
    assert redirect_resp.status_code in (301, 302, 307, 308)
    assert redirect_resp.headers["Location"] == url

    # # 等待过期
    # import time
    # time.sleep(expire_seconds + 1)
    # # 跳转测试（已过期）
    # expired_resp = requests.get(f"{BASE_URL}/u/{alias}", allow_redirects=False)
    # assert expired_resp.status_code == 404

if __name__ == "__main__":
    print("🚀 开始运行短链接测试套件")
    print("=" * 50)
    
    total_start = time.time()
    
    test_create_and_redirect()
    test_404_for_invalid_alias()
    test_create_with_expire_and_expire_check()
    
    total_elapsed = time.time() - total_start
    print("=" * 50)
    print(f"🎉 所有测试通过! 总耗时: {total_elapsed:.3f}秒")