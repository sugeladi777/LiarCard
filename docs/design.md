# LiarCard 玩法与架构设计

## 核心玩法

### 基础规则

* 使用一副或多副扑克牌。
* 包含大小王。
* Joker 是万能牌，可以当作任意点数。
* 最先出完手牌的玩家获胜。

### 回合流程

1. 一名玩家开启新一轮。
2. 开轮玩家自由声明一个点数，并打出任意数量的牌。
3. 本轮其他玩家只能围绕该点数行动。
4. 玩家可以选择：跟牌、不跟、质疑、使用技能。

### 跟牌

玩家继续声明本轮相同点数，并打出任意数量手牌。

实际出的牌可以是真，也可以是假。

每当有玩家成功跟牌后，之前的不跟记录清空，重新从该玩家之后开始计算。

### 不跟

玩家跳过本次行动。

如果所有其他玩家都不跟，中央牌堆清空，由最后出牌者开启新一轮。

### 质疑

玩家可以质疑上一名出牌者。

只检查上一名玩家刚出的牌：

* 如果全是声明点数或 Joker，质疑失败，质疑者拿走中央牌堆。
* 如果存在不符合声明的普通牌，质疑成功，出牌者拿走中央牌堆。
* 质疑结算后，由质疑胜利方开启新一轮。

### 胜利规则

玩家出完最后一张牌后，进入质疑窗口。

* 没人成功质疑：该玩家获胜。
* 被成功质疑：该玩家拿走中央牌堆，游戏继续。

### 测谎仪机制

每名玩家都有测谎仪。

其他玩家出牌后，会显示其说谎概率。

测谎仪的概率受以下因素影响：
- 职业特性
- 技能特性
- 历史对出牌玩家的质疑成功率
- 出牌数量

### 职业系统

初版设计 4 个职业：

#### 侦探

主动技能，每轮一次，提高本次测谎准确度。

#### 骗术师

被动技能，降低自己被测出的说谎概率。

#### 赌徒

被动技能，质疑奖惩增强。

#### 魔术师

主动技能，每局一次，将一张普通牌临时视为 Joker。

## Unity 架构设计

### 1. 架构目标

LiarCard 采用 **分层架构 + 状态机 + 事件驱动**。

核心目标：

* 规则逻辑和 Unity 表现分离
* 玩法系统可扩展
* AI、测谎仪、职业技能可以独立开发
* 避免所有逻辑堆进 `GameManager`

### 2. 总体目录结构

```text
Assets/Scripts/
├── Core/                  # 核心规则层
├── Gameplay/              # 游戏流程层
├── Systems/               # 玩法系统层
│   ├── LieDetector/       # 测谎仪系统
│   ├── Skill/             # 职业技能系统
│   └── AI/                # AI 决策系统
├── Presentation/          # 表现层
│   ├── UI/                # 界面交互
│   └── View/              # 场景表现
├── Data/                  # 配置数据
└── Infrastructure/        # 基础服务
```

### 3. 依赖关系

依赖方向：

```text
Presentation
    ↓
Gameplay
    ↓
Systems
    ↓
Core
```

说明：

* `Core` 不依赖任何 Unity 场景对象。
* `Gameplay` 调用 `Core` 和 `Systems` 推进游戏。
* `Systems` 负责测谎仪、职业技能、AI 等独立玩法。
* `Presentation` 只负责输入、显示和动画。
* `View` 不直接修改游戏状态，只根据事件刷新画面。

## 4. Core：核心规则层

`Core` 是游戏最底层。

### 主要类

```text
Card
PlayerState
GameState
RoundState
Move
RuleEngine
```

### 职责

* 保存卡牌数据
* 保存玩家状态
* 保存当前轮状态
* 判断 Joker 是否有效
* 判断出牌是否合法
* 判断质疑是否成功
* 判断是否进入胜利状态

## 5. Gameplay：游戏流程层

`Gameplay` 负责控制整局游戏的运行。

### 主要类

```text
GameController
GameStateMachine
TurnManager
RoundManager
DeckManager
ActionResolver
```

### 职责

* 初始化游戏
* 创建牌组
* 洗牌发牌
* 控制当前玩家
* 处理跟牌、不跟、质疑
* 处理无人跟牌清场
* 处理胜利判定
* 调用测谎仪和技能系统
* 派发游戏事件

### 类职责划分

#### GameController

游戏总入口，负责协调各模块。

不直接写复杂规则，只负责组织流程。

#### GameStateMachine

管理游戏状态切换。

例如：

```text
Init → DealCards → RoundStart → PlayerTurn → WaitForChallenge
```

#### TurnManager

管理玩家顺序。

负责：

```text
当前玩家是谁
下一家是谁
谁开启新一轮
```

#### RoundManager

管理单轮流程。

负责：

```text
当前声明点数
最后出牌者
不跟玩家记录
中央牌堆
无人跟牌判断
```

#### DeckManager

负责牌组。

负责：

```text
创建牌组
洗牌
发牌
支持多副牌
支持 Joker
```

#### ActionResolver

处理玩家行动结果。

负责：

```text
执行出牌
执行不跟
执行质疑
结算中央牌堆
触发 PendingWin
```

## 6. Systems：玩法系统层

`Systems` 存放可独立扩展的玩法系统。

### 6.1 LieDetector：测谎仪系统

#### 主要类

```text
LieDetectorSystem
LieResult
LieDetectorContext
LieDetectorConfig
```

#### 职责

* 根据出牌行为计算说谎概率
* 加入随机误差
* 受职业和技能影响
* 输出最终显示给玩家的概率

#### 输入

```text
出牌玩家
声明点数
出牌数量
当前中央牌堆数量
玩家历史质疑记录
职业技能修正
```

#### 输出

```text
LieResult
- lieProbability
- accuracyLevel
- isModifiedBySkill
```

### 6.2 Skill：职业技能系统

#### 主要类

```text
SkillManager
SkillBase
RoleData
SkillContext
SkillResult
DetectiveSkill
TricksterSkill
GamblerSkill
MagicianSkill
```

#### 职责

* 管理玩家职业
* 处理主动技能
* 处理被动技能
* 修改测谎结果
* 修改质疑结算
* 修改 Joker 判定

#### 技能触发时机

```text
出牌前
测谎前
质疑前
质疑结算后
胜利判定前
```

#### 初版职业

```text
侦探：主动技能，提高测谎准确度
骗术师：被动技能，降低自己被测出的说谎概率
赌徒：被动技能，强化质疑奖惩
魔术师：主动技能，将一张普通牌临时视为 Joker
```

### 6.3 AI：电脑玩家系统

#### 主要类

```text
AIController
AIDecision
AIContext
SimpleAI
RoleAI
```

#### 职责

* 判断是否跟牌
* 判断是否不跟
* 判断是否质疑
* 判断是否使用职业技能
* 选择出哪些牌
* 决定是否撒谎

#### AI 输入

```text
当前手牌
当前声明点数
测谎仪结果
中央牌堆数量
其他玩家剩余手牌数
自身职业
当前风险
```

#### AI 输出

```text
AIDecision
- Play
- Pass
- Challenge
- UseSkill
```

## 7. Presentation：表现层

`Presentation` 只负责玩家看到和操作的内容。

### 7.1 UI：界面交互

#### 主要类

```text
GameUIController
HandPanel
ActionPanel
DeclarePanel
LieDetectorPanel
RoleSkillPanel
ResultPanel
```

#### 职责

* 显示玩家手牌
* 显示当前声明点数
* 显示跟牌、不跟、质疑按钮
* 显示测谎概率
* 显示职业技能按钮
* 显示结算结果

UI 不直接修改 `GameState`，只把玩家输入提交给 `GameController`。

### 7.2 View：场景表现

#### 主要类

```text
CardView
PlayerView
TablePileView
CardAnimationController
```

#### 职责

* 显示卡牌正反面
* 显示玩家头像和手牌数量
* 显示中央牌堆
* 播放出牌动画
* 播放翻牌动画
* 播放拿走牌堆动画
* 播放职业技能表现

`View` 不判断规则，只根据事件刷新画面。

## 8. Data：配置数据层

使用 `ScriptableObject` 保存配置。

### 主要配置

```text
GameConfig
AIConfig
RoleConfig
SkillConfig
LieDetectorConfig
CardSkinConfig
```

### 职责

* 配置玩家人数
* 配置牌组数量
* 配置是否启用 Joker
* 配置职业参数
* 配置技能冷却和次数
* 配置测谎仪误差
* 配置 AI 难度
* 配置卡牌皮肤

## 9. Infrastructure：基础服务层

放置与具体玩法无关的通用服务。

### 主要类

```text
EventBus
AudioService
SaveService
SceneLoader
ObjectPool
```

### 职责

* 派发游戏事件
* 播放音效
* 保存设置
* 切换场景
* 管理卡牌对象池

## 10. 状态机设计

游戏状态：

```text
Init
DealCards
RoundStart
PlayerTurn
WaitForChallenge
ResolveChallenge
RoundClear
PendingWin
GameOver
```

### 状态说明

#### Init

初始化配置和玩家数据。

#### DealCards

创建牌组、洗牌、发牌。

#### RoundStart

开启新一轮，当前玩家选择声明点数并出牌。

#### PlayerTurn

当前玩家行动，可以跟牌、不跟、质疑或使用技能。

#### WaitForChallenge

某玩家出牌后，进入质疑窗口，并显示测谎仪概率。

#### ResolveChallenge

结算质疑结果，决定谁拿走中央牌堆。

#### RoundClear

无人跟牌，中央牌堆清空，由最后出牌者开启新一轮。

#### PendingWin

玩家出完手牌，但需要等待是否被成功质疑。

#### GameOver

游戏结束，显示胜利结果。

## 11. 核心流程

### 11.1 玩家出牌流程

```text
玩家选择手牌
→ 选择声明点数
→ UI 提交操作
→ GameController 接收
→ RuleEngine 判断是否合法
→ SkillManager 应用技能修正
→ 更新 GameState
→ LieDetectorSystem 计算说谎概率
→ EventBus 派发出牌事件
→ UI / View 刷新
→ 进入 WaitForChallenge
```

### 11.2 质疑流程

```text
玩家点击质疑
→ GameController 接收
→ RuleEngine 判断上一手真假
→ SkillManager 修正质疑结果
→ 失败方拿走中央牌堆
→ 更新 GameState
→ EventBus 派发质疑结果
→ 质疑胜利方开启新一轮
```

### 11.3 无人跟牌流程

```text
玩家选择不跟
→ RoundManager 记录不跟玩家
→ 判断是否所有其他玩家都不跟
→ 如果没有结束，轮到下一家
→ 如果结束，中央牌堆清空
→ 最后出牌者开启新一轮
```

### 11.4 胜利流程

```text
玩家出完手牌
→ 进入 PendingWin
→ 等待其他玩家质疑
→ 如果无人成功质疑，游戏结束
→ 如果被成功质疑，玩家拿回中央牌堆，游戏继续
```
