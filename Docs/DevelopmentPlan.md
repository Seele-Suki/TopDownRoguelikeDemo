# TopDownRoguelikeDemo 开发规划

## 1. 开发原则

本项目按“可运行闭环优先、系统可扩展其次、表现最后补强”的方式推进。

每个阶段都必须满足：

1. 当前阶段功能能在 Unity Editor 中运行验证。
2. 代码能解释清楚模块职责。
3. 每次完成阶段后提交 Git。
4. 不把无法讲清楚的复杂功能写进简历。

## 2. 阶段总览

| 阶段 | 目标 | 技术重点 |
| --- | --- | --- |
| Phase 0 | 工程初始化 | Git、Unity 项目设置、目录结构 |
| Phase 1 | 玩家移动闭环 | Input Manager、Rigidbody2D、Camera |
| Phase 2 | 基础战斗闭环 | Collider2D、DamageInfo、Object Pool |
| Phase 3 | 敌人与生成 | FSM、EnemyData、Spawner |
| Phase 4 | 经验与升级 | Experience Orb、LevelSystem、事件 |
| Phase 5 | 肉鸽强化 | ScriptableObject、UpgradeEffect、UGUI |
| Phase 6 | 技能系统 | Dash、Cooldown、ActiveSkill |
| Phase 7 | 表现与演示 | Animator、VFX、SFX、Build |
| Phase 8 | 简历与复盘 | README、架构图、面试讲解稿 |

## 3. Phase 0：工程初始化

目标：

建立可维护的 Unity 工程基础。

任务：

1. 配置 Unity 项目为 Force Text 和 Visible Meta Files。
2. 配置 `.gitignore`、`.gitattributes`、`README.md`。
3. 建立 `Docs/` 文档目录。
4. 建立基础资源目录结构。

技术：

Unity Editor Settings、Git、GitHub、Markdown。

验收标准：

1. GitHub 仓库可正常访问。
2. `Library/`、`Logs/`、`UserSettings/` 不进入 Git。
3. 文档中明确工程结构和阶段规划。

## 4. Phase 1：玩家移动闭环

目标：

实现一个可控制的玩家角色，支持俯视角移动和鼠标朝向。

任务：

1. 创建 Player 占位 Sprite。
2. 实现 `PlayerController`。
3. 使用 `Rigidbody2D` 移动。
4. 实现鼠标方向朝向。
5. 配置主摄像机跟随玩家。

技术：

Unity Input Manager、Rigidbody2D、Camera Follow、MonoBehaviour。

验收标准：

1. WASD 可移动。
2. 玩家移动不穿透基础碰撞。
3. 玩家能朝鼠标方向旋转或更新朝向。
4. 代码中输入、移动、朝向职责清晰。

## 5. Phase 2：基础战斗闭环

目标：

实现玩家远程射击、子弹命中敌人、敌人扣血死亡。

任务：

1. 设计 `DamageInfo`。
2. 设计 `IDamageable`。
3. 实现 `Projectile`。
4. 实现 `RangedWeapon`。
5. 实现基础对象池。
6. 创建 Dummy Enemy 供测试。

技术：

Collider2D Trigger、LayerMask、Object Pool、C# interface。

验收标准：

1. 鼠标左键可射击。
2. 子弹命中敌人后造成伤害。
3. 敌人生命归零后消失。
4. 子弹不频繁 Instantiate/Destroy。

## 6. Phase 3：敌人与生成

目标：

实现基础敌人追击、攻击和波次生成。

任务：

1. 实现 `EnemyController`。
2. 实现简单 FSM：Idle、Chase、Attack、Dead。
3. 实现 `EnemySpawner`。
4. 用 `EnemyData` 配置敌人数值。
5. 支持定时生成敌人。

技术：

FSM、ScriptableObject、Rigidbody2D、Collider2D。

验收标准：

1. 敌人能朝玩家移动。
2. 敌人接近玩家后造成伤害。
3. 敌人死亡后触发事件。
4. 生成器能持续制造战斗压力。

## 7. Phase 4：经验与升级

目标：

实现击杀敌人掉落经验，玩家拾取后升级。

任务：

1. 实现 `ExperienceOrb`。
2. 实现 `ExperienceCollector`。
3. 实现 `LevelSystem`。
4. 实现经验条 UI。
5. 触发升级事件。

技术：

Trigger Collider2D、Object Pool、AnimationCurve、UGUI Slider、C# event。

验收标准：

1. 敌人死亡掉落经验。
2. 玩家靠近后拾取经验。
3. 经验条实时变化。
4. 达到阈值后玩家升级。

## 8. Phase 5：肉鸽三选一强化

目标：

实现升级时暂停战斗，弹出三个强化选项，选择后应用效果。

任务：

1. 实现 `UpgradeData`。
2. 实现 `UpgradeEffect` 基类。
3. 实现 `UpgradeManager`。
4. 实现 `UpgradePanelView`。
5. 完成至少 6 个强化。

强化类型：

| 强化 | 效果 |
| --- | --- |
| Fire Rate Up | 提升射速 |
| Bullet Damage Up | 提升子弹伤害 |
| Move Speed Up | 提升移速 |
| Max Health Up | 提升最大生命 |
| Dash Cooldown Down | 降低冲刺冷却 |
| Projectile Count Up | 增加弹丸数量 |

技术：

ScriptableObject、UGUI Button、C# abstract class、事件通信。

验收标准：

1. 升级后出现三个随机强化。
2. 选择强化后面板关闭。
3. 对应属性立即生效。
4. 强化系统可以继续扩展新效果。

## 9. Phase 6：技能系统

目标：

实现冲刺和一个主动技能，让角色操作有动作游戏手感。

任务：

1. 实现 `DashSkill`。
2. 实现技能冷却显示。
3. 实现一个主动技能雏形。
4. 接入升级强化。

技术：

Coroutine、Rigidbody2D、Cooldown Timer、UGUI Image Fill。

验收标准：

1. 玩家按键可冲刺。
2. 冲刺有冷却。
3. 冲刺期间具备位移手感。
4. UI 能显示冷却进度。

## 10. Phase 7：表现与演示

目标：

把占位 Demo 打磨成可录屏、可展示、可面试讲解的版本。

任务：

1. 替换基础免费素材。
2. 增加射击、受击、死亡反馈。
3. 增加简单音效。
4. 制作开始、战斗、结束 UI。
5. 打 Windows 构建包。

技术：

Animator、Animation Clip、ParticleSystem、AudioSource、Build Settings。

验收标准：

1. Demo 能完整演示 3-5 分钟。
2. 玩家能看懂目标和成长反馈。
3. 战斗反馈清晰。
4. 构建包可在本机运行。

## 11. Phase 8：简历与复盘

目标：

整理项目展示材料，为投递和面试准备。

任务：

1. 更新 README。
2. 补充架构图。
3. 写核心模块说明。
4. 准备 2 分钟项目介绍。
5. 准备常见面试追问。

技术：

Markdown、Mermaid、GitHub README、录屏/GIF。

验收标准：

1. GitHub 首页能说明项目亮点。
2. README 有运行环境、核心功能、模块结构。
3. 能讲清楚战斗、对象池、升级系统和 ScriptableObject 配置。

## 12. 推荐提交节奏

每完成一个小闭环提交一次。

提交示例：

```text
Add player movement controller
Add projectile pooling
Add enemy chase behavior
Add experience and level system
Add upgrade selection UI
Add dash skill cooldown
```

## 13. 第一版完成标准

第一版 Demo 达到以下标准即可进入简历包装：

1. 玩家可以移动、射击、冲刺。
2. 敌人可以生成、追击、攻击、死亡。
3. 击杀敌人可以获得经验。
4. 升级可以三选一强化。
5. 至少有 6 个强化选项。
6. 至少有 2 种敌人或 1 种敌人加 1 个精英雏形。
7. 有基础 UI 和可演示构建。
8. GitHub 仓库整洁，README 能看懂项目。

