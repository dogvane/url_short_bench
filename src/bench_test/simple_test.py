import requests

try:
    resp = requests.get('http://localhost:10086/')
    print(f'GET / status: {resp.status_code}')
    print(f'GET / response: {resp.text}')
except Exception as e:
    print(f'Error connecting to server: {e}')

try:
    resp = requests.post('http://localhost:10086/create', json={'url': 'https://www.example.com/test'})
    print(f'POST /create status: {resp.status_code}')
    print(f'POST /create response: {resp.text}')
except Exception as e:
    print(f'Error connecting to server: {e}')
