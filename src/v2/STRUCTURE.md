# 项目目录结构说明

## 整理后的目录结构

```
v2/
├── Controllers/           # 控制器层 - 处理HTTP请求
│   └── ShortUrlController.cs
├── Repositories/          # 数据访问层 - 数据库操作
│   ├── IShortUrlRepository.cs       # 仓储接口
│   ├── DbRepository.cs              # 原始实现
│   └── DbRepositoryAutoIncrement.cs # 自增ID实现
├── Configuration/         # 配置和工厂类 - 依赖注入配置
│   ├── ShortUrlOptions.cs           # 配置选项
│   └── ShortUrlRepositoryFactory.cs # 工厂类
├── Tests/                # 测试文件 - 性能测试和比较
│   └── ProgramComparison.cs
├── Data/                 # 数据文件 - 数据库文件
│   └── shortlinks.db
├── Properties/           # 项目属性文件
├── bin/                  # 编译输出目录
├── obj/                  # 编译中间文件目录
├── appsettings.json      # 生产环境配置文件
├── appsettings.Development.json  # 开发环境配置文件
├── Program.cs            # 应用程序入口点
├── v2.csproj            # 项目文件
└── README.md            # 项目说明文档（合并后的完整文档）
```

## 文件分类说明

### 1. 业务逻辑层

- **Controllers/**: 处理HTTP请求，定义API端点
- **Repositories/**: 数据访问抽象，包含接口和具体实现

### 2. 配置层

- **Configuration/**: 依赖注入配置，工厂模式实现

### 3. 测试层

- **Tests/**: 性能测试和功能验证

### 4. 数据层

- **Data/**: 数据库文件和数据相关资源

### 5. 配置文件

- **appsettings.json**: 生产环境配置
- **appsettings.Development.json**: 开发环境配置

## 整理的优势

### 1. 清晰的分层架构

- 每个目录职责单一，便于维护
- 符合 Clean Architecture 原则
- 易于扩展和重构

### 2. 文档整合

- 将3个README文件合并为1个完整文档
- 减少文档维护成本
- 提供全面的项目信息

### 3. 便于开发

- 文件分类清晰，查找方便
- 新开发者容易理解项目结构
- 支持团队协作开发

### 4. 部署友好

- 配置文件和代码分离
- 数据文件独立目录
- 便于CI/CD流程

## 注意事项

1. **数据库路径**: 更新了配置文件中的数据库连接字符串为 `Data Source=Data/shortlinks.db`
2. **命名空间**: 所有类文件的命名空间保持为 `v2`，无需修改
3. **编译正常**: 项目结构调整后编译通过，无需额外配置
