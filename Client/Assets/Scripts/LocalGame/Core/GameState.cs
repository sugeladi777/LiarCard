using System.Collections.Generic;

namespace LiarCard.LocalGame.Core
{

    public enum GamePhase
    {
        NotStarted,
        DealCards,
        RoundStart,
        PlayerTurn,
        WaitForChallenge,
        ResolveChallenge,
        RoundClear,
        PendingWin,
        GameOver
    }

    public class GameState
    {
        public List<PlayerState> Players { get;} = new List<PlayerState>();
        public List<Card> CenterPile {get;} = new List<Card>();
        public RoundState Round {get;} = new RoundState();
        public GamePhase Phase { get; set; } = GamePhase.NotStarted;
        public int CurrentPlayerIndex { get; set; }
        public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];


        public void AddPlayer(PlayerState playerState)
        {
            Players.Add(playerState);
        }
    }
}