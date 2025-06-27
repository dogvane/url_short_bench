@echo off
setlocal

REM ��ȡ��ǰʱ�䣬��ʽΪyyyyMMdd_HHmmss
for /f "tokens=1-4 delims=/ " %%a in ('date /t') do set d=%%a%%b%%c
for /f "tokens=1-2 delims=: " %%a in ('time /t') do set t=%%a%%b
set timestr=%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set timestr=%timestr: =0%

set reportdir=report\%timestr%
mkdir %reportdir%


start locust -f v1_test.py --master --headless --run-time 2m -u 400 -r 100 --host=http://192.168.1.3:10086 --csv=%reportdir%\report

REM �ȴ� master �������
ping 127.0.0.1 -n 4 >nul

REM �ȴ� 1 ��ȷ�� master ��ȫ����
ping 127.0.0.1 -n 4 >nul

start locust -f v1_test.py --worker --master-host=192.168.1.3
start locust -f v1_test.py --worker --master-host=192.168.1.3
start locust -f v1_test.py --worker --master-host=192.168.1.3
start locust -f v1_test.py --worker --master-host=192.168.1.3