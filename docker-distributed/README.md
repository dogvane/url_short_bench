# URL Short Service - 分布式部署

这是一个基于分布式架构的短链接服务，采用以下技术栈：
# wsl 下开启外部对wsl内访问的方式，先获得wsl的虚拟网卡ip，然后做转发

# (base) d@DESKTOP-LIC495U:/mnt/e/blogs/url_short_bench/docker-distributed$ hostname -I
# 172.20.223.99 172.17.0.1 172.18.0.1
# windows 管理员权限
# 转发端口
#  netsh interface portproxy add v4tov4 listenport=10086 listenaddress=0.0.0.0 connectport=10086 connectaddress=172.20.223.99 
# 允许防火墙访问
#  netsh advfirewall firewall add rule name="WSL 10086" dir=in action=allow protocol=TCP localport=10086

## 架构组件

- **Nginx**: 反向代理和负载均衡
- **ASP.NET Core**: 3个应用实例提供高可用性
- **Redis**: 缓存层，提升读取性能
- **MySQL**: 持久化存储

## 快速开始

### 前置要求

- Docker 和 Docker Compose
- 确保端口 80, 3306, 6379, 8081-8083 可用

### 启动服务

```bash
# Windows
start.bat

# Linux/Mac
chmod +x start.sh
./start.sh
```

### 停止服务

```bash
# Windows
stop.bat

# Linux/Mac
chmod +x stop.sh
./stop.sh
```

### 监控服务

```bash
# Windows
monitor.bat
```

## API 接口

### 创建短链接

```bash
POST http://localhost/create
Content-Type: application/json

{
    "url": "https://www.example.com",
    "expire": 3600  # 可选，过期时间（秒）
}
```

响应：
```json
{
    "alias": "abc123",
    "url": "https://www.example.com",
    "id": 1
}
```

### 访问短链接

```bash
GET http://localhost/u/{alias}
```

成功时会重定向到原始URL，失败时返回404。

## 服务配置

### 负载均衡

Nginx 配置了3个应用实例的负载均衡：
- urlshort-app-1:8080
- urlshort-app-2:8080  
- urlshort-app-3:8080

使用 `least_conn` 算法确保请求均匀分布。

### 缓存策略

Redis 缓存配置：
- 缓存键格式：`shortlink:{alias}`
- 默认过期时间：60分钟
- 如果短链接有过期时间，使用较短的时间

### 数据库优化

MySQL 配置优化：
- InnoDB 存储引擎
- 连接池：最大1000连接
- 索引优化：alias, expire, created_at
- 字符集：utf8mb4

## 性能特性

1. **读写分离**: Redis缓存减少数据库查询
2. **负载均衡**: 3个应用实例分担请求
3. **连接池**: 数据库和Redis连接复用
4. **缓存预热**: 新创建的短链接立即缓存

## 监控和日志

使用 `monitor.bat` 可以：
- 查看服务状态
- 实时查看各组件日志
- 监控资源使用情况
- 测试服务端点

## 扩展和维护

### 水平扩展

要增加应用实例，在 `docker-compose.yml` 中添加新的服务：

```yaml
urlshort-app-4:
  # 复制现有应用配置
```

并在 Nginx 配置中添加对应的 upstream 服务器。

### 数据库维护

```bash
# 连接到MySQL容器
docker-compose exec mysql mysql -u urlshort -p urlshort

# 查看短链接统计
SELECT COUNT(*) as total_links FROM short_links;
SELECT COUNT(*) as active_links FROM short_links WHERE expire = 0 OR expire > UNIX_TIMESTAMP() * 1000;
```

### Redis 监控

```bash
# 连接到Redis容器
docker-compose exec redis redis-cli

# 查看缓存统计
INFO stats
DBSIZE
```

## 故障排除

### 常见问题

1. **端口被占用**: 修改 `docker-compose.yml` 中的端口映射
2. **内存不足**: 调整 Redis 和 MySQL 的内存限制
3. **连接数过多**: 增加数据库连接池大小

### 日志位置

- Nginx: `/var/log/nginx/` (容器内)
- 应用: stdout (通过 `docker-compose logs` 查看)
- MySQL: `/var/log/mysql/` (容器内)
- Redis: stdout (通过 `docker-compose logs` 查看)

### 健康检查

```bash
# 检查所有服务状态
curl http://localhost/health

# 检查应用实例
curl http://localhost:8081/health
curl http://localhost:8082/health  
curl http://localhost:8083/health
```

## 安全建议

1. 为Redis设置密码（生产环境）
2. 使用HTTPS（配置SSL证书）
3. 限制数据库访问IP
4. 定期备份数据
5. 监控异常访问模式
