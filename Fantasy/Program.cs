using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TableParser;

namespace Fantasy
{
    class Program
    {
        static string host = @"https://fantasy.premierleague.com/";
        static string standings = @"drf/leagues-classic-standings/313?phase=1&le-page=1&ls-page={0}";
        static string statics = @"drf/bootstrap-static";

        static string teamPicks = @"drf/entry/{0}/event/{1}/picks";

        private static List<string> TopTeams = new List<string>();
        private static Dictionary<string, JToken> Players = new Dictionary<string, JToken>();
        private static string CurrentGameweekId;
        private static object _lock = new object();

        private static Dictionary<string, int> MostUsedPlayers = new Dictionary<string, int>();

        static async Task Main(string[] args)
        {
            await Get(statics, async (result) =>
            {
                var response = JObject.Parse(await result.Content.ReadAsStringAsync());
                JArray items = (JArray)response["elements"];
                foreach (var item in items)
                {
                    Players.Add(item["id"].ToString(), item);
                    //Console.WriteLine($"{ item["id"] }: { item["first_name"] } { item["second_name"] }");
                }

                JArray events = (JArray)response["events"];
                foreach (var item in events)
                {
                    if (item.Value<bool>("is_current"))
                    {
                        CurrentGameweekId = item.Value<string>("id");
                        break;
                    }
                }
            });

            await Get(string.Format(standings, 1), async (result) =>
            {
                var response = JObject.Parse(await result.Content.ReadAsStringAsync());
                JArray items = (JArray)response["standings"]["results"];
                foreach (var item in items)
                {
                    TopTeams.Add(item["entry"].ToString());
                    //Console.WriteLine($"{ item["entry"] }: { item["total"] }");
                }
            });

            await Get(string.Format(standings, 2), async (result) =>
            {
                var response = JObject.Parse(await result.Content.ReadAsStringAsync());
                JArray items = (JArray)response["standings"]["results"];
                foreach (var item in items)
                {
                    TopTeams.Add(item["entry"].ToString());
                    //Console.WriteLine($"{ item["entry"] }: { item["total"] }");
                }
            });

            foreach (var team in TopTeams)
            {
                await Get(string.Format(teamPicks, team, CurrentGameweekId), async (result) =>
                {
                    var response = JObject.Parse(await result.Content.ReadAsStringAsync());
                    JArray items = (JArray)response["picks"];
                    foreach (var item in items)
                    {
                        var playerId = item.Value<string>("element");
                        lock (_lock)
                        {
                            if (MostUsedPlayers.ContainsKey(playerId))
                            {
                                MostUsedPlayers[playerId]++;
                            }
                            else
                            {
                                MostUsedPlayers.Add(playerId, 1);
                            }
                        }
                    }
                });
            }

            List<Tuple<JToken, string>> output = new List<Tuple<JToken, string>>();

            foreach (var player in MostUsedPlayers.OrderByDescending(x => x.Value))
            {
                output.Add(new Tuple<JToken, string>(Players[player.Key], player.Value.ToString()));
            }

            Console.WriteLine(output.ToStringTable(
                new[] { "First name", "Second Name", "Pos", "Used", "TP", "Form", "GS", "GC", "Ass", "Min", "CP(this)", "CP(next)", "Sel %", "Cost" },
                 a => a.Item1["first_name"],
                 a => a.Item1["second_name"],
                 a => {
                     switch (a.Item1["element_type"].ToString())
                     {
                         case "1":
                             return "GK";
                         case "2":
                             return "DEF";
                         case "3":
                             return "MID";
                         case "4":
                             return "FWD";
                         default:
                             return "UNK";
                     }
                 },
                 a => a.Item2,
                 a => a.Item1["total_points"],
                 a => a.Item1["form"],
                 a => a.Item1["goals_scored"],
                 a => a.Item1["goals_conceded"],
                 a => a.Item1["assists"],
                 a => a.Item1["minutes"],
                 a => a.Item1["chance_of_playing_this_round"],
                 a => a.Item1["chance_of_playing_next_round"],
                 a => a.Item1["selected_by_percent"],
                 a =>
                 {
                     var cost = a.Item1["now_cost"].ToString();
                     var lastIndex = cost.Length - 1;
                     cost = cost.Insert(lastIndex, ".");
                     return cost;
                 }

                 ));

            Console.ReadKey();
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
}
