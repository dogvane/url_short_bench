from locust import task, between, FastHttpUser
import random
import string

# 仅测试创建性能的测试用例
# locust -f create_only_test.py --host=http://localhost:10086
# locust -f create_only_test.py --host=http://192.168.1.3:10086 -u 1 -r 100 --headless --csv=create_report --run-time 1m

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
        print(f"[Locust] 当前 host: {self.host}")
        """专门测试创建短链的性能"""
        url = "https://www.example.com/" + ''.join(random.choices(string.ascii_letters + string.digits, k=12))
        data = {"url": url}
        # 随机添加过期时间（10%概率）
        if random.random() < 0.1:
            expire = random.choice([60, 300, 3600, 7200, 86400])
            data["expire"] = expire
        resp = self.client.post("/create", json=data)
        if resp.status_code != 200:
            if resp.status_code == 0:
                raise Exception("HTTP 0: 连接失败或目标服务无响应，可能服务未启动或网络异常")
            else:
                raise Exception(f"HTTP {resp.status_code}: {resp.text}")
        result = resp.json()
        assert "alias" in result, "响应中缺少 alias 字段"
        alias = result["alias"]
        assert alias, "alias 为空"
