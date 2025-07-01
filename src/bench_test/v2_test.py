from locust import HttpUser, task, between, FastHttpUser
import random
import string
from collections import deque
import threading
import os

# locust -f v1_test.py --host=http://localhost:10086
# locust -f v1_test.py --host=http://192.168.1.3:10086 -u 100 -r 100 --headless --csv=report --run-time 1m

# 分布式的测试命令：
# 主节点 (Master): 负责分发任务和汇总数据，需要指定测试目标 --host
# locust -f v1_test.py --master --headless --run-time 5m -u 1000 -r 500 --host=http://192.168.1.3:10086
# locust -f v2_test.py --master --host=http://192.168.1.3:10086 --master-bind-host=0.0.0.0
# 工作节点 (Worker): 负责执行任务，只需要知道主节点的地址
# locust -f v1_test.py --worker --master-host=192.168.1.3

CREATE_WEIGHT = 1
READ_WEIGHT_BY_FILE = 10
READ_WEIGHT = 189

ALIAS_FILE = "created_aliases.txt"
ALIAS_FILE_LOCK = threading.Lock()

# 测试用例说明
# 创建一个短链，可能对应从数据库中读取10个短链的操作，这个可能命中数据库，也可能命中redis缓存


class ShortUrlUser(FastHttpUser):
    wait_time = between(0, 0)

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.created_aliases = []
        self.aliases_lock = threading.Lock()
        self.file_aliases = []

    def on_start(self):
        # 每个用户预先创建10个短链
        self.load_aliases_from_file()
        self.create_short_url()

    def load_aliases_from_file(self):
        """读取 alias 文件，写入 file_aliases 列表"""
        if os.path.exists(ALIAS_FILE):
            with ALIAS_FILE_LOCK:
                with open(ALIAS_FILE, "r", encoding="utf-8") as f:
                    self.file_aliases = [line.strip() for line in f if line.strip()]
                    if len(self.file_aliases) > 0:
                        self.created_aliases.append(self.file_aliases[0])

    def save_alias_to_file(self, alias):
        """将 alias 写入文件，带锁防止并发冲突"""
        with ALIAS_FILE_LOCK:
            with open(ALIAS_FILE, "a", encoding="utf-8") as f:
                f.write(alias + "\n")

    @task(CREATE_WEIGHT)
    def create_short_url(self):
        url = "https://www.example.com" + ''.join(random.choices(string.ascii_letters + string.digits, k=8))
        data = {"url": url}
        resp = self.client.post("/create", json=data)
        if resp.status_code == 200:
            try:
                alias = resp.json().get("alias")
                if alias:
                    with self.aliases_lock:
                        self.created_aliases.append(alias)
                    self.save_alias_to_file(alias)
            except Exception:
                pass

    @task(READ_WEIGHT)
    def visit_short_url(self):
        with self.aliases_lock:
            if not self.created_aliases:
                return
            alias = random.choice(self.created_aliases)
        resp = self.client.get(f"/u/{alias}", allow_redirects=False, name="/u/[alias]")
        if resp.status_code == 302:
            pass  # 成功
        elif resp.status_code == 404:
            pass  # 失败
        else:
            print(f"Unexpected status code: {resp.status_code} for alias: {alias}")

    @task(READ_WEIGHT_BY_FILE)
    def visit_short_url_from_file(self):
        """从文件读取的 alias 列表中访问短链"""
        if not self.file_aliases:
            return
        alias = random.choice(self.file_aliases)
        resp = self.client.get(f"/u/{alias}", allow_redirects=False, name="/u/[alias]_file")
        if resp.status_code == 302:
            pass
        elif resp.status_code == 404:
            pass
        else:
            print(f"Unexpected status code: {resp.status_code} for alias: {alias}")