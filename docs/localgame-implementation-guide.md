# LocalGame 文件编写指南

这份文档用于指导下一阶段的本地单机规则开发。目标不是一次写完所有功能，而是让每个文件的职责清楚、接口稳定，然后逐步把“发牌 -> 出牌 -> 不跟 -> 质疑 -> 胜利”跑通。

当前阶段建议只改这些文件：

- `Client/Assets/Scripts/LocalGame/Core/Card.cs`
- `Client/Assets/Scripts/LocalGame/Core/PlayerState.cs`
- `Client/Assets/Scripts/LocalGame/Core/RoundState.cs`
- `Client/Assets/Scripts/LocalGame/Core/GameState.cs`
- `Client/Assets/Scripts/LocalGame/Gameplay/DeckManager.cs`
- `Client/Assets/Scripts/LocalGame/Gameplay/LocalGameService.cs`
- `Client/Assets/Scripts/App/GameBootstrap.cs`

暂时不要手动改 `.meta` 文件。Unity 会维护它们。

## 1. 总体分层原则

### Core 层

路径：

```text
Client/Assets/Scripts/LocalGame/Core/
```

职责：

- 保存游戏状态。
- 提供轻量的状态操作。
- 不依赖 Unity。
- 不写 `Debug.Log`。
- 不关心 UI、场景、动画、网络。

Core 层应该是纯 C# 逻辑。以后 Go 服务端或 AI 训练要复用规则时，Core 层越干净越好。

### Gameplay 层

路径：

```text
Client/Assets/Scripts/LocalGame/Gameplay/
```

职责：

- 编排游戏流程。
- 校验玩家动作是否合法。
- 调用 Core 层修改状态。
- 实现出牌、不跟、质疑、胜利判定。

`LocalGameService` 应该是本地游戏规则的主要入口。以后 UI 不应该直接改 `GameState`，而是调用 `LocalGameService`。

### App 层

路径：

```text
Client/Assets/Scripts/App/
```

职责：

- Unity 启动入口。
- 创建本地游戏服务。
- 临时输出调试日志。

`GameBootstrap` 不应该写规则。它只负责启动和临时调试。

## 2. Card.cs

路径：

```text
Client/Assets/Scripts/LocalGame/Core/Card.cs
```

### 当前职责

`Card` 表示一张牌。

当前已有：

- `Suit`
- `Rank`
- `Card.Id`
- `Card.Suit`
- `Card.Rank`

### 建议补充

给 `Card` 增加一些只读判断方法，让质疑逻辑更清楚。

建议新增：

```csharp
public bool IsJoker => Suit == Suit.Joker;

public bool MatchesDeclaredRank(Rank declaredRank)
{
    return IsJoker || Rank == declaredRank;
}

public override string ToString()
{
    return $"{Suit} {Rank}";
}
```

### 编写原则

- `Card` 不应该修改自己的点数或花色。
- Joker 是否能当任意点数，可以通过 `MatchesDeclaredRank` 判断。
- 暂时不要把“魔术师技能临时把普通牌当 Joker”写进 `Card`。技能属于后续 systems 或规则修正层。
- `Id` 应该保持唯一，用来识别同一张物理牌。

### 验收标准

- 普通牌只有点数相等时才匹配声明点数。
- Joker 匹配任意声明点数。
- `ToString()` 可以帮助调试日志看懂牌面。

## 3. PlayerState.cs

路径：

```text
Client/Assets/Scripts/LocalGame/Core/PlayerState.cs
```

### 当前职责

`PlayerState` 表示一个玩家的本地状态。

当前已有：

- `Id`
- `HandCards`
- `AddCardToHand`

### 建议补充

玩家需要能安全地收牌、出牌、检查是否拥有某些牌。

建议新增：

```csharp
public bool HasCard(Card card)
{
    return HandCards.Exists(handCard => handCard.Id == card.Id);
}

public bool HasCards(IEnumerable<Card> cards)
{
    foreach (Card card in cards)
    {
        if (!HasCard(card))
        {
            return false;
        }
    }

    return true;
}

public void AddCardsToHand(IEnumerable<Card> cards)
{
    HandCards.AddRange(cards);
}

public void RemoveCardsFromHand(IEnumerable<Card> cards)
{
    foreach (Card card in cards)
    {
        int index = HandCards.FindIndex(handCard => handCard.Id == card.Id);
        if (index < 0)
        {
            throw new System.InvalidOperationException($"Card not found in hand: {card.Id}");
        }

        HandCards.RemoveAt(index);
    }
}

public bool HasNoCards => HandCards.Count == 0;
```

### 编写原则

- 外部不要直接随意改 `HandCards`。
- 第一版可以保留 `List<Card>`，但尽量通过方法增删手牌。
- 移除手牌时用 `Id` 判断，不要依赖对象引用。
- 玩家排序手牌可以放在 `DeckManager` 或 `PlayerState.SortHandCards`，但不要两个地方都写不同逻辑。

### 验收标准

- 玩家出牌前可以检查是否拥有这些牌。
- 玩家出牌后，手牌数量正确减少。
- 玩家拿走中央牌堆后，手牌数量正确增加。
- 试图移除不存在的牌会明确报错。

## 4. RoundState.cs

路径：

```text
Client/Assets/Scripts/LocalGame/Core/RoundState.cs
```

### 当前职责

`RoundState` 保存当前一轮的状态。

当前已有：

- `StarterPlayerId`
- `CurrentDeclaredRank`
- `LastPlayedPlayerId`
- `LastPlayedCards`
- `PassedPlayerIds`
- `PendingWinnerPlayerId`
- `StartNewRound`

### 建议补充

回合状态需要记录出牌、不跟、待确认胜利。

建议新增：

```csharp
public bool HasDeclaredRank => CurrentDeclaredRank != null;

public bool HasLastPlay =>
    !string.IsNullOrEmpty(LastPlayedPlayerId) && LastPlayedCards.Count > 0;

public bool HasPendingWinner =>
    !string.IsNullOrEmpty(PendingWinnerPlayerId);

public void RecordPlay(string playerId, Rank declaredRank, List<Card> playedCards)
{
    if (playedCards == null || playedCards.Count == 0)
    {
        throw new System.ArgumentException("Played cards cannot be empty.");
    }

    if (CurrentDeclaredRank == null)
    {
        CurrentDeclaredRank = declaredRank;
    }
    else if (CurrentDeclaredRank.Value != declaredRank)
    {
        throw new System.InvalidOperationException("Declared rank must match current round.");
    }

    LastPlayedPlayerId = playerId;
    LastPlayedCards.Clear();
    LastPlayedCards.AddRange(playedCards);
    PassedPlayerIds.Clear();
}

public void RecordPass(string playerId)
{
    PassedPlayerIds.Add(playerId);
}

public void MarkPendingWinner(string playerId)
{
    PendingWinnerPlayerId = playerId;
}

public void ClearPendingWinner()
{
    PendingWinnerPlayerId = string.Empty;
}
```

### 编写原则

- `StartNewRound` 必须清理上一轮残留状态。
- `CurrentDeclaredRank` 只在本轮第一次出牌时设置。
- 同一轮后续出牌必须沿用同一个声明点数。
- 每次成功出牌后，应该清空之前的不跟记录。
- `LastPlayedCards` 只表示上一名玩家刚出的牌，不表示整个中央牌堆。
- `CenterPile` 属于 `GameState`，不要放进 `RoundState`。

### 验收标准

- 开新轮后，声明点数为空。
- 第一次出牌后，声明点数被设置。
- 后续出牌声明不同点数会报错。
- 成功出牌后，上一手出牌记录正确。
- 成功出牌后，不跟记录被清空。

## 5. GameState.cs

路径：

```text
Client/Assets/Scripts/LocalGame/Core/GameState.cs
```

### 当前职责

`GameState` 保存整局游戏状态。

当前已有：

- `Players`
- `CenterPile`
- `Round`
- `Phase`
- `CurrentPlayerIndex`
- `CurrentPlayer`
- `AddPlayer`

### 建议补充

整局状态需要支持查找玩家、移动当前玩家、记录赢家。

建议新增：

```csharp
public string WinnerPlayerId { get; private set; } = string.Empty;

public bool HasWinner => !string.IsNullOrEmpty(WinnerPlayerId);

public PlayerState FindPlayer(string playerId)
{
    PlayerState player = Players.Find(candidate => candidate.Id == playerId);
    if (player == null)
    {
        throw new System.InvalidOperationException($"Player not found: {playerId}");
    }

    return player;
}

public void MoveToPlayer(string playerId)
{
    int index = Players.FindIndex(player => player.Id == playerId);
    if (index < 0)
    {
        throw new System.InvalidOperationException($"Player not found: {playerId}");
    }

    CurrentPlayerIndex = index;
}

public void MoveToNextPlayer()
{
    if (Players.Count == 0)
    {
        throw new System.InvalidOperationException("No players in game.");
    }

    CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
}

public void SetWinner(string playerId)
{
    FindPlayer(playerId);
    WinnerPlayerId = playerId;
    Phase = GamePhase.GameOver;
}
```

### 编写原则

- `GameState` 负责保存状态，不负责判断动作是否合法。
- 动作是否合法主要由 `LocalGameService` 判断。
- `CurrentPlayer` 在 `Players.Count == 0` 时会越界，所以 `StartGame` 必须先创建玩家再访问。
- `CenterPile` 表示整个中央牌堆。
- `Round.LastPlayedCards` 只表示上一手。

### 验收标准

- 可以根据玩家 ID 找到玩家。
- 可以切换到指定玩家。
- 可以切换到下一个玩家。
- 设置赢家后，阶段进入 `GameOver`。

## 6. DeckManager.cs

路径：

```text
Client/Assets/Scripts/LocalGame/Gameplay/DeckManager.cs
```

### 当前职责

`DeckManager` 负责牌堆相关操作。

当前已有：

- `CreateDeck`
- `Shuffle`
- `DealCards`
- `SortHandCards`

### 建议补充

当前实现已经基本可用，下一步主要补边界检查。

建议修改：

```csharp
public List<Card> CreateDeck(int deckCount = 1, bool includeJokers = true)
{
    if (deckCount <= 0)
    {
        throw new System.ArgumentOutOfRangeException(nameof(deckCount));
    }

    // 保留现有创建逻辑
}

public void DealCards(List<Card> deck, List<PlayerState> players)
{
    if (deck == null || deck.Count == 0)
    {
        throw new System.ArgumentException("Deck cannot be empty.");
    }

    if (players == null || players.Count == 0)
    {
        throw new System.ArgumentException("Players cannot be empty.");
    }

    // 保留现有发牌逻辑
}
```

可选增强：

```csharp
public DeckManager(int? seed = null)
{
    _random = seed.HasValue ? new Random(seed.Value) : new Random();
}
```

如果后续想写稳定测试，可以用固定 seed 让洗牌结果可重复。

### 编写原则

- `DeckManager` 不应该知道游戏阶段。
- `DeckManager` 不应该知道谁是当前玩家。
- 它只负责牌堆创建、洗牌、发牌、排序。
- 不要把出牌、质疑、胜利写进 `DeckManager`。

### 验收标准

- 一副牌带 Joker 时总数为 54。
- 一副牌不带 Joker 时总数为 52。
- 4 人发 54 张牌时，手牌数量应为 14、14、13、13。
- `players.Count == 0` 时不会静默崩溃，而是抛出明确错误。

## 7. LocalGameService.cs

路径：

```text
Client/Assets/Scripts/LocalGame/Gameplay/LocalGameService.cs
```

### 当前职责

`LocalGameService` 是本地游戏流程入口。

当前已有：

- `State`
- `StartGame`
- `CreatePlayers`

### 最终应承担的职责

`LocalGameService` 应该提供这些公共方法：

```csharp
public void StartGame(int playerCount = 4, int deckCount = 1, bool includeJokers = true)
public void PlayCards(List<Card> cards, Rank declaredRank)
public void Pass()
public ChallengeResult Challenge()
public void ConfirmPendingWin()
```

其中：

- `StartGame` 负责初始化。
- `PlayCards` 负责出牌或跟牌。
- `Pass` 负责不跟。
- `Challenge` 负责质疑。
- `ConfirmPendingWin` 负责无人成功质疑后的胜利确认。

### StartGame 应该如何写

建议处理：

```csharp
public void StartGame(int playerCount = 4, int deckCount = 1, bool includeJokers = true)
{
    if (playerCount < 2)
    {
        throw new System.ArgumentOutOfRangeException(nameof(playerCount));
    }

    State = new GameState();
    State.Phase = GamePhase.DealCards;

    CreatePlayers(playerCount);

    List<Card> deck = _deckManager.CreateDeck(deckCount, includeJokers);
    _deckManager.Shuffle(deck);
    _deckManager.DealCards(deck, State.Players);

    State.CurrentPlayerIndex = 0;
    State.Round.StartNewRound(State.CurrentPlayer.Id);
    State.Phase = GamePhase.RoundStart;
}
```

注意点：

- 先创建玩家，再访问 `State.CurrentPlayer`。
- `playerCount` 至少应该是 2。
- `deckCount` 至少应该是 1。
- 发牌阶段可以短暂设置为 `DealCards`，发完后进入 `RoundStart`。

### PlayCards 应该如何写

建议方法：

```csharp
public void PlayCards(List<Card> cards, Rank declaredRank)
```

第一版可以直接传 `Card`。后续 UI 成熟后，可以改成传 `cardIds`。

处理顺序：

1. 确认游戏已开始。
2. 确认当前阶段允许出牌。
3. 确认 `cards` 不为空。
4. 确认 `declaredRank` 不是 `SmallJoker` 或 `BigJoker`。
5. 确认当前玩家拥有这些牌。
6. 如果本轮已有声明点数，确认本次声明点数一致。
7. 从当前玩家手牌移除这些牌。
8. 把这些牌加入 `State.CenterPile`。
9. 调用 `State.Round.RecordPlay(...)`。
10. 如果当前玩家手牌为空，进入待胜利确认。
11. 如果当前玩家还有手牌，切换到下一个玩家。

建议伪代码：

```csharp
public void PlayCards(List<Card> cards, Rank declaredRank)
{
    EnsureGameStarted();
    EnsureCanPlayCards(cards, declaredRank);

    PlayerState player = State.CurrentPlayer;

    player.RemoveCardsFromHand(cards);
    State.CenterPile.AddRange(cards);
    State.Round.RecordPlay(player.Id, declaredRank, cards);

    if (player.HasNoCards)
    {
        State.Round.MarkPendingWinner(player.Id);
        State.Phase = GamePhase.PendingWin;
        State.MoveToNextPlayer();
        return;
    }

    State.Phase = GamePhase.PlayerTurn;
    State.MoveToNextPlayer();
}
```

注意点：

- 出牌成功后必须清空不跟记录，这件事放在 `RoundState.RecordPlay` 里做。
- `LastPlayedCards` 只记录本次出的牌。
- `CenterPile` 累加所有未结算的牌。
- 玩家出完牌不要立刻胜利，应进入 `PendingWin`。

### Pass 应该如何写

建议方法：

```csharp
public void Pass()
```

处理顺序：

1. 确认游戏已开始。
2. 确认当前阶段允许不跟。
3. 确认已经有人出过牌。
4. 确认当前玩家不是上一名出牌者。
5. 记录当前玩家不跟。
6. 如果待胜利确认中，并且所有其他玩家都不质疑，则确认胜利。
7. 如果普通回合中所有其他玩家都不跟，则清空中央牌堆，由上一名出牌者开新轮。
8. 否则切换到下一个可行动玩家。

建议伪代码：

```csharp
public void Pass()
{
    EnsureGameStarted();
    EnsureCanPass();

    string playerId = State.CurrentPlayer.Id;
    State.Round.RecordPass(playerId);

    if (State.Round.HasPendingWinner && DidAllOtherPlayersPass(State.Round.PendingWinnerPlayerId))
    {
        State.SetWinner(State.Round.PendingWinnerPlayerId);
        return;
    }

    if (DidAllOtherPlayersPass(State.Round.LastPlayedPlayerId))
    {
        State.CenterPile.Clear();
        State.MoveToPlayer(State.Round.LastPlayedPlayerId);
        State.Round.StartNewRound(State.CurrentPlayer.Id);
        State.Phase = GamePhase.RoundStart;
        return;
    }

    MoveToNextActionPlayer();
}
```

注意点：

- “所有其他玩家都不跟”里的“其他玩家”是指除上一名出牌者以外的人。
- 不跟不会把牌加入中央牌堆。
- 所有人都不跟后，中央牌堆直接清空，不给任何人。
- 待胜利确认时，不跟可以理解为“不质疑”。

### Challenge 应该如何写

建议方法：

```csharp
public ChallengeResult Challenge()
```

建议结果类型：

```csharp
public class ChallengeResult
{
    public bool IsSuccessful { get; }
    public string ChallengerPlayerId { get; }
    public string ChallengedPlayerId { get; }
    public string PenaltyPlayerId { get; }
    public string NextStarterPlayerId { get; }
    public bool IsGameOver { get; }
}
```

处理顺序：

1. 确认游戏已开始。
2. 确认已经有人出过牌。
3. 当前玩家是质疑者。
4. 上一名出牌者是被质疑者。
5. 检查 `RoundState.LastPlayedCards`。
6. 如果存在非 Joker 且点数不等于声明点数的牌，则质疑成功。
7. 质疑成功时，被质疑者拿走中央牌堆。
8. 质疑失败时，质疑者拿走中央牌堆。
9. 如果被质疑者是待胜利玩家，且质疑失败，则游戏结束。
10. 否则由质疑胜利方开启新一轮。

建议伪代码：

```csharp
public ChallengeResult Challenge()
{
    EnsureGameStarted();
    EnsureCanChallenge();

    PlayerState challenger = State.CurrentPlayer;
    PlayerState challenged = State.FindPlayer(State.Round.LastPlayedPlayerId);
    bool isSuccessful = ContainsLie(State.Round.LastPlayedCards, State.Round.CurrentDeclaredRank.Value);

    PlayerState penaltyPlayer = isSuccessful ? challenged : challenger;
    PlayerState nextStarter = isSuccessful ? challenger : challenged;

    bool isPendingWinnerChallenge = State.Round.PendingWinnerPlayerId == challenged.Id;
    bool isGameOver = isPendingWinnerChallenge && !isSuccessful;

    if (isGameOver)
    {
        State.SetWinner(challenged.Id);
    }
    else
    {
        penaltyPlayer.AddCardsToHand(State.CenterPile);
        State.CenterPile.Clear();
        State.MoveToPlayer(nextStarter.Id);
        State.Round.StartNewRound(nextStarter.Id);
        State.Phase = GamePhase.RoundStart;
    }

    return new ChallengeResult(...);
}
```

注意点：

- 质疑只检查上一手牌，不检查整个中央牌堆。
- Joker 永远视为符合声明点数。
- 质疑成功和失败都要清空中央牌堆。
- 质疑结算后，本轮结束。

### ConfirmPendingWin 应该如何写

建议方法：

```csharp
public void ConfirmPendingWin()
```

这个方法可以先不做成公开按钮逻辑，而是在 `Pass()` 中自动调用。

规则：

- 只有 `RoundState.HasPendingWinner` 时才能确认。
- 如果所有其他玩家都放弃质疑，则待胜利玩家获胜。
- 设置 `WinnerPlayerId`。
- 进入 `GameOver`。

### LocalGameService 私有辅助方法

建议新增：

```csharp
private void EnsureGameStarted()
private void EnsureCanPlayCards(List<Card> cards, Rank declaredRank)
private void EnsureCanPass()
private void EnsureCanChallenge()
private bool IsDeclarableRank(Rank rank)
private bool ContainsLie(List<Card> playedCards, Rank declaredRank)
private bool DidAllOtherPlayersPass(string excludedPlayerId)
private void MoveToNextActionPlayer()
```

这些方法可以让公共方法更短、更容易调试。

### 状态流转建议

```text
NotStarted
  -> StartGame
DealCards
  -> RoundStart
RoundStart
  -> PlayCards
PlayerTurn
  -> PlayCards
  -> Pass
  -> Challenge
PendingWin
  -> Pass
  -> Challenge
RoundClear
  -> RoundStart
GameOver
```

第一版不用把每个阶段都用得非常细，但不要让 `GameOver` 后还能继续出牌。

### 验收标准

- `StartGame` 后玩家和手牌数量正确。
- `PlayCards` 后玩家手牌减少、中央牌堆增加。
- `Pass` 后不跟记录增加。
- 所有人不跟后中央牌堆清空，并开启新一轮。
- `Challenge` 成功时出牌者收走中央牌堆。
- `Challenge` 失败时质疑者收走中央牌堆。
- 玩家出完牌后会进入待胜利确认。
- 胜利确认后进入 `GameOver`。

## 8. GameBootstrap.cs

路径：

```text
Client/Assets/Scripts/App/GameBootstrap.cs
```

### 当前职责

Unity 启动时创建本地游戏服务，并调用 `StartGame()`。

### 建议补充

给调试留出 Inspector 参数。

建议字段：

```csharp
[SerializeField] private int playerCount = 4;
[SerializeField] private int deckCount = 1;
[SerializeField] private bool includeJokers = true;
[SerializeField] private bool printDebugState = true;
```

建议方法：

```csharp
private void Start()
{
    _localGameService = new LocalGameService();
    _localGameService.StartGame(playerCount, deckCount, includeJokers);

    if (printDebugState)
    {
        PrintGameState();
    }
}

private void PrintGameState()
{
    GameState state = _localGameService.State;

    Debug.Log($"Phase: {state.Phase}");
    Debug.Log($"CurrentPlayer: {state.CurrentPlayer.Id}");
    Debug.Log($"CenterPile: {state.CenterPile.Count}");

    foreach (PlayerState player in state.Players)
    {
        Debug.Log($"{player.Id}: {player.HandCards.Count} cards");
    }
}
```

### 编写原则

- `GameBootstrap` 可以 `Debug.Log`。
- `GameBootstrap` 不要判断质疑成功与否。
- `GameBootstrap` 不要直接改玩家手牌。
- 后续 UI 完成后，`GameBootstrap` 只负责装配服务和 UI。

### 验收标准

- 进入 Play Mode 后 Console 能看到玩家手牌数量。
- 修改 Inspector 的玩家数量后，发牌结果随之改变。
- 规则逻辑仍然在 `LocalGameService` 中。

## 9. 暂时不需要手写的文件

### `.meta` 文件

Unity 自动生成，不手动编辑。

### Scene 文件

路径：

```text
Client/Assets/Scenes/SampleScene.unity
```

当前只需要确保场景里有一个挂载 `GameBootstrap` 的 GameObject。

暂时不要在 Scene 里做复杂 UI。规则没闭环前，场景越简单越好。

### Presentation、Application、OnlineGame、RLRuntime

这些目录先保留，不急着写。

当前阶段只要 LocalGame 能用代码跑通就够了。

## 10. 推荐开发顺序

建议按下面顺序提交，每一步都能独立验证：

1. 完善 `Card.cs`、`PlayerState.cs`、`RoundState.cs`、`GameState.cs` 的状态操作。
2. 给 `DeckManager.cs` 补边界检查。
3. 在 `LocalGameService.cs` 实现 `PlayCards`。
4. 在 `LocalGameService.cs` 实现 `Pass`。
5. 在 `LocalGameService.cs` 实现 `Challenge`。
6. 在 `LocalGameService.cs` 实现胜利确认。
7. 在 `GameBootstrap.cs` 添加调试输出。

每一步完成后运行：

```powershell
dotnet build Client\Assembly-CSharp.csproj
```

然后检查：

```powershell
git status --short
```

如果 Unity 自动修改了 `SampleScene.unity` 或 `ProjectSettings`，确认它们是否属于本次改动，再决定是否一起提交。

## 11. 第一版不要追求完美

第一版 LocalGame 的目标是规则可跑通，不是架构一步到位。

可以接受：

- 方法稍微长一点。
- 结果类先写在 `LocalGameService.cs` 同文件里。
- 用 `Debug.Log` 做临时调试。
- 用 `List<Card>` 作为出牌参数。

暂时避免：

- 抽象太多接口。
- 一开始就做事件系统。
- 一开始就做命令模式。
- 一开始就接 UI。
- 一开始就接 Server。

等本地规则跑通后，再根据真实重复点拆分文件，会更稳。
