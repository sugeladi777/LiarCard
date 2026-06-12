using System;
using System.Collections.Generic;
using LiarCard.LocalGame.Core;

namespace LiarCard.LocalGame.Gameplay
{
    public class DeckManager
    {
        private readonly Random _random = new Random();

        public List<Card> CreateDeck(int deckCount = 1, bool includeJokers = true)
        {
            List<Card> deck = new List<Card>();
            int id = 0;

            for (int deckIndex = 0; deckIndex < deckCount; deckIndex++)
            {
                foreach (Suit suit in new[] { Suit.Spade, Suit.Heart, Suit.Club, Suit.Diamond })
                {
                    for (Rank rankValue = Rank.Ace; rankValue <= Rank.King; rankValue++)
                    {
                        deck.Add(new Card(id++, suit, rankValue));
                    }
                }

                if (includeJokers)
                {
                    deck.Add(new Card(id++, Suit.Joker, Rank.SmallJoker));
                    deck.Add(new Card(id++, Suit.Joker, Rank.BigJoker));
                }
            }
            return deck;
        }

        public void Shuffle(List<Card> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }

        public void DealCards(List<Card> deck, List<PlayerState> players)
        {
            int playerIndex = 0;
            int playerCount = players.Count;

            foreach(Card card in deck)
            {
                players[playerIndex].AddCardToHand(card);
                playerIndex = (playerIndex + 1) % playerCount;
            }

            foreach (var player in players)
            {
                SortHandCards(player.HandCards);
            }
        }

        private void SortHandCards(List<Card> handCards)
        {
            handCards.Sort((a, b) =>
            {
                int rankComparison = a.Rank.CompareTo(b.Rank);
                if (rankComparison != 0) return rankComparison;
                return a.Suit.CompareTo(b.Suit);
            });
        }
    }
}