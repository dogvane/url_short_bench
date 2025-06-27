from locust import task, between, FastHttpUser
import random
import string

# 仅测试创建性能的测试用例
# locust -f create_only_test.py --host=http://localhost:10086
# locust -f create_only_test.py --host=http://192.168.1.3:10086 -u 100 -r 100 --headless --csv=create_report --run-time 1m

# 分布式的测试命令：
# 主节点 (Master): 负责分发任务和汇总数据，需要指定测试目标 --host
# locust -f create_only_test.py --master --headless --run-time 5m -u 1000 -r 500 --host=http://192.168.1.3:10086
# 工作节点 (Worker): 负责执行任务，只需要知道主节点的地址
# locust -f create_only_test.py --worker --master-host=192.168.1.3

class CreateOnlyUser(FastHttpUser):
    wait_time = between(0, 0)

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.created_count = 0

    @task
    def create_short_url(self):
        """专门测试创建短链的性能"""
        # 生成随机URL
        url = "https://www.example.com/" + ''.join(random.choices(string.ascii_letters + string.digits, k=12))
        data = {"url": url}
        
        # 随机添加过期时间（10%的概率）
        if random.random() < 0.1:
            expire = random.choice([60, 300, 3600, 7200, 86400])  # 1分钟到1天
            data["expire"] = expire
            
        resp = self.client.post("/create", json=data)
        
        if resp.status_code == 200:
            self.created_count += 1
            try:
                result = resp.json()
                alias = result.get("alias")
                if not alias:
                    self.environment.events.request_failure.fire(
                        request_type="POST",
                        name="/create",
                        response_time=resp.elapsed.total_seconds() * 1000,
                        response=resp,
                        exception=Exception("No alias in response")
                    )
            except Exception as e:
                self.environment.events.request_failure.fire(
                    request_type="POST",
                    name="/create",
                    response_time=resp.elapsed.total_seconds() * 1000,
                    response=resp,
                    exception=e
                )
        else:
            # 记录失败请求
            self.environment.events.request_failure.fire(
                request_type="POST",
                name="/create",
                response_time=resp.elapsed.total_seconds() * 1000,
                response=resp,
                exception=Exception(f"HTTP {resp.status_code}: {resp.text}")
            )

    def on_stop(self):
        """测试结束时输出统计信息"""
        print(f"User created {self.created_count} short URLs")


class CreateBatchUser(FastHttpUser):
    """批量创建测试用户 - 用于高并发场景"""
    wait_time = between(0, 0.01)  # 极短等待时间，模拟高并发

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.created_count = 0
        self.failed_count = 0

    @task
    def create_short_url_batch(self):
        """高频创建短链"""
        # 生成更复杂的URL路径
        path_segments = [
            ''.join(random.choices(string.ascii_lowercase, k=random.randint(3, 8)))
            for _ in range(random.randint(1, 3))
        ]
        url = "https://www.test-domain.com/" + "/".join(path_segments)
        
        # 添加查询参数（20%的概率）
        if random.random() < 0.2:
            params = []
            for _ in range(random.randint(1, 3)):
                key = ''.join(random.choices(string.ascii_lowercase, k=random.randint(2, 5)))
                value = ''.join(random.choices(string.ascii_letters + string.digits, k=random.randint(3, 10)))
                params.append(f"{key}={value}")
            url += "?" + "&".join(params)
        
        data = {"url": url}
        
        with self.client.post("/create", json=data, catch_response=True) as resp:
            if resp.status_code == 200:
                self.created_count += 1
                resp.success()
            else:
                self.failed_count += 1
                resp.failure(f"HTTP {resp.status_code}")

    def on_stop(self):
        """测试结束时输出统计信息"""
        total = self.created_count + self.failed_count
        success_rate = (self.created_count / total * 100) if total > 0 else 0
        print(f"Batch User - Created: {self.created_count}, Failed: {self.failed_count}, Success Rate: {success_rate:.2f}%")


# 如果需要混合测试，可以注释掉上面的类，使用下面的类
class MixedCreateUser(FastHttpUser):
    """混合创建测试 - 不同类型的URL和参数"""
    wait_time = between(0, 0.001)

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.url_templates = [
            "https://www.github.com/user/{}/repo/{}",
            "https://stackoverflow.com/questions/{}/{}",
            "https://medium.com/@{}/{}",
            "https://www.youtube.com/watch?v={}",
            "https://docs.google.com/document/d/{}/edit",
            "https://www.amazon.com/dp/{}",
            "https://twitter.com/{}/status/{}",
        ]

    @task(70)
    def create_normal_url(self):
        """创建普通URL"""
        template = random.choice(self.url_templates)
        if template.count('{}') == 2:
            url = template.format(
                ''.join(random.choices(string.ascii_letters, k=8)),
                ''.join(random.choices(string.ascii_letters + string.digits, k=12))
            )
        else:
            url = template.format(''.join(random.choices(string.ascii_letters + string.digits, k=16)))
        
        data = {"url": url}
        self.client.post("/create", json=data)

    @task(20)
    def create_url_with_expire(self):
        """创建带过期时间的URL"""
        url = "https://temp.example.com/" + ''.join(random.choices(string.ascii_letters + string.digits, k=10))
        expire_times = [60, 300, 600, 1800, 3600, 7200, 86400]  # 1分钟到1天
        
        data = {
            "url": url,
            "expire": random.choice(expire_times)
        }
        self.client.post("/create", json=data)

    @task(10)
    def create_complex_url(self):
        """创建复杂URL（带参数、锚点等）"""
        base_url = "https://www.example.com/api/v1/data"
        
        # 添加查询参数
        params = []
        for _ in range(random.randint(1, 5)):
            key = random.choice(['id', 'type', 'filter', 'sort', 'page', 'limit', 'search'])
            value = ''.join(random.choices(string.ascii_letters + string.digits, k=random.randint(1, 10)))
            params.append(f"{key}={value}")
        
        url = base_url + "?" + "&".join(params)
        
        # 有时添加锚点
        if random.random() < 0.3:
            anchor = ''.join(random.choices(string.ascii_lowercase, k=random.randint(3, 8)))
            url += f"#{anchor}"
        
        data = {"url": url}
        self.client.post("/create", json=data)
