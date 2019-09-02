using Newtonsoft.Json.Linq;

namespace Fantasy
{
    public class Player
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public Position Position { get; set; }
        public string Team { get; set; }
        public int Used { get; set; }
        public double TotalPoints { get; set; }
        public string Form { get; set; }
        public string GoalsScored { get; set; }
        public string GoalsConceded { get; set; }
        public string Assists { get; set; }
        public string MinutesPlayed { get; set; }
        public string ChancePlayingThis { get; set; }
        public string ChancePlayingNext { get; set; }
        public string Selected { get; set; }
        public double Cost { get; set; }
        public double Weight { get; set; }

        public string Info => $"{FirstName} {SecondName} ({Team}, {Position.ToString()})";
    }
}
