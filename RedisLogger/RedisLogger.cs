using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisLogger
{
    public static class RedisLogger
    {
        private static ConnectionMultiplexer _redisClient;
        private const string RedisHost = "localhost";
        private const int RedisPort = 6379;

        public static void Init()
        {
            _redisClient = ConnectionMultiplexer.Connect($"{RedisHost}:{RedisPort},allowAdmin=true");
        }

        private static readonly Random Random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        public static async Task WriteLogMessage(int id, string msg)
        {
            await Task.Run(() =>
            {
                long ticks = DateTime.Now.Ticks;
                var key = $"pddrlog_{ticks}_{RandomString(5)}_{id}";
                var db = _redisClient.GetDatabase();
                db.StringSet(key, msg);
            });
        }

        public static List<KeyValuePair<string, string>> GetAllLogs(int id = -1)
        {
            var server = _redisClient.GetServer(RedisHost, RedisPort);
            var db = _redisClient.GetDatabase();
            IEnumerable<RedisKey> keys;
            if (id == -1)
            {
                keys = server.Keys();
            }
            else
            {
                keys = server.Keys(pattern: $"*_{id}");
            }

            string[] keysArr = keys.Select(key => (string) key).ToArray();
            var results = new List<KeyValuePair<string, string>>();
            foreach (string key in keysArr)
            {
                results.Add(new KeyValuePair<string, string>(key, db.StringGet(key)));
            }

            return results.OrderBy(x => x.Key).ToList();
        }

        public static void DeleteLogByActionId(int id)
        {
            var server = _redisClient.GetServer("localhost", 6379);
            var db = _redisClient.GetDatabase();
            var keys = server.Keys(pattern: $"*_{id}");
            string[] keysArr = keys.Select(key => (string) key).ToArray();
            foreach (string key in keysArr)
            {
                db.KeyDelete(key);
            }
        }

        public static void Dispose()
        {
            _redisClient.Dispose();
        }
    }
}