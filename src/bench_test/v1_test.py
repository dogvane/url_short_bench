from locust import HttpUser, task, between, FastHttpUser
import random
import string
from collections import deque
import threading

# locust -f v1_test.py --host=http://localhost:10086
# locust -f v1_test.py --host=http://192.168.1.3:10086 -u 100 -r 100 --headless --csv=report --run-time 1m

# 分布式的测试命令：
# 主节点 (Master): 负责分发任务和汇总数据，需要指定测试目标 --host
# locust -f v1_test.py --master --headless --run-time 5m -u 5000 -r 500 --host=http://192.168.1.3:10086
# 工作节点 (Worker): 负责执行任务，只需要知道主节点的地址
# locust -f v1_test.py --worker --master-host=192.168.1.3

CREATE_WEIGHT = 1
READ_WEIGHT = 100

class ShortUrlUser(FastHttpUser):
    wait_time = between(0, 0)

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.created_aliases = []
        self.aliases_lock = threading.Lock()

    def on_start(self):
        # 每个用户预先创建10个短链
        self.create_short_url()


    @task(CREATE_WEIGHT)
    def create_short_url(self):
        url = "https://www.example.com" + ''.join(random.choices(string.ascii_letters + string.digits, k=8))
        data = {"url": url}
        
        # expire = random.choice([None, 60, 3600, 86400])
        # if expire:
        #     data["expire"] = expire
            
        resp = self.client.post("/create", json=data)
        if resp.status_code == 200:
            try:
                alias = resp.json().get("alias")
                if alias:
                    with self.aliases_lock:
                        self.created_aliases.append(alias)

                        
            except Exception:
                pass

    @task(READ_WEIGHT)
    def visit_short_url(self):
        with self.aliases_lock:
            alias = random.choice(self.created_aliases)
            resp = self.client.get(f"/u/{alias}", allow_redirects=False, name="/u/[alias]")
            # 302为成功，404为失败
            if resp.status_code == 302:
                pass  # 成功
            elif resp.status_code == 404:
                self.environment.events.request_failure.fire(
                    request_type="GET",
                    name="/u/[alias]",
                    response_time=resp.elapsed.total_seconds() * 1000,
                    response=resp,
                    exception=Exception(f"404 Not Found for alias: {alias}")
                )
            else:
                print(f"Unexpected status code: {resp.status_code} for alias: {alias}")