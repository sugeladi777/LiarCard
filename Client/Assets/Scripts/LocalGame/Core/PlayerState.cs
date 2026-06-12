using System.Collections.Generic;

namespace LiarCard.LocalGame.Core
{
    public class PlayerState
    {
        public string Id { get; set; }
        public List<Card> HandCards { get; set; } = new List<Card>();

        public PlayerState(string id)
        {
            this.Id = id;
        }

        public void AddCardToHand(Card card)
        {
            HandCards.Add(card);
        }
    }
}