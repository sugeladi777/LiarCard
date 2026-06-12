namespace LiarCard.LocalGame.Core
{

    public enum Suit
    {
        Spade,
        Heart,
        Club,
        Diamond,
        Joker
    }

    public enum Rank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        SmallJoker = 14,
        BigJoker = 15
    }

    public class Card
    {   
        public int Id { get; private set; }
        public Suit Suit { get; private set; }
        public Rank Rank { get; private set; }

        public Card(int id, Suit suit, Rank rank)
        {
            this.Id = id;
            this.Suit = suit;
            this.Rank = rank;
        }
    }
}