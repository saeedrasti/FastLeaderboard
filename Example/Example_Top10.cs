using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tetra.FastLeaderboard;

namespace Example
{
    public class Example_Top10
    {
        public void Run()
        {
            Leaderboard leaderboard = new Leaderboard();

            //Init leader board ... 
            leaderboard.AddOrUpdate("Alex_id", 2);
            leaderboard.AddOrUpdate("John_id", 22);
            leaderboard.AddOrUpdate("Daniel_id", 4);
            leaderboard.AddOrUpdate("Jackson_id", 34);

            leaderboard.Init();

            //Added after init when user change score
            leaderboard.AddOrUpdate("John_id", 36);

            //When user get leaderboard top 10
            foreach (var row in leaderboard.GetTops(10))
            {
                Console.WriteLine("UserID:{0} Score :{1} Rank :{2} Delta :{3}", row.AccountId, row.Score, row.Rank, row.Delta);
            }

            //Output :
            //UserID: John_id       Score :36   Rank: 1     Delta:1
            //UserID: Jackson_id    Score :34   Rank: 2     Delta: 0
            //UserID: Daniel_id     Score :4    Rank: 3     Delta: 0
            //UserID: Alex_id       Score :2    Rank: 4     Delta: 0
        }
    }
}
