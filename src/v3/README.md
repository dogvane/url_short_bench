# URL 短链服务 - MySQL 版本

## 启动 MySQL 数据库

### 在 WSL 中启动

1. 进入 docker 目录：
```bash
cd docker
```

2. 启动 MySQL 容器：
```bash
# Linux/WSL
chmod +x start_mysql.sh
./start_mysql.sh

# 或者直接使用 docker-compose
docker-compose up -d
```

### 在 Windows 中启动

1. 进入 docker 目录，双击运行 `start_mysql.bat`

或者在命令行中：
```cmd
cd docker
start_mysql.bat
```

## 数据库连接信息

- **Host**: localhost
- **Port**: 3306
- **Database**: urlshort
- **Username**: urlshort
- **Password**: urlshort123
- **Root Password**: root123456

## 连接字符串

```
Server=localhost;Port=3306;Database=urlshort;Uid=urlshort;Pwd=urlshort123;CharSet=utf8mb4;Pooling=true;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30;
```

## 运行应用

1. 确保 MySQL 容器已启动
2. 进入 v2_mysql 目录
3. 运行应用：

```bash
cd src/v2_mysql
dotnet run
```

## 停止 MySQL 容器

```bash
cd docker
docker-compose down
```

## 查看日志

```bash
cd docker
docker-compose logs -f mysql
```

## 数据持久化

MySQL 数据文件存储在 `./docker/mysql` 目录下，容器重启后数据不会丢失。

## 性能优化配置

Docker 容器已配置以下 MySQL 性能优化：
- innodb-buffer-pool-size=512M
- innodb-log-file-size=128M
- max-connections=1000
- query-cache-type=1
- query-cache-size=64M
- UTF8MB4 字符集支持
