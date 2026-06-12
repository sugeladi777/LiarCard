using UnityEngine;
using LiarCard.LocalGame.Core;
using System.Collections.Generic;

namespace LiarCard.LocalGame.Gameplay
{
    public class LocalGameService
    {
        public GameState State { get; private set; }
        private readonly DeckManager _deckManager = new DeckManager();

        public void StartGame(int playerCount = 4, int deckCount = 1, bool includeJokers = true)
        {
            State = new GameState();
            // 创建玩家
            CreatePlayers(playerCount);
            // 创建牌堆
            List<Card> deck = _deckManager.CreateDeck(deckCount, includeJokers);
            // 洗牌
            _deckManager.Shuffle(deck);
            // 发牌
            _deckManager.DealCards(deck, State.Players);
            //设置初始出牌人
            State.CurrentPlayerIndex = 0;
            // 设置游戏状态
            State.Phase = GamePhase.RoundStart;
            // 初始化回合状态
            State.Round.StartNewRound(State.CurrentPlayer.Id);

        }

        private void CreatePlayers(int playerCount)
        {
            for (int i = 0; i < playerCount; i++)
            {
                State.AddPlayer(new PlayerState($"Player {i + 1}"));
            }
        }
    }
}