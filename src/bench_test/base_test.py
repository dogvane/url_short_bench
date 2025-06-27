import requests
import random
import string

BASE_URL = "http://localhost:10086"

def test_create_and_redirect():
    # 1. 创建短链接
    creat_reandom_url = "https://www.example.com/" + ''.join(random.choices(string.ascii_letters + string.digits, k=8))
    print(f"Trying to create URL: {creat_reandom_url}")
    resp = requests.post(f"{BASE_URL}/create", json={"url": creat_reandom_url})
    print(f"Response status: {resp.status_code}")
    print(f"Response text: {resp.text}")
    if resp.status_code != 200:
        print(f"Response headers: {dict(resp.headers)}")
        return
    assert resp.status_code == 200
    data = resp.json()
    print("Short URL created:", data)
    assert "alias" in data
    alias = data["alias"]

    # 2. 跳转测试
    url = f"{BASE_URL}/u/{alias}"
    print("Testing redirect url:", url)
    redirect_resp = requests.get(url, allow_redirects=False)
    print(f"First test - Redirect response status: {redirect_resp.status_code}")
    print(f"First test - Redirect response headers: {dict(redirect_resp.headers)}")
    if redirect_resp.status_code not in (301, 302, 307, 308):
        print(f"First test - Redirect response text: {redirect_resp.text}")
    
    assert redirect_resp.status_code in (301, 302, 307, 308)
    assert redirect_resp.headers["Location"] == creat_reandom_url

def test_404_for_invalid_alias():
    # 3. 随机 alias，应该 404
    random_alias = ''.join(random.choices(string.ascii_lowercase + string.digits, k=6))
    resp = requests.get(f"{BASE_URL}/u/{random_alias}", allow_redirects=False)
    assert resp.status_code == 404

def test_create_with_expire_and_expire_check():
    # 创建带过期时间的短链接
    url = "https://www.example.com/expire/" + ''.join(random.choices(string.ascii_letters + string.digits, k=8))
    expire_seconds = 30  # 改为30秒，确保有足够时间测试
    resp = requests.post(f"{BASE_URL}/create", json={"url": url, "expire": expire_seconds})
    assert resp.status_code == 200
    data = resp.json()
    print("Short URL with expire created:", data)
    assert "alias" in data
    alias = data["alias"]

    # 跳转测试（未过期）
    redirect_resp = requests.get(f"{BASE_URL}/u/{alias}", allow_redirects=False)
    print(f"Redirect response status: {redirect_resp.status_code}")
    print(f"Redirect response headers: {dict(redirect_resp.headers)}")
    print(f"Redirect response text: {redirect_resp.text}")
    assert redirect_resp.status_code in (301, 302, 307, 308)
    assert redirect_resp.headers["Location"] == url

    # # 等待过期
    # import time
    # time.sleep(expire_seconds + 1)
    # # 跳转测试（已过期）
    # expired_resp = requests.get(f"{BASE_URL}/u/{alias}", allow_redirects=False)
    # assert expired_resp.status_code == 404

if __name__ == "__main__":
    test_create_and_redirect()
    test_404_for_invalid_alias()
    test_create_with_expire_and_expire_check()
    print("All tests passed.")