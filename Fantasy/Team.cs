using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fantasy
{
    public class Team
    {
        public Tuple<int, int, int> Formation = new Tuple<int, int, int>(4, 5, 1);
        public Player Gk { get; set; }

        public Player SubGk { get; set; }

        public List<Player> Defenders { get; set; } = new List<Player>();
        public List<Player> SubDefenders { get; set; } = new List<Player>();

        public List<Player> Midfielders { get; set; } = new List<Player>();
        public List<Player> SubMidfielders { get; set; } = new List<Player>();

        public List<Player> Forwards { get; set; } = new List<Player>();
        public List<Player> SubForwards { get; set; } = new List<Player>();

        public List<Player> AllPlayers
        {
            get
            {
                var toReturn = new List<Player>();
                toReturn.AddRange(Defenders);
                toReturn.AddRange(SubDefenders);

                toReturn.AddRange(Midfielders);
                toReturn.AddRange(SubMidfielders);

                toReturn.AddRange(Forwards);
                toReturn.AddRange(SubForwards);
                toReturn.Add(Gk);
                toReturn.Add(SubGk);

                return toReturn;
            }
        }

        public List<Player> FirstTeamPlayers
        {
            get
            {
                var toReturn = new List<Player>();
                toReturn.AddRange(Defenders);
                toReturn.AddRange(Midfielders);
                toReturn.AddRange(Forwards);
                toReturn.Add(Gk);

                return toReturn;
            }
        }

        public double FirstTeamCost => Defenders.Sum(x => x.Cost) + Midfielders.Sum(x => x.Cost) + Forwards.Sum(x => x.Cost) + Gk.Cost;

        public double SubstitutesCost => SubDefenders.Sum(x => x.Cost) + SubMidfielders.Sum(x => x.Cost) + SubForwards.Sum(x => x.Cost) + SubGk.Cost;

        public bool IsClubRuleOk => AllPlayers.GroupBy(x => x.Team).All(x => x.Count() <= 3);

        public double TotalTeamCost => AllPlayers.Sum(x => x.Cost);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Gk.Info);
            sb.AppendLine(string.Join(", ", Defenders.Select(x => x.Info)));
            sb.AppendLine(string.Join(", ", Midfielders.Select(x => x.Info)));
            sb.AppendLine(string.Join(", ", Forwards.Select(x => x.Info)));

            sb.AppendLine("--------------------------");

            sb.AppendLine(SubGk.Info);
            sb.AppendLine(string.Join(", ", SubDefenders.Select(x => x.Info)));
            sb.AppendLine(string.Join(", ", SubMidfielders.Select(x => x.Info)));
            sb.AppendLine(string.Join(", ", SubForwards.Select(x => x.Info)));

            return sb.ToString();
        }

    }
}
