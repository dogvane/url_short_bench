# 短链接服务 v2

## 项目概述

这是一个高性能的短链接服务，提供URL缩短和重定向功能。项目支持多种实现策略，可以根据部署环境和性能需求灵活配置。

## 目录结构

```
v2/
├── Controllers/           # 控制器层
│   └── ShortUrlController.cs
├── Repositories/          # 数据访问层
│   ├── IShortUrlRepository.cs
│   ├── DbRepository.cs
│   └── DbRepositoryAutoIncrement.cs
├── Configuration/         # 配置和工厂类
│   ├── ShortUrlOptions.cs
│   └── ShortUrlRepositoryFactory.cs
├── Tests/                # 测试文件
│   └── ProgramComparison.cs
├── Data/                 # 数据文件
│   └── shortlinks.db
├── Properties/           # 项目属性
├── bin/                  # 编译输出
├── obj/                  # 编译中间文件
├── appsettings.json      # 生产环境配置
├── appsettings.Development.json  # 开发环境配置
├── Program.cs            # 应用程序入口
└── v2.csproj            # 项目文件
```

## API 接口

### 1. 创建短链接

- **端点**: `POST /create`
- **参数**:
  - `url`: 原始URL（必填）
  - `expire`: 过期时间，单位秒（可选，不设置则永久有效）

**请求示例**:

```bash
POST http://localhost:10086/create
Content-Type: application/json

{
  "url": "http://www.baidu.com",
  "expire": 3600
}
```

**响应示例**:

```json
{
  "alias": "1a2b3c",
  "url": "http://www.baidu.com",
  "id": 123456
}
```

### 2. 短链接重定向

- **端点**: `GET /u/{alias}`
- **参数**: `alias` - 短链接别名

**请求示例**:

```bash
GET http://localhost:10086/u/1a2b3c
```

**响应**: 302重定向到原始URL，或404未找到

## 数据库设计

### 表结构

```sql
CREATE TABLE IF NOT EXISTS short_links (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    alias TEXT UNIQUE,
    url TEXT NOT NULL,
    expire INTEGER DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### 字段说明

- `id`: 主键，自增ID
- `alias`: 短链接别名，使用Base62编码生成
- `url`: 原始URL
- `expire`: 过期时间戳（0表示永不过期）
- `created_at`: 创建时间

## 短码生成算法

项目使用Base62编码算法将数据库ID转换为短码：

- **字符集**: `0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ`
- **映射方式**: 数据库ID → Base62编码 → 6位ASCII字符串
- **特点**: 可逆、唯一、URL安全

## 实现策略

### 1. Original（原始实现）

- **流程**: INSERT临时数据 → 获取ID → UPDATE真实短码
- **优点**: 适合多实例部署，数据一致性好
- **缺点**: 需要2次数据库操作，性能相对较低
- **适用场景**: 生产环境、多实例部署

### 2. AutoIncrement（自增ID实现）

- **流程**: 内存自增ID → 生成短码 → 直接INSERT完整数据
- **优点**: 只需1次数据库操作，性能更优
- **缺点**: 需要同步最大ID，不适合多实例
- **适用场景**: 单实例部署、高并发场景

## 配置说明

### 基本配置 (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/shortlinks.db;Cache=Shared;Mode=ReadWriteCreate;Pooling=True;"
  },
  "ShortUrl": {
    "RepositoryType": "Original",
    "EnablePerformanceLogging": false
  }
}
```

### 配置参数

- **RepositoryType**:
  - `"Original"` - 原始实现（推荐用于生产环境）
  - `"AutoIncrement"` - 自增ID实现（推荐用于开发/单实例）
- **EnablePerformanceLogging**: 是否启用性能监控日志

### 环境配置

- **生产环境**: 使用 `Original` 类型，关闭性能日志
- **开发环境**: 使用 `AutoIncrement` 类型，开启性能日志

## 依赖注入架构

项目使用接口抽象和工厂模式实现灵活的依赖注入：

```csharp
// 接口定义
public interface IShortUrlRepository
{
    Task<(long id, string url, long expire)> GetUrlByAliasAsync(string alias);
    (long id, string alias) CreateShortLink(string url, int? expireSeconds);
    long GetCurrentMaxId();
}

// 工厂创建
var factory = new ShortUrlRepositoryFactory(options);
var repository = factory.CreateRepository();
```

## 性能对比

| 特性 | Original | AutoIncrement |
|------|----------|---------------|
| 数据库操作次数 | 2次 (INSERT + UPDATE) | 1次 (INSERT) |
| 并发性能 | 中等 | 优秀 |
| 多实例支持 | 优秀 | 需要额外协调 |
| 内存占用 | 低 | 中等 |
| 启动速度 | 快 | 需要查询最大ID |

## 快速开始

### 1. 构建和运行

```bash
cd src/v2
dotnet build
dotnet run
```

### 2. 测试接口

```bash
# 创建短链接
curl -X POST http://localhost:10086/create \
  -H "Content-Type: application/json" \
  -d '{"url": "https://github.com", "expire": 3600}'

# 访问短链接
curl -L http://localhost:10086/u/{返回的alias}
```

### 3. 性能测试

运行比较测试程序：

```bash
dotnet run --project Tests/ProgramComparison.cs
```

## 扩展和维护

### 添加新的实现策略

1. 实现 `IShortUrlRepository` 接口
2. 在 `ShortUrlRepositoryType` 枚举中添加新类型
3. 在 `ShortUrlRepositoryFactory` 中添加创建逻辑

### 监控和调试

- 应用启动时会输出当前配置信息
- 支持实时查看最大ID状态
- 可通过配置启用性能监控日志

## 技术栈

- **.NET 8.0** - 运行时框架
- **ASP.NET Core** - Web框架
- **Dapper** - 数据访问ORM
- **SQLite** - 默认数据库
- **Base62** - 短码编码算法

## 许可证

本项目遵循 MIT 许可证。
