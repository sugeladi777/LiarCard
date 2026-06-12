using System.Collections.Generic;

namespace LiarCard.LocalGame.Core
{
    public class RoundState
    {
        public string StarterPlayerId { get; private set; } = string.Empty;
        public Rank? CurrentDeclaredRank {get;private set;}
        public string LastPlayedPlayerId {get;private set;} = string.Empty;
        public List<Card> LastPlayedCards {get;} = new List<Card>();
        public HashSet<string> PassedPlayerIds {get;} = new HashSet<string>();
        public string PendingWinnerPlayerId {get;set;} = string.Empty;

        public void StartNewRound(string _starterPlayerId)
        {
            StarterPlayerId = _starterPlayerId;
            CurrentDeclaredRank = null;
            LastPlayedPlayerId = string.Empty;
            LastPlayedCards.Clear();
            PassedPlayerIds.Clear();
            PendingWinnerPlayerId = string.Empty;
        }

    }
}