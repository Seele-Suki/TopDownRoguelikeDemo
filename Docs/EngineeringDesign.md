# TopDownRoguelikeDemo 工程设计文档

## 1. 项目定位

本项目是一个 Unity 2D 俯视角动作 Roguelike Demo，目标是作为校招 Gameplay/玩法开发方向的主项目。

核心玩法循环：

玩家移动与瞄准 -> 射击/近战/冲刺 -> 击败敌人 -> 获得经验 -> 升级三选一强化 -> 进入更高强度战斗。

项目优先级：

1. 先保证核心玩法闭环稳定。
2. 再扩展技能、敌人、升级强化和关卡节奏。
3. 最后补充表现、音效、简单 UI、可演示构建和简历说明。

## 2. 基础技术栈

| 部分 | 技术/框架 |
| --- | --- |
| 游戏引擎 | Unity 2022.3.62f3c1 LTS |
| 渲染管线 | 2D Built-In Render Pipeline |
| 开发语言 | C# |
| IDE | Visual Studio 2022 |
| 输入方案 | Unity Input Manager / 旧输入系统 |
| 物理系统 | Unity 2D Physics |
| 数据配置 | ScriptableObject |
| UI 系统 | Unity UGUI |
| 动画 | Animator + Animation Clip |
| 对象复用 | 自研 Object Pool |
| 事件通信 | C# event / Action |
| 场景管理 | Unity SceneManager |
| 版本管理 | Git + GitHub |

## 3. 工程目录规划

```text
Assets/
  Art/
  Audio/
  Prefabs/
    Characters/
    Enemies/
    Projectiles/
    UI/
    VFX/
  Scenes/
  ScriptableObjects/
    Characters/
    Enemies/
    Skills/
    Upgrades/
    Weapons/
  Scripts/
    Core/
    Gameplay/
      Characters/
      Combat/
      Enemies/
      Experience/
      Skills/
      Upgrades/
      Weapons/
    Infrastructure/
      Pooling/
      Events/
      Save/
    UI/
    Utilities/
  Settings/
```

## 4. 模块设计

### 4.1 Core 核心层

职责：

维护游戏状态、关卡流程、暂停/恢复、全局服务入口。

主要类：

| 类名 | 职责 |
| --- | --- |
| GameManager | 管理游戏主状态，如准备、战斗中、升级选择、结束 |
| GameState | 定义游戏状态枚举 |
| SceneLoader | 封装场景加载 |
| TimeController | 管理暂停、慢动作、恢复 |

技术：

Unity MonoBehaviour、C# enum、C# event、SceneManager。

### 4.2 Player 角色模块

职责：

处理玩家移动、朝向、受击、生命值、技能入口和动画状态。

主要类：

| 类名 | 职责 |
| --- | --- |
| PlayerController | 读取输入并驱动移动、朝向 |
| PlayerStats | 保存玩家生命、移速、攻击等运行时属性 |
| PlayerHealth | 管理受击、死亡、无敌帧 |
| PlayerAnimationController | 管理动画参数 |

技术：

Unity Input Manager、Rigidbody2D、Collider2D、Animator。

### 4.3 Combat 战斗模块

职责：

管理伤害、命中、击退、暴击、阵营关系和伤害结算。

主要类：

| 类名 | 职责 |
| --- | --- |
| DamageInfo | 描述一次伤害的数据 |
| IDamageable | 可受击对象接口 |
| Hitbox | 攻击判定区域 |
| Hurtbox | 受击判定区域 |
| KnockbackReceiver | 处理击退 |

技术：

C# interface、struct/class 数据对象、Collider2D Trigger、LayerMask。

### 4.4 Weapon 武器模块

职责：

支持主射击武器、近战武器、弹道生成和武器参数配置。

主要类：

| 类名 | 职责 |
| --- | --- |
| WeaponController | 当前武器入口 |
| RangedWeapon | 远程武器逻辑 |
| MeleeWeapon | 近战武器逻辑 |
| Projectile | 子弹生命周期和命中 |
| WeaponData | 武器配置 |

技术：

ScriptableObject、Object Pool、Collider2D、MonoBehaviour。

### 4.5 Skill 技能模块

职责：

实现冲刺、主动技能、技能冷却和技能升级扩展。

主要类：

| 类名 | 职责 |
| --- | --- |
| SkillController | 玩家技能统一入口 |
| DashSkill | 冲刺技能 |
| ActiveSkill | 主动技能基类 |
| SkillCooldown | 冷却计时 |
| SkillData | 技能配置 |

技术：

ScriptableObject、Coroutine、Time.deltaTime、C# abstract class。

### 4.6 Enemy 敌人模块

职责：

实现敌人生成、寻路追击、攻击、死亡掉落和波次压力。

主要类：

| 类名 | 职责 |
| --- | --- |
| EnemyController | 敌人状态入口 |
| EnemyMovement | 敌人移动和追击 |
| EnemyAttack | 敌人攻击 |
| EnemyHealth | 敌人生命和死亡 |
| EnemySpawner | 生成敌人 |
| EnemyData | 敌人配置 |

技术：

Rigidbody2D、Collider2D、ScriptableObject、简单有限状态机 FSM。

### 4.7 Experience 经验升级模块

职责：

处理经验掉落、拾取、等级成长和升级事件。

主要类：

| 类名 | 职责 |
| --- | --- |
| ExperienceOrb | 经验球 |
| ExperienceCollector | 玩家拾取经验 |
| LevelSystem | 等级和经验曲线 |
| LevelUpEvent | 升级事件 |

技术：

Trigger Collider2D、AnimationCurve、C# event、Object Pool。

### 4.8 Upgrade 肉鸽强化模块

职责：

在升级时从强化池中抽取三个选项，玩家选择后改变角色、武器或技能参数。

主要类：

| 类名 | 职责 |
| --- | --- |
| UpgradeManager | 管理升级选项生成和应用 |
| UpgradeData | 强化配置 |
| UpgradeEffect | 强化效果基类 |
| UpgradeOptionView | 单个强化选项 UI |

技术：

ScriptableObject、UGUI、C# abstract class、事件通信。

### 4.9 UI 模块

职责：

显示生命值、经验条、等级、技能冷却、升级三选一面板和游戏结束面板。

主要类：

| 类名 | 职责 |
| --- | --- |
| HUDController | HUD 总入口 |
| HealthBarView | 生命条 |
| ExperienceBarView | 经验条 |
| SkillCooldownView | 技能冷却显示 |
| UpgradePanelView | 升级三选一面板 |

技术：

Unity UGUI、Slider、Button、TextMeshPro。

### 4.10 Infrastructure 基础设施模块

职责：

提供对象池、事件中心、工具函数和调试辅助。

主要类：

| 类名 | 职责 |
| --- | --- |
| ObjectPool | 通用对象池 |
| PoolableObject | 可复用对象基类 |
| EventBus | 轻量事件中心 |
| GameLogger | 调试日志封装 |

技术：

C# generic、Queue、Dictionary、C# event。

## 5. 数据配置设计

优先使用 ScriptableObject 管理可调参数。

配置类型：

| 配置 | 内容 |
| --- | --- |
| CharacterData | 角色生命、移速、基础攻击 |
| WeaponData | 射速、伤害、弹速、散射、射程 |
| SkillData | 冷却、持续时间、倍率 |
| EnemyData | 生命、速度、伤害、经验掉落 |
| UpgradeData | 强化名称、描述、稀有度、效果 |

## 6. 事件设计

基础事件：

| 事件 | 触发时机 |
| --- | --- |
| OnPlayerDamaged | 玩家受击 |
| OnEnemyKilled | 敌人死亡 |
| OnExperienceChanged | 经验变化 |
| OnPlayerLevelUp | 玩家升级 |
| OnUpgradeSelected | 玩家选择强化 |
| OnGameOver | 游戏结束 |

事件只用于跨模块通知，不用于承载复杂业务逻辑。

## 7. 可写入简历的技术点

1. 基于 Unity 2D Physics 实现俯视角动作战斗和命中判定。
2. 使用 ScriptableObject 设计角色、武器、敌人、技能和肉鸽强化配置。
3. 设计对象池复用子弹、经验球和敌人，减少频繁 Instantiate/Destroy。
4. 使用事件机制解耦战斗、经验、UI 和升级模块。
5. 实现升级三选一强化系统，支持数值型和机制型强化扩展。
6. 使用 Git 管理 Unity 工程，配置 Force Text、Visible Meta Files、.gitignore 和 .gitattributes。

## 8. 不在第一阶段实现的内容

以下内容暂不进入第一阶段：

1. 存档系统。
2. 联机。
3. 复杂 AI 行为树。
4. 大地图程序生成。
5. 商业化资源管理系统。
6. 完整剧情和任务系统。

这些内容可以作为后续扩展，不影响第一版 Demo 的核心展示价值。

