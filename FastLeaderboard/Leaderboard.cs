using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
namespace Tetra.FastLeaderboard
{
    public class Leaderboard
    {
        private readonly ConcurrentDictionary<string, LblItem> id_data_dic = new ConcurrentDictionary<string, LblItem>();
        private readonly SortedList<LeaderboardKey, string> sortList = new SortedList<LeaderboardKey, string>();
        private bool isInit;
        private readonly object sync = new object();

        private readonly List<LeaderboardKey> preUpdate = new List<LeaderboardKey>();
        public int Count
        {
            get
            {
                lock (sync)
                    return id_data_dic.Count;
            }
        }

        public void Init()
        {
            if (!isInit)
            {

                lock (sync)
                {
                    isInit = true;

                    foreach (var i in preUpdate.AsParallel().OrderBy(i => i))
                    {
                        AddOrUpdate(i.AccountId, i.Score, i.Payload);
                    }

                    preUpdate.Clear();
                    preUpdate.TrimExcess();
                }
            }
        }


        public void Clear()
        {
            lock (sync)
            {
                isInit = false;
                sortList.Clear();
                sortList.TrimExcess();
                id_data_dic.Clear();
                preUpdate.Clear();
                preUpdate.TrimExcess();
            }
        }

        public void AddOrUpdate(string AccountId, float Score, int DeltaRank = 0)
        {
            lock (sync)
            {
                if (isInit)
                {
                    if (id_data_dic.TryGetValue(AccountId, out LblItem item))
                    {
                        int prevRank = GetRank(new LeaderboardKey(item.Score, AccountId));


                        sortList.Remove(new LeaderboardKey(item.Score, AccountId));

                        item.Score = Score;

                        sortList.Add(new LeaderboardKey(Score, AccountId), AccountId);

                        int curRank = GetRank(new LeaderboardKey(item.Score, AccountId));

                        item.DeltaRank = prevRank - curRank;
                    }
                    else
                    {
                        item = new LblItem()
                        {
                            AccountId = AccountId,
                            Score = Score
                        };

                        id_data_dic.TryAdd(AccountId, item);

                        sortList.Add(new LeaderboardKey(Score, AccountId), AccountId);

                        item.DeltaRank = DeltaRank;
                    }
                }
                else
                {
                    preUpdate.Add(new LeaderboardKey(Score, AccountId, DeltaRank));
                }
            }

        }
        public void Remove(string AccountId)
        {
            lock (sync)
            {
                if (id_data_dic.TryGetValue(AccountId, out LblItem find))
                {
                    id_data_dic.TryRemove(AccountId, out find);
                    sortList.Remove(new LeaderboardKey(find.Score, find.AccountId));
                }
            }
        }
        public List<RowData> GetTops(int Count)
        {
            lock (sync)
            {
                List<RowData> result = new List<RowData>();
                int listCount = sortList.Count;

                for (int index = 0; index < Count; index++)
                {
                    if (index < listCount)
                    {
                        string id = sortList.Values[index];
                        result.Add(GetItemByAccountId(id).GetView());
                    }
                }
                return result;
            }

        }
        public List<RowData> GetNears(string AccountId, int Count)
        {
            List<RowData> result = new List<RowData>();
            lock (sync)
            {

                LblItem playerData = GetItemByAccountId(AccountId);
                if (playerData != null)
                {
                    List<RowData> upList = new List<RowData>();
                    List<RowData> downList = new List<RowData>();

                    int midRank = GetRank(new LeaderboardKey(playerData.Score, playerData.AccountId));

                    #region UP_LIST
                    for (int rank = midRank - Count / 2; rank < midRank - 1; rank++)
                    {
                        if (rank >= 0 && rank < sortList.Count)
                        {
                            string uId = sortList.Values[rank];
                            LblItem item = GetItemByAccountId(uId);
                            if (item != null)
                                upList.Add(item.GetView());
                        }
                    }
                    #endregion
                    #region DOWN_LIST
                    for (int rank = midRank; rank < midRank + Count / 2; rank++)
                    {
                        if (rank >= 0 && rank < sortList.Count)
                        {
                            string uId = sortList.Values[rank];
                            LblItem item = GetItemByAccountId(uId);
                            if (item != null)
                                downList.Add(item.GetView());
                        }
                    }
                    #endregion

                    result.AddRange(upList);
                    result.Add(playerData.GetView());
                    result.AddRange(downList);
                }

            }
            return result;
        }
        public LblItem GetItemByAccountId(string AccountId)
        {
            lock (sync)
            {
                if (id_data_dic.TryGetValue(AccountId, out LblItem find))
                {
                    find.Rank = GetRank(find);
                    return find;
                }
            }
            return null;
        }
        public LblItem GetItemByRank(int Rank)
        {
            lock (sync)
            {
                if (Rank >= 0 && Rank < sortList.Count)
                {
                    string uId = sortList.Values[Rank];
                    return GetItemByAccountId(uId);
                }
            }
            return null;
        }

        private int GetRank(LeaderboardKey key)
        {
            lock (sync)
                return sortList.IndexOfKey(key) + 1;
        }
        private int GetRank(LblItem item)
        {
            lock (sync)
                return sortList.IndexOfKey(new LeaderboardKey(item.Score, item.AccountId)) + 1;
        }
    }
    public class LblItem
    {
        public string AccountId { get; set; }
        public float Score { get; set; }
        public int DeltaRank { get; set; }
        public int Rank { get; set; }
        public RowData GetView()
        {
            return new RowData()
            {
                AccountId = AccountId,
                Rank = Rank,
                Score = Score,
                Delta = DeltaRank
            };
        }
    }
    public struct RowData
    {
        public string AccountId { get; set; }
        public float Score { get; set; }
        public int Delta { get; set; }
        public int Rank { get; set; }
    }
    public struct LeaderboardKey : IComparable<LeaderboardKey>
    {
        public string AccountId;
        public float Score;
        public int Payload;
        public LeaderboardKey(float Score, string AccountId, int Payload = 0)
        {
            this.Score = Score;
            this.AccountId = AccountId;
            this.Payload = Payload;
        }

        public int CompareTo(LeaderboardKey other)
        {
            int res = 0;

            if (Score > other.Score)
                res = -1;
            else
            if (Score == other.Score && AccountId.CompareTo(other.AccountId) < 0)
                res = -1;
            else
            if (Score == other.Score && AccountId == other.AccountId)
                res = 0;
            else
            if (Score == other.Score && AccountId.CompareTo(other.AccountId) > 0)
                res = 1;
            else
            if (Score < other.Score)
                res = 1;

            return res;
        }
    }
}