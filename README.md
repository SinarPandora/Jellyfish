# Jellyfish Kook Bot

> Kook 基础功能扩展机器人，同时包含大量为「斯普拉遁-加鱿站」定制的功能

## 功能列表与开发计划

### 基本功能

- [x] 指令权限管理
- [x] 通俗易懂的全局帮助
- [ ] 多阶段指令（可回退）

### Kook 功能补强

- [x] 临时文本频道
- [x] 游戏组队功能（点击自动创建临时语音/临时文本频道）
- [x] 频道组（根据条件创建和管理一组频道）
- [ ] 每日打卡
- [ ] 活跃度系统
- [ ] 自定义投票
- [ ] 信息收集表
- [ ] 分页卡片
- [ ] 倒计时频道名
- [ ] 根据最后消息时间自动排序频道

### 斯普拉遁扩展功能

- [x] 与其他相关 Bot 联动
- [ ] 私房规则选择器

## 开发指南

### 启动步骤

1. 安装 dotnet 8 环境
2. 执行 `Infrastructure` 文件夹下的 Postgres.compose.yml 文件创建数据库
3. 重命名 `Jellyfish/appsettings.json` 到 `Jellyfish/appsettings.Development.json`，并更新配置
4. 在 `Jellyfish` 目录下执行 `dotnet tool install --global dotnet-ef && dotnet ef database update`
5. 主函数运行 `Jellyfish/src/Jellyfish.cs`

### 文件夹结构

```tree
├── Infrastructure // 基础设施脚本
│         └── Postgres.compose.yml // 数据库部署脚本
├── Jellyfish // 项目应用源码
│         ├── Migrations // 数据库迁移脚本
│         ├── Properties // IDE 配置脚本
│         │         └── launchSettings.json // IDE 启动配置
│         ├── Resources // 应用资源文件
│         │         └── NLog.config // 应用 Log 格式配置文件
│         ├── src // 应用源码根目录
│         │         ├── Client // 外接 API 客户端
│         │         ├── Core   // 核心组件
│         │         ├── Custom // 自定义模块（用于对特定的游戏/话题进行扩展，不通用）
│         │         ├── Module // 功能模块（通用于所有的 Kook 服务器）
│         │         ├── Util   // 工具类
│         │         └── Jellyfish.cs // 程序入口
│         ├── Dockerfile                   // 应用打包部署脚本
│         ├── Jellyfish.csproj             // 项目文件
│         ├── appsettings.Development.json // 开发配置文件
│         ├── appsettings.Production.json  // 生产配置文件
│         └── appsettings.json             // 应用配置文件模板
├── CHANGELOG.md  // 更新记录（手动更新）
├── Jellyfish.sln // 解决方案文件
├── LICENSE       // 证书文件
├── README.md     // 项目说明文件
└── justfile      // 常用指令合集
```

### 应用配置文件说明

```json5
{
  // 日志开关配置
  "Logging": {
    // 日志等级配置
    "LogLevel": {
      // 默认输出 Info 级别日志
      "Default": "Information",
      // 关闭 .Net Core 服务器链接日志，只保留 Warning 级别日志
      "Microsoft.AspNetCore": "Warning"
    }
  },
  // .Net Core 服务器开放访问地址范围
  "AllowedHosts": "*",
  // 数据库连接字符串
  "DatabaseConnection": "",
  // Kook 相关配置
  "Kook": {
    // Bot websocket token
    "Token": "",
    // 是否启用调试日志（为避免日志溢出不要在生产环境打开）
    "EnableDebug": false,
    // 连接 Kook 服务器超时
    "ConnectTimeout": 6000
  }
}

```

### API Hosts

国内部分地区 DNS 被污染，若启动后始终无法受到消息，请添加以下地址解析内容：

```hosts
203.107.54.174  kaiheila.cn
101.201.199.174 ws.kaiheila.cn
39.102.47.15    www.kookapp.cn
203.107.54.174  kookapp.cn
```

### 依赖库及文档

#### Nuget 依赖

* [Kook.Net](https://kooknet.dev/index.html)
* [NLog](https://nlog-project.org/)
* [EntityFrameworkCore](https://docs.microsoft.com/zh-cn/ef/core/)
* [Newtonsoft.Json](https://www.newtonsoft.com/json)
* [Z.ExtensionMethods](https://csharp-extension.com/)
* [FluentScheduler](https://fluentscheduler.github.io/creating-schedules/)
* [Polly](https://www.thepollyproject.org/)
* [Autofac](https://autofac.org/)

#### Kook 开发者文档

* [KookAPI](https://developer.kookapp.cn/doc/reference)
* [KMarkdown Preview](https://www.kookapp.cn/tools/message-builder.html#/kmarkdown)

## 部署指南

### 部署步骤

1. 执行 `Infrastructure` 文件夹下的 Postgres.compose.yml 文件创建数据库
2. 重命名 `Jellyfish/appsettings.json` 到 `Jellyfish/appsettings.Production.json`，并更新配置
3. 在根目录下执行 `just migrate && just deploy` 进行部署

