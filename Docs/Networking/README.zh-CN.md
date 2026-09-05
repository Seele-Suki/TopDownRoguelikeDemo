# Golden Sand Caravan 联机网络文档

这是 `Golden Sand Caravan（金砂旅团）` 联机系统的中文总览，面向项目学习、构建、测试和面试复盘。它汇总 `Docs/Networking` 下的五份英文技术文档；协议和命令以工程当前实现为准。

## 文档导航

| 中文入口 | 对应原文 | 用途 |
| --- | --- | --- |
| 本文 | [Architecture.md](Architecture.md)、[Protocol.md](Protocol.md)、[BuildAndRun.md](BuildAndRun.md)、[IPv6TestGuide.md](IPv6TestGuide.md)、[PlayerSyncProtocol.md](PlayerSyncProtocol.md) | 快速理解和查找 |
| [Architecture.md](Architecture.md) | 网络架构 | 模块职责、权威归属和数据流 |
| [Protocol.md](Protocol.md) | 网络协议 | TCP/UDP 头、消息类型和通用规则 |
| [BuildAndRun.md](BuildAndRun.md) | 构建运行 | CMake、CTest、服务器和 Unity 构建 |
| [IPv6TestGuide.md](IPv6TestGuide.md) | IPv6 测试 | `::1`、公网 IPv6、防火墙和排错 |
| [PlayerSyncProtocol.md](PlayerSyncProtocol.md) | 玩家同步协议 | 玩家输入、状态、血量和世界快照 |

## 1. 系统架构

项目由三个主要部分组成：

- **Unity 客户端**：菜单、玩法表现、本地输入、本地 UI；房主客户端还负责权威 Gameplay 模拟。
- **C++ NetworkServer**：使用 C++17 和 Winsock2，管理 TCP/UDP 连接、房间、玩家 ID、会话令牌、消息转发、心跳和断线检测；服务器不模拟战斗。
- **NetworkClient**：Unity C# 网络门面。后台线程只处理字节并投递消息，Unity 对象由主线程处理。

`NetworkGameBootstrap` 根据会话模式连接玩家注册器、世界快照发布器/消费者和战斗协调器。

### 权威归属

| 数据 | 权威方 | 主要通道 |
| --- | --- | --- |
| 房间成员、玩家映射、会话 | C++ Server | TCP |
| 玩家输入转发 | 客户端发送，服务器转发 | UDP |
| 玩家移动、瞄准、射击和技能 | Host Unity | UDP 状态/事件 |
| 敌人、Boss、经验球和战斗结果 | Host Unity | UDP 快照、TCP 关键事件 |
| 玩家当前血量、最大血量和死亡 | Host Unity | UDP 状态，TCP 死亡确认 |
| 最终胜利/失败 | Host Unity | TCP |

Client 只应用 Host 的权威状态，不能自行决定伤害、死亡、敌人生成或最终结果。

### 三种运行模式

- **单人**：只创建本地玩家，不启动 Socket、服务器、心跳或网络协调器。
- **Host**：模拟双方玩家和整个战斗世界，并发布权威快照。
- **Client**：发送自己的输入，接收并应用 Host 的玩家、世界和战斗状态。

## 2. TCP 与 UDP

### TCP

TCP 是可靠字节流，负责连接、昵称、创建/加入房间、准备、开始、退出、心跳、错误和关键战斗事件。每个数据包使用 12 字节固定消息头：

```text
偏移 0  : uint32 Magic       = 0x54445231
偏移 4  : uint16 Version     = 1
偏移 6  : uint16 MessageType
偏移 8  : uint32 PayloadSize
```

接收端必须处理半包和粘包，只有在完整消息到达后才交给上层。

### UDP

UDP 用于高频输入和状态：玩家输入、玩家快照、世界快照、射击表现和霰弹枪表现。UDP 头为 32 字节：

```text
偏移 0  : uint32 Magic       = 0x54445255
偏移 4  : uint16 Version     = 1
偏移 6  : uint16 MessageType
偏移 8  : byte[16] SessionToken
偏移 24 : uint32 PlayerId
偏移 28 : uint32 Sequence
```

整数统一使用网络字节序（大端）；浮点数按 IEEE-754 binary32 的位模式传输，不能直接发送 C++ 结构体内存。

## 3. 玩家同步与血量

`PlayerInput` 负载固定 28 字节，包含移动方向、瞄准方向、输入标志、冲刺请求序号和霰弹枪请求序号。Client 发送输入，不发送权威位置。

`PlayerStateSnapshot` 以 4 字节玩家数量开头，每个玩家记录 28 字节：

```text
uint32 PlayerId
float  PositionX, PositionY, AimX, AimY
uint32 Flags       // bit 0: FireHeld, bit 1: IsDashing
uint16 CurrentHealth
uint16 MaxHealth
```

血量规则：

- `MaxHealth >= 1`
- `0 <= CurrentHealth <= MaxHealth`
- `CurrentHealth == 0` 表示权威死亡
- Client 只能在 Unity 主线程把 Host 的结果应用到 `PlayerHealth`
- `HealthBarView` 继续监听 `PlayerHealth`，不直接解析网络字节
- 左下角血条只显示当前客户端自己的玩家，不显示远程玩家头顶血条

`WorldStateSnapshot` 用稳定实体 ID同步 Player、Enemy、Boss、ExperienceOrb 和 BossProjectile。实体 ID 在同一场战斗中不复用；对象池重新激活经验球时必须分配新 ID。

## 4. 消息类型速查

| 编号 | 消息 | 通道 | 作用 |
| ---: | --- | --- | --- |
| 10-20 | 房间请求、响应、开始、退出、错误 | TCP | 房间生命周期 |
| 21-22 | TCP 心跳请求/响应 | TCP | 活性检测 |
| 30-33 | UDP 绑定、Ping/Pong | UDP | UDP 身份绑定与连通性 |
| 34 | PlayerInput | UDP | 客户端输入 |
| 35 | PlayerStateSnapshot | UDP | 玩家权威状态 |
| 36-37 | PlayerShotEvent / PlayerShotgunEvent | UDP | 射击表现 |
| 40 | WorldStateSnapshot | UDP | 世界高频状态 |
| 41-45 | 实体生成、移除、玩家死亡、经验球事件 | TCP | 可靠生命周期事件 |
| 46-48 | 升级开始、选择、完成 | TCP | 同步升级暂停 |
| 49-50 | Boss 阶段、GameResult | TCP | 关键流程 |
| 51-52 | SharedExperienceSnapshot / BossCombatState | UDP | 共享经验和 Boss 状态 |

UDP 序号按发送者分开追踪，重复、旧包和歧义半区序号丢弃；后续快照能够修正一次丢失的血量更新。心跳间隔为 2 秒，超时为 6 秒。

## 5. 断线、暂停和资源释放

断线处理的规则如下：

- Host 断线：Client 结束房间并返回联机菜单。
- Client 断线：Host 暂停并选择继续单人或退出。
- 等待选择期间，Host 战斗暂停；继续后保留血量、经验、Boss 和战斗进度。
- 不支持断线重连和主机迁移。
- `NetworkShutdownCoordinator` 统一停止心跳、输入、快照发布、事件订阅、Socket、线程和远程对象。
- Unity 只关闭自己自动启动的服务器；手动启动的服务器由用户负责关闭。

关闭流程必须幂等，不能重复弹窗、残留线程、Socket、玩家/敌人/Boss/经验球对象或 Unity 所有的服务器进程。

## 6. 构建和运行速查

在 Visual Studio x64 Developer PowerShell 中，从仓库根目录执行：

```powershell
cmake -S NetworkServer -B NetworkServer/build -G "Visual Studio 17 2022" -A x64 -DBUILD_TESTING=ON
cmake --build NetworkServer/build --config Debug
ctest --test-dir NetworkServer/build -C Debug --output-on-failure
```

服务器默认端口为 `7777`：

```powershell
.\NetworkServer\build\Debug\NetworkServer.exe 7777
```

Unity 中通过 `文件 > 生成设置` 选择 Windows、`x86_64`，先构建 Development Build 验证，再构建最终非 Development Build。建议输出目录：

```text
F:\Unity\Builds\GoldenSandCaravan\
```

将 `NetworkServer.exe` 放在开发构建的 `NetworkServer/build/Debug/`，或放在最终 Unity 可执行文件旁边。服务器控制台查看 Server 日志；编辑器查看 Unity Console，打包版查看 Player.log。

## 7. IPv6 测试速查

- 同机测试地址：`::1`
- Host 优先显示本机全局 IPv6
- 不使用 `fe80::/10` 链路本地、`fc00::/7` ULA、组播或 IPv4 地址作为公网地址
- 没有全局 IPv6 时回退到 `::1`
- Client 填写 Host 的 IPv6，不填写默认网关
- 组合显示格式：`[IPv6]:7777`

主机检查端口：

```powershell
Get-NetTCPConnection -LocalPort 7777 -ErrorAction SilentlyContinue
Get-NetUDPEndpoint -LocalPort 7777 -ErrorAction SilentlyContinue
Test-NetConnection -ComputerName ::1 -Port 7777
```

公网测试还需要在主机防火墙放行 TCP 7777 和 UDP 7777。连接失败时依次检查服务器是否运行、IPv6 是否为 Host 地址、端口是否被占用、防火墙是否放行，以及 Client 是否填写了正确端口。

## 8. 测试清单

- C++ CTest：协议编解码、TCP 粘包/半包、UDP 绑定/序号、玩家血量边界、世界实体、心跳和断线。
- Unity EditMode：客户端状态、协议、玩家同步、血条、经验、升级、Boss、断线暂停和关闭幂等。
- 人工回归：单人完整流程、双人房间、移动战斗、血量/血条、经验升级、Boss、胜负、Host/Client 断线。
- 分发验证：编辑器、一个打包客户端、两个打包客户端，以及没有 Unity 编辑器的 Windows 目录。

最终检查时确认：没有外部 NUnit NuGet 包、没有构建目录或日志进入 Git、没有私人 IPv6 地址进入文档或资源，且 Unity Console 没有红色错误。
