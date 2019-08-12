using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TableParser;

namespace Fantasy
{
    public enum Position
    {
        GK,
        DEF,
        MID,
        FWD
    }

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

        public double FirstTeamCost
        {
            get
            {
                return Defenders.Sum(x => x.Cost) + Midfielders.Sum(x => x.Cost) + Forwards.Sum(x => x.Cost) + Gk.Cost;
            }
        }

        public double SubstitutesCost
        {
            get
            {
                return SubDefenders.Sum(x => x.Cost) + SubMidfielders.Sum(x => x.Cost) + SubForwards.Sum(x => x.Cost) + SubGk.Cost;
            }
        }

        public bool IsClubRuleOk
        {
            get
            {
                return AllPlayers.GroupBy(x => x.Team).All(x => x.Count() <= 3);
            }
        }

        public double TotalTeamCost
        {
            get
            {
                return AllPlayers.Sum(x => x.Cost);
            }
        }

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

    public class Player
    {
        public JToken PlayerJson { get; set; }
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

        public string Info
        {
            get
            {
                return $"{FirstName} {SecondName} ({Team}, {Position.ToString()})";
            }
        }
    }

    class Program
    {
        static string host = @"https://fantasy.premierleague.com/";
        //316403 - nasata
        //313 - overall
        //147 - macedonia
        static string standings = @"api/leagues-classic/314/standings/?page_new_entries=1&page_standings={0}&phase=1";
        static string statics = @"api/bootstrap-static";
        static string teamPicks = @"api/entry/{0}/event/{1}/picks";

        static int TotalPlayersConsidered = 50;

        private static List<string> TopUserTeamCodes = new List<string>();
        private static List<Player> MostWeightedPlayers = new List<Player>();

        private static Dictionary<string, JToken> Players = new Dictionary<string, JToken>();
        private static Dictionary<string, JToken> Clubs = new Dictionary<string, JToken>();
        private static Dictionary<string, JToken> Events = new Dictionary<string, JToken>();

        private static string CurrentGameweekId;
        private static object _lock = new object();

        private static Team BestTeam = new Team();

        private static double UsedWeight = 1;
        private static double TotalPointsWeight = 0;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter Used weight (default 1):");
            var entered = Console.ReadLine();
            UsedWeight = double.Parse(string.IsNullOrEmpty(entered) ? "1" : entered);
        
            Console.WriteLine("Enter Total points weight (default 0):");
            entered = Console.ReadLine();
            TotalPointsWeight = double.Parse(string.IsNullOrEmpty(entered) ? "0" : entered);

            Console.WriteLine("Working...");

            await GetStatics();

            await GetStandings();

            await ParseTopPicks();

            Console.Clear();

            PrintTable();

            Console.WriteLine("Try make best team? (Y/N)");
            if (Console.ReadLine().ToLower() == "y")
            {
                try
                {
                    MakeBestTeam();
                }
                catch
                {
                    Console.Write("Cannot make best team");
                }
            }

            Console.ReadKey();
        }

        private static void MakeBestTeam()
        {
            //najdi ja najdobrata formacija spored tabelata

            //Zemi gi najeftinite zameni po 1 za sekoja pozicija (4)
            //odzemi ja nivnata vkupna suma od 100 i raboti so ostatokot za prv tim (11)
            var cheapestFirst = MostWeightedPlayers.OrderBy(x => x.Cost).ToList();

            BestTeam.SubGk = cheapestFirst.First(x => x.Position == Position.GK);
            BestTeam.SubDefenders.AddRange(cheapestFirst.Where(x => x.Position == Position.DEF).Take(5 - BestTeam.Formation.Item1));
            BestTeam.SubMidfielders.AddRange(cheapestFirst.Where(x => x.Position == Position.MID).Take(5 - BestTeam.Formation.Item2));
            BestTeam.SubForwards.AddRange(cheapestFirst.Where(x => x.Position == Position.FWD).Take(3 - BestTeam.Formation.Item3));

            //Zemi gi prvite 11 od tabelata i stavi gi vo sostavot
            var mostUsed = MostWeightedPlayers.ToList();
            BestTeam.Gk = mostUsed.First(x => x.Position == Position.GK);
            BestTeam.Defenders = mostUsed.Where(x => x.Position == Position.DEF).Take(BestTeam.Formation.Item1).ToList();
            BestTeam.Midfielders = mostUsed.Where(x => x.Position == Position.MID).Take(BestTeam.Formation.Item2).ToList();
            BestTeam.Forwards = mostUsed.Where(x => x.Position == Position.FWD).Take(BestTeam.Formation.Item3).ToList();

            var ReplacedPlayers = new List<Player>();

            while (!BestTeam.IsClubRuleOk || BestTeam.TotalTeamCost >= 100)
            {
                var skippedPlayers = new List<Player>();

                while (BestTeam.TotalTeamCost >= 100) //not ok
                {
                    var sub = MostWeightedPlayers.Where(x =>
                                                    !BestTeam.AllPlayers.Select(y => y.Id).Contains(x.Id)
                                                    &&
                                                    !ReplacedPlayers.Select(y => y.Id).Contains(x.Id)
                                                    &&
                                                    BestTeam.FirstTeamPlayers.Count(y => y.Team == x.Team) < 3
                                                    )
                                                    .MostWeight();

                    if (sub == null)
                    {

                    }

                    switch (sub.Position)
                    {
                        case Position.GK:
                            {

                                ReplacedPlayers.Add(BestTeam.Gk);
                                BestTeam.Gk = sub;
                                break;
                            }
                        case Position.DEF:
                            {
                                var toReplace = BestTeam.Defenders.LeastWeight();

                                Console.WriteLine($"Cost substituting {toReplace.Info} with {sub.Info}");
                                var index = BestTeam.Defenders.IndexOf(toReplace);
                                BestTeam.Defenders[index] = sub;
                                ReplacedPlayers.Add(toReplace);
                                break;
                            }
                        case Position.MID:
                            {
                                var toReplace = BestTeam.Midfielders.LeastWeight();

                                Console.WriteLine($"Cost substituting {toReplace.Info} with {sub.Info}");
                                var index = BestTeam.Midfielders.IndexOf(toReplace);
                                BestTeam.Midfielders[index] = sub;
                                ReplacedPlayers.Add(toReplace);
                                break;
                            }
                        case Position.FWD:
                            {
                                var toReplace = BestTeam.Forwards.LeastWeight();

                                Console.WriteLine($"Cost substituting {toReplace.Info} with {sub.Info}");
                                var index = BestTeam.Forwards.IndexOf(toReplace);
                                BestTeam.Forwards[index] = sub;
                                ReplacedPlayers.Add(toReplace);
                                break;
                            }
                    }
                }

                if (!BestTeam.IsClubRuleOk)
                {
                    var toReplace = BestTeam.AllPlayers.GroupBy(x => x.Team).Where(x => x.Count() > 3).First().LeastWeight();
                    ReplacedPlayers.RemoveAll(x => x.Team != toReplace.Team);

                    var sub = MostWeightedPlayers.Where(newSub =>
                                                    newSub.Position == toReplace.Position
                                                    &&
                                                    !BestTeam.AllPlayers.Select(y => y.Id).Contains(newSub.Id)
                                                    &&
                                                    !ReplacedPlayers.Select(y => y.Id).Contains(newSub.Id)
                                                    &&
                                                    BestTeam.FirstTeamPlayers.Count(y => y.Team == newSub.Team) < 3)
                                                    .MostWeight();

                    switch (toReplace.Position)
                    {
                        case Position.GK:
                            {
                                ReplacedPlayers.Add(BestTeam.Gk);
                                BestTeam.Gk = sub;
                                break;
                            }
                        case Position.DEF:
                            {
                                Console.WriteLine($"Same team substituting {toReplace.Info} with {sub.Info}");
                                var index = BestTeam.Defenders.IndexOf(toReplace);
                                BestTeam.Defenders[index] = sub;
                                ReplacedPlayers.Add(toReplace);
                                break;
                            }
                        case Position.MID:
                            {
                                Console.WriteLine($"Same team substituting {toReplace.Info} with {sub.Info}");
                                var index = BestTeam.Midfielders.IndexOf(toReplace);
                                BestTeam.Midfielders[index] = sub;
                                ReplacedPlayers.Add(toReplace);
                                break;
                            }
                        case Position.FWD:
                            {
                                Console.WriteLine($"Same team substituting {toReplace.Info} with {sub.Info}");
                                var index = BestTeam.Forwards.IndexOf(toReplace);
                                BestTeam.Forwards[index] = sub;
                                ReplacedPlayers.Add(toReplace);
                                break;
                            }
                    }
                }
            }

            // -> loop
            //Proveri dali se zadovoleni uslvovite
            // *maks trojca od 1 tim vklucuvajki gi izmenite (ako izmenata e problem vidi dali ima druga izmena so ista suma za ista pozicija)
            // *site pozicii se zadovoleniasdf
            // *Ako nivnata suma gi nadminuva moznite sredstva zemi go naredniot od tabelata i zameni go so nekoj od sostavot (nekoj kako se odlucuva osven po pozicija)
            //ako e vo red break else loop

            Console.WriteLine();
            Console.WriteLine("*****************************");
            Console.WriteLine(BestTeam.ToString());
            Console.WriteLine("*****************************");

            Console.WriteLine("Total team cost {0}", BestTeam.TotalTeamCost);
        }

        private static void PrintTable()
        {
            Console.WriteLine(MostWeightedPlayers.ToStringTable(
                new[] { "First name", "Second Name", "Pos", "Team", "Weight", "Used", "TP", "Form", "GS", "GC", "Ass", "Min", "CP(this)", "CP(next)", "Sel %", "Cost" },
                 p => p.FirstName,
                 p => p.SecondName,
                 p => p.Position.ToString(),
                 p => p.Team,
                 p => p.Weight,
                 p => p.Used,
                 p => p.TotalPoints,
                 p => p.Form,
                 p => p.GoalsScored,
                 p => p.GoalsConceded,
                 p => p.Assists,
                 p => p.MinutesPlayed,
                 p => p.ChancePlayingThis,
                 p => p.ChancePlayingNext,
                 p => p.Selected,
                 p => p.Cost
                 ));
        }

        private static async Task ParseTopPicks()
        {
            foreach (var team in TopUserTeamCodes)
            {
                await Get(string.Format(teamPicks, team, CurrentGameweekId), async (result) =>
                {
                    var response = JObject.Parse(await result.Content.ReadAsStringAsync());
                    JArray items = (JArray)response["picks"];
                    foreach (var item in items)
                    {
                        var playerId = item.Value<string>("element");
                        var player = Players[playerId];
                        lock (_lock)
                        {
                            if (MostWeightedPlayers.Any(x => x.Id == playerId))
                            {
                                MostWeightedPlayers.First(x => x.Id == playerId).Used++;
                            }
                            else
                            {
                                var cost = player["now_cost"].ToString();
                                var lastIndex = cost.Length - 1;
                                cost = cost.Insert(lastIndex, ".");

                                Position pos = Position.GK;
                                switch (player["element_type"].ToString())
                                {
                                    case "1":
                                        pos = Position.GK;
                                        break;
                                    case "2":
                                        pos = Position.DEF;
                                        break;
                                    case "3":
                                        pos = Position.MID;
                                        break;
                                    case "4":
                                        pos = Position.FWD;
                                        break;
                                }
                                var pl = new Player
                                {
                                    Id = playerId,
                                    Used = 1,
                                    FirstName = player["first_name"].ToString(),
                                    SecondName = player["second_name"].ToString(),
                                    TotalPoints = double.Parse(player["total_points"].ToString()),
                                    Form = player["form"].ToString(),
                                    GoalsScored = player["goals_scored"].ToString(),
                                    GoalsConceded = player["goals_conceded"].ToString(),
                                    Assists = player["assists"].ToString(),
                                    MinutesPlayed = player["minutes"].ToString(),
                                    ChancePlayingThis = player["chance_of_playing_this_round"].ToString(),
                                    ChancePlayingNext = player["chance_of_playing_next_round"].ToString(),
                                    Selected = player["selected_by_percent"].ToString(),
                                    Team = Clubs[player["team_code"].ToString()]["short_name"].ToString(),
                                    Cost = double.Parse(cost),
                                    Position = pos,
                                    PlayerJson = player
                                };

                                //if (pl.ChancePlayingNext == "100")
                                //{
                                    MostWeightedPlayers.Add(pl);
                                //}
                            }
                        }
                    }

                });
            }

            //calc weight
            foreach (var player in MostWeightedPlayers)
            {
                //percentage of usage
                var used = player.Used / (double)TotalPlayersConsidered;
               
                var totalPoints = player.TotalPoints / Events.Count;
                player.Weight = Math.Round((used * UsedWeight) + (totalPoints * TotalPointsWeight), 3);
            }

            MostWeightedPlayers = MostWeightedPlayers.OrderByDescending(x => x.Weight).ToList();
        }

        private static async Task GetStandings()
        {
            for (int i = 1; i <= TotalPlayersConsidered / 50; i++)
            {
                await Get(string.Format(standings, i), async (result) =>
                {
                    var response = JObject.Parse(await result.Content.ReadAsStringAsync());
                    JArray items = (JArray)response["standings"]["results"];
                    foreach (var item in items)
                    {
                        TopUserTeamCodes.Add(item["entry"].ToString());
                    }
                });
            }
        }

        private static async Task GetStatics()
        {
            await Get(statics, async (result) =>
            {
                var response = JObject.Parse(await result.Content.ReadAsStringAsync());

                JArray items = (JArray)response["elements"];
                foreach (var item in items)
                {
                    Players.Add(item["id"].ToString(), item);
                }

                JArray events = (JArray)response["events"];
                foreach (var item in events)
                {
                    Events.Add(item.Value<string>("id"), item);
                    if (item.Value<bool>("is_current"))
                    {
                        CurrentGameweekId = item.Value<string>("id");
                        break;
                    }
                }

                JArray clubs = (JArray)response["teams"];
                foreach (var item in clubs)
                {
                    Clubs.Add(item.Value<string>("code"), item);
                }
            });
        }

        private static async Task Get(string path, Action<HttpResponseMessage> action)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(host);
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_4) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.103 Safari/537.36");
                var result = await httpClient.GetAsync(path);
                if (result.IsSuccessStatusCode)
                {
                    action(result);
                }
            }
        }
    }

    public static class Extensions
    {
        public static Player LeastWeight(this IEnumerable<Player> list)
        {
            return list.OrderBy(x => x.Weight).First();
        }

        public static Player MostWeight(this IEnumerable<Player> list)
        {
            return list.OrderByDescending(x => x.Weight).FirstOrDefault();
        }
    }
}
