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

## 项目架构设计

### 1. 架构目标

LiarCard 采用：

> **Unity 3D 客户端 + Go 在线权威服务器 + Python 强化学习训练**

### 2. 总体仓库结构

```text
LiarCard/
├── Client/                 # Unity 3D 客户端
├── Server/                 # Go 在线权威服务器
├── AITraining/             # Python 强化学习训练
├── docs/                   # 项目文档
├── README.md
└── .gitignore
```

### 3. Client 内部顶层结构

```text
Client/
├── Assets/
│   ├── Scenes/             # Unity 场景
│   ├── Scripts/            # 游戏脚本
│   ├── Prefabs/            # 预制体
│   ├── Art/                # 美术资源
│   ├── Audio/              # 音频资源
│   ├── Materials/          # 材质资源
│   └── Animations/         # 动画资源
├── Packages/
└── ProjectSettings/
```

### 4. Client/Scripts 内部顶层结构

```text
Client/Assets/Scripts/
├── App/                    # 游戏启动、模式切换、全局入口
├── Presentation/           # UI、3D 表现、动画和特效
├── Application/            # 玩家操作入口，统一调度不同游戏模式
├── LocalGame/              # 本地单机逻辑与本地规则结算
├── OnlineGame/             # 在线模式客户端逻辑
├── ClientState/            # 客户端状态镜像
├── RLRuntime/              # Unity 内强化学习 AI 运行模块
├── Data/                   # 客户端配置数据
└── Infrastructure/         # 客户端基础服务
```


### 5. Server 内部顶层结构

```text
Server/
├── cmd/                    # 服务端启动入口
├── internal/               # 服务端内部逻辑
├── configs/                # 服务端配置
├── scripts/                # 部署与运维脚本
├── deployments/            # Docker、服务器部署配置
└── go.mod
```

### 6. Server/internal 内部顶层结构

```text
Server/internal/
├── ws/                     # WebSocket 连接
├── room/                   # 房间管理
├── game/                   # 对局流程与状态机
├── core/                   # 核心规则
├── systems/                # 测谎仪、职业技能、AI 托管
├── protocol/               # 客户端与服务器通信协议
├── storage/                # 数据存储
└── platform/               # Steam 等平台接入
```

### 7. AITraining 内部顶层结构

```text
AITraining/
├── env/                    # 强化学习环境封装
├── agents/                 # AI 智能体
├── training/               # 训练流程
├── evaluation/             # 模型评估
├── configs/                # 训练配置
├── models/                 # 已训练模型
├── logs/                   # 训练日志
├── data/                   # 对局数据
├── scripts/                # 辅助脚本
├── train.py                # 训练入口
└── requirements.txt        # Python 依赖
```

### 8. 模式关系

```text
本地单机模式：
Client
└── LocalGame

强化学习模式：
Client
├── LocalGame
└── RL
    ↕
AITraining

在线多人模式：
Client
└── Network
    ↕
Server
```
