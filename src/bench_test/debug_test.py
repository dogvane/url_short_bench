import requests
import random
import string

BASE_URL = 'http://localhost:10086'

# 1. 创建短链接
creat_reandom_url = 'https://www.example.com/' + ''.join(random.choices(string.ascii_letters + string.digits, k=8))
print(f'Trying to create URL: {creat_reandom_url}')
try:
    resp = requests.post(f'{BASE_URL}/create', json={'url': creat_reandom_url})
    print(f'Response status: {resp.status_code}')
    print(f'Response text: {resp.text}')
    print(f'Response headers: {dict(resp.headers)}')
except Exception as e:
    print(f'Error: {e}')
