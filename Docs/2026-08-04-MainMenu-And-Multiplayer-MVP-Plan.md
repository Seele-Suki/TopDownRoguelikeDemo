# TopDown Roguelike Demo 开始页与联机 MVP 实施计划

> 一次只实施一个 Phase。每个 Phase 完成后停止，等待用户在 Unity 中测试并确认。不得自动提交或推送 Git。

**目标：**保留现有单人玩法，新增独立开始页，并逐步完成基于 C++ Winsock2 Server、Unity C# Socket Client、房主权威模型的双人合作联机 MVP。

**架构：**Unity 客户端负责 UI 和游戏表现；房主 Unity 客户端运行权威 Gameplay；独立 C++ Server 管理 TCP/UDP 连接、房间、玩家映射和消息路由。单人和联机共用 `SampleScene`，通过会话上下文和网络适配层区分模式。

**技术栈：**Unity 2022.3.62f3c1、C#、UGUI、TextMeshPro、Unity Test Framework、C++17、Winsock2、CMake、TCP、UDP、IPv6 双栈、Windows 10/11 x64。

## 总体边界

- `Assets/Scripts/Infrastructure/App`：跨场景会话数据和场景入口。
- `Assets/Scripts/UI/Menu`：开始页、准备页、联机菜单和错误提示。
- `Assets/Scripts/Networking`：Unity 网络客户端、协议、房间状态和 Gameplay 适配器。
- `NetworkServer`：独立 C++ Server、协议实现和服务器测试。
- `Assets/Tests`：EditMode 和 PlayMode 测试。
- `Docs/Networking`：协议、构建、运行和测试文档。
- UI 不直接收发 Socket；旧 Gameplay 脚本不直接操作 Socket。

---

## Phase 1：开始页和场景切换

### 文件

- 新增 `Assets/Scenes/MainMenu.unity`
- 新增 `Assets/Scripts/Infrastructure/App/GameMode.cs`
- 新增 `Assets/Scripts/Infrastructure/App/GameSession.cs`
- 新增 `Assets/Scripts/Infrastructure/App/SceneLoader.cs`
- 新增 `Assets/Scripts/UI/Menu/MainMenuView.cs`
- 新增 `Assets/Scripts/UI/Menu/CreditsPanelView.cs`
- 新增 `Assets/Tests/EditMode/Infrastructure/GameSessionTests.cs`
- 修改 `ProjectSettings/EditorBuildSettings.asset`

### 脚本职责

- `GameMode`：定义空、单人、联机房主、联机加入者模式。
- `GameSession`：保存跨场景会话；本阶段只实现模式清空和读取。
- `SceneLoader`：集中加载 `MainMenu` 与 `SampleScene`。
- `MainMenuView`：控制四个主菜单入口。
- `CreditsPanelView`：显示制作人、Kenney 等素材署名、`v0.1.0`，并打开 GitHub。

### Unity 编辑器操作

1. `文件 > 新建场景 > 基础 2D`，保存为 `Assets/Scenes/MainMenu.unity`。
2. 创建 Canvas、EventSystem、标题、静态背景和四个按钮。
3. 使用现有地图、玩家和敌人 Sprite 组成静态背景，不改原素材或 Prefab。
4. 创建默认隐藏的制作人员面板，配置关闭和 GitHub 按钮。
5. `文件 > 生成设置`：`MainMenu` 索引 0，`SampleScene` 索引 1。
6. 保存场景。

### 测试与完成标准

- 在 `窗口 > 常规 > 测试运行程序` 运行会话 EditMode 测试。
- 从 `MainMenu` 播放，验证制作人员面板和 GitHub 按钮。
- Windows Development Build 首先进入开始页，打包版退出按钮有效。
- 本阶段不修改 `SampleScene` 或 Gameplay；现有单人功能不受影响。

---

## Phase 2：单人模式入口

### 文件

- 新增 `Assets/Scripts/Infrastructure/App/CharacterId.cs`
- 新增 `Assets/Scripts/Infrastructure/App/DifficultyId.cs`
- 新增 `Assets/Scripts/UI/Menu/SinglePlayerSetupView.cs`
- 新增 `Assets/Scripts/UI/Menu/SelectionCardView.cs`
- 新增 `Assets/Tests/EditMode/UI/SinglePlayerSetupTests.cs`
- 修改 `Assets/Scripts/Infrastructure/App/GameSession.cs`
- 修改 `Assets/Scenes/MainMenu.unity`
- 修改 `Assets/Scripts/Gameplay/Core/GameManager.cs`
- 修改 `Assets/Scripts/UI/RunResultView.cs`

### 脚本职责

- `CharacterId`、`DifficultyId`：稳定保存角色与难度标识。
- `SelectionCardView`：处理可用、选中和“暂未开放”状态。
- `SinglePlayerSetupView`：角色与难度均选中后写入会话并加载 `SampleScene`。

### Unity 编辑器操作

1. 创建单人准备面板、远程角色卡片、近战占位卡片和三个难度卡片。
2. 近战、困难、地狱显示“暂未开放”并禁用交互。
3. 绑定返回按钮和 `Esc`；不创建“开始游戏”按钮。
4. 把可用卡片拖入 `SinglePlayerSetupView` 对应字段并保存场景。

### 测试与完成标准

- 页面打开时两项均未选中；以任意顺序选择远程角色和普通难度后自动进入游戏。
- 未开放卡片不能选择，返回按钮和 `Esc` 均有效。
- 完整运行一次现有 5 分钟流程，移动、射击、升级、Boss、结算不变。
- 只增加单人入口和返回开始页，不改变战斗参数。

---

## Phase 3：联机菜单原型

### 文件

- 新增 `Assets/Scripts/Networking/Room/RoomRole.cs`
- 新增 `Assets/Scripts/Networking/Room/RoomPlayerState.cs`
- 新增 `Assets/Scripts/Networking/Room/RoomState.cs`
- 新增 `Assets/Scripts/UI/Menu/MultiplayerMenuView.cs`
- 新增 `Assets/Scripts/UI/Menu/RoomLobbyView.cs`
- 新增 `Assets/Scripts/UI/Menu/PlayerSlotView.cs`
- 新增 `Assets/Scripts/UI/Menu/ConnectionDialogView.cs`
- 新增 `Assets/Tests/EditMode/Networking/RoomStateTests.cs`
- 修改 `Assets/Scenes/MainMenu.unity`

### 脚本职责

- 房间状态保存房主/加入者、昵称、玩家 ID、角色、难度和准备状态。
- `RoomState` 预留 4 个逻辑槽位，第一版 UI 只显示 2 个。
- 联机菜单校验昵称、IPv6 地址和端口。
- 房间界面仅在双方准备后为房主启用开始按钮。

### Unity 编辑器操作

1. 创建联机入口和房间面板。
2. 添加昵称、服务器地址、端口三个 TMP 输入框。
3. 添加创建、加入、准备、开始和退出按钮。
4. 创建两个玩家槽位；双方可选远程角色，近战显示“暂未开放”。
5. 房主可选普通难度；加入者只读；困难、地狱不可选。
6. 保存场景。

### 测试与完成标准

- 使用模拟数据演示房主与加入者完整 UI 状态流。
- 空昵称、非法端口和空地址显示明确提示。
- 只有房主能修改难度和开始；未全部准备时不能开始。
- 不启动 Socket，不包含账号、房间列表、房间码或聊天；单人无影响。

---

## Phase 4：Host/Client 连接

### 文件

- 新增 `NetworkServer/CMakeLists.txt`
- 新增 `NetworkServer/src/main.cpp`
- 新增 `NetworkServer/src/net/SocketRuntime.h/.cpp`
- 新增 `NetworkServer/src/net/TcpListener.h/.cpp`
- 新增 `NetworkServer/src/net/UdpSocket.h/.cpp`
- 新增 `NetworkServer/src/net/SelectLoop.h/.cpp`
- 新增 `NetworkServer/src/protocol/MessageHeader.h`
- 新增 `NetworkServer/src/protocol/MessageType.h`
- 新增 `NetworkServer/src/protocol/PacketCodec.h/.cpp`
- 新增 `NetworkServer/src/room/RoomManager.h/.cpp`
- 新增 `NetworkServer/tests/PacketCodecTests.cpp`
- 新增 `Assets/Scripts/Networking/Protocol/MessageType.cs`
- 新增 `Assets/Scripts/Networking/Protocol/PacketCodec.cs`
- 新增 `Assets/Scripts/Networking/Transport/TcpClientTransport.cs`
- 新增 `Assets/Scripts/Networking/Transport/UdpClientTransport.cs`
- 新增 `Assets/Scripts/Networking/Transport/MainThreadMessageQueue.cs`
- 新增 `Assets/Scripts/Networking/Client/NetworkClient.cs`
- 新增 `Assets/Scripts/Networking/Client/ServerProcessLauncher.cs`
- 新增 `Assets/Tests/EditMode/Networking/PacketCodecTests.cs`
- 修改 Phase 3 菜单脚本接入真实连接状态

### 核心设计

- C++17 + Winsock2，不安装 NGO、Relay 或第三方网络框架。
- TCP 固定消息头处理粘包和半包；多字节整数统一网络字节序。
- UDP 包包含会话令牌、玩家 ID、序列号和消息类型。
- Server 监听 IPv6 双栈地址；开发阶段支持 `::1`。
- Unity 后台线程只处理字节，Unity 对象仅由主线程队列访问。
- 第一版单房间，`RoomManager` API 预留多房间。

### 操作、测试与完成标准

1. 使用 CMake 生成 Visual Studio x64 工程并编译 `NetworkServer.exe`。
2. 开发时手动启动服务器；Unity 配置 EXE 路径、TCP/UDP 端口。
3. 先以 `::1` 同机测试，再用打包客户端进行公网 IPv6 测试。
4. C++ 测试覆盖编码、拆包、非法长度和人数限制；C# 测试验证字节布局一致。
5. 两个客户端完成创建、加入、昵称、准备、开始和退出；断服不阻塞 Unity 主线程。
6. TCP 与 UDP 映射为同一玩家身份；本阶段尚不进入战斗同步。
7. 单人模式不创建 Socket、不启动服务器。

---

## Phase 5：玩家生成和同步

### 文件

- 新增 `Assets/Scripts/Gameplay/Characters/IPlayerInputSource.cs`
- 新增 `Assets/Scripts/Gameplay/Characters/LocalPlayerInputSource.cs`
- 新增 `Assets/Scripts/Networking/Gameplay/NetworkPlayerRegistry.cs`
- 新增 `Assets/Scripts/Networking/Gameplay/RemotePlayerInputSource.cs`
- 新增 `Assets/Scripts/Networking/Gameplay/HostPlayerStatePublisher.cs`
- 新增 `Assets/Scripts/Networking/Gameplay/ClientPlayerStateConsumer.cs`
- 新增 `Assets/Scripts/Networking/Gameplay/RemotePlayerInterpolator.cs`
- 新增 `Assets/Scripts/Networking/Gameplay/NetworkGameBootstrap.cs`
- 新增玩家状态协议和 `SequenceWindowTests.cs`
- 修改 `PlayerController.cs`、`PlayerShooter.cs`、`DashSkill.cs`、`ShotgunSkill.cs`
- 修改 `CameraFollow.cs`、`GameManager.cs` 和 `SampleScene.unity`

### 核心设计

- 本地玩家读取真实输入；远程玩家由网络输入源驱动。
- 房主模拟两名玩家，发布权威位置、朝向、生命和技能状态。
- 加入者发送输入并插值显示状态；过期和重复 UDP 包被丢弃。
- 相机只跟随本地玩家。

### Unity 编辑器操作

1. 在 `SampleScene` 创建第二玩家生成点。
2. 配置 `NetworkGameBootstrap` 的玩家 Prefab、生成点和相机引用。
3. 确保远程玩家不读取本地输入，保存场景和受影响 Prefab。

### 测试与完成标准

- 单人只有一个玩家，输入和相机不变。
- 双客户端能看到双方移动、瞄准、射击、冲刺和霰弹枪。
- 远程玩家插值稳定；重复或乱序数据不会回滚状态。
- `PlayerController` 改为输入源接口，但默认输入源保持现有单人行为。

---

## Phase 6：核心战斗状态同步

### 文件

- 新增 `NetworkEntityId.cs`、`NetworkEntityRegistry.cs`
- 新增 `HostWorldSnapshotPublisher.cs`、`ClientWorldSnapshotConsumer.cs`
- 新增 `NetworkCombatBridge.cs`、`NetworkExperienceCoordinator.cs`
- 新增 `NetworkUpgradeCoordinator.cs`、`NetworkBossCoordinator.cs`
- 新增战斗事件、世界快照协议和对应测试
- 修改 `EnemySpawner.cs`、`EnemyMovement.cs`、`EnemyHealth.cs`、`EnemyAttack.cs`
- 修改 `PlayerHealth.cs`、`ExperienceOrb.cs`、`LevelSystem.cs`
- 修改 `UpgradeManager.cs`、`UpgradePanelView.cs`
- 修改 `BossEncounterController.cs`、`BossHealth.cs`
- 修改 `GameManager.cs`、`RunResultView.cs`

### 核心设计

- 只有房主生成和模拟敌人、Boss、经验球与伤害结果。
- 加入者接收稳定实体 ID 的快照和关键事件。
- 高频位置走 UDP；生成、死亡、升级、Boss 阶段和胜负走 TCP。
- 全队共享经验；任意玩家升级时房主广播全局暂停。
- 两名玩家独立提交强化选择；选择完成后房主恢复游戏。
- 一名玩家死亡后停止战斗，另一名玩家继续。

### 6I：敌人客户端接入与多玩家索敌约束

敌人客户端生成、快照应用和多玩家索敌必须在 6I 一起实现和验证。6D
只负责敌人同步底层能力和房主权威模拟，不在加入者尚未接入敌人对象时提前
实现完整索敌逻辑。

- 只有房主运行敌人的索敌、移动、攻击和命中判定；加入者不运行敌人 AI。
- 房主从 `NetworkPlayerRegistry` 中选择存活玩家作为目标，不能继续依赖
  `GameObject.FindGameObjectWithTag("Player")` 取得单一目标。
- 默认目标规则为距离最近的存活玩家；距离相同时使用稳定的 `PlayerId`
  作为 tie-break，避免目标在两帧之间无意义抖动。
- `EnemyMovement` 与 `EnemyAttack` 必须共享同一个当前目标，避免追击目标
  与攻击目标不一致。
- 房主将目标选择产生的敌人位置、朝向、生命、死亡和攻击结果通过现有权威
  快照/事件同步给加入者；加入者只应用这些结果。
- BossProjectile 必须由房主权威生成和模拟；加入者不运行 Boss 攻击逻辑。
- 通过 TCP 关键事件或带序列号的权威事件同步 Boss 子弹的生成、方向、速度、
  伤害结果和生命周期；加入者只生成视觉对象并应用房主结果。
- Boss 死亡、战斗结束或断线时，双方必须清理尚未结束的 Boss 子弹，避免残留
  视觉对象或继续造成伤害。

6I 的双端验收至少包括：敌人分别追踪较近的 Host 或 Client、距离接近时
目标选择稳定、攻击命中正确玩家，以及两端看到的敌人位置、朝向、生命和
死亡状态一致。单人模式仍须保持原有的单目标行为。

### Unity 编辑器操作

1. 给同步的敌人和 Boss Prefab 添加网络实体标识。
2. 在 `SampleScene` 配置快照发布器、消费者和协调器引用。
3. 配置本地升级选择与远程等待状态，保存 Prefab 和场景。

### 测试与完成标准

- 双客户端完整通过普通敌人、升级、Boss 和胜利流程。
- 双方敌人数量、生命、死亡、经验和 Boss 阶段一致。
- 升级时双方暂停；一名玩家死亡后另一名玩家可继续。
- 完整单人回归通过；旧 Gameplay 只增加事件出口和适配器，不直接操作 Socket。

---

## Phase 7：房间退出、断线和错误提示

### 文件

- 新增 `ConnectionWatchdog.cs`、`DisconnectReason.cs`
- 新增 `NetworkShutdownCoordinator.cs`、`DisconnectDialogView.cs`
- 新增心跳、超时、离房协议和测试
- 修改 `ServerProcessLauncher.cs`、`NetworkGameBootstrap.cs`
- 修改 `GameManager.cs`、`RunResultView.cs`

### 核心设计

- 房主断线：加入者结束房间并返回联机菜单。
- 加入者断线：房主选择继续单人或退出。
- 不做断线重连和主机迁移。
- Unity 只关闭由自己启动的服务器；手动启动的服务器保留。
- Socket、线程和进程句柄都有明确释放路径。

### Unity 编辑器操作

1. 在菜单和战斗 Canvas 配置断线提示面板。
2. 配置“继续游戏”“退出到菜单”“确认”按钮并保存场景。

### 测试与完成标准

- 战斗前后分别关闭房主、加入者和服务器，状态符合规则。
- 客户端不卡死、不重复弹窗；远程玩家对象正确清理。
- 重复创建房间时端口可再次绑定，没有残留服务器进程。
- 单人只复用返回菜单，不执行网络断线逻辑。

---

## Phase 8：测试、文档和演示

### 文件

- 新增 `Docs/Networking/Architecture.md`
- 新增 `Docs/Networking/Protocol.md`
- 新增 `Docs/Networking/BuildAndRun.md`
- 新增 `Docs/Networking/IPv6TestGuide.md`
- 新增 `Docs/Networking/InterviewNotes.md`
- 完善 `Assets/Tests` 与 `NetworkServer/tests`
- 仅在确有构建产物需要忽略时修改 `.gitignore`

### 操作与测试

1. `文件 > 生成设置` 选择 Windows、x86_64 和 Development Build。
2. 输出包含 Unity 客户端和 `NetworkServer.exe` 的可分发目录。
3. 运行全部 Unity EditMode/PlayMode 与 C++ 测试。
4. 执行单人完整回归、同机双客户端、朋友跨网络 IPv6 测试。
5. 测试延迟、少量丢包、非法地址、端口占用和服务器关闭。
6. 记录服务器控制台、房间 UI、双人战斗、Boss 和结算演示。

### 完成标准

- Windows 10/11 x64 打包版无需 Unity 编辑器即可运行。
- 单人和双人合作流程都通过验收。
- 文档能指导其他开发者构建、运行和讲解项目。
- Git 不包含日志、服务器构建目录或私人 IP 地址。

---

## 执行规则

1. 从 Phase 1 开始，不跨阶段提前实现网络代码。
2. 每次修改前列出本阶段实际修改文件。
3. 修改后提供中文菜单路径、对象、组件、字段引用和参数值。
4. 每个 Phase 先运行自动化测试，再提供人工测试步骤。
5. 用户测试确认前不进入下一阶段。
6. 不自动提交、不自动推送、不修改无关素材或布局。
7. 发现现有代码风险时先说明，不顺手扩大任务范围。
