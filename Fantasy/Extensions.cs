using System.Collections.Generic;
using System.Linq;

namespace Fantasy
{
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
