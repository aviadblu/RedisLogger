using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace RedisLoggerExample
{
    public class Tests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public Tests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1()
        {
            using (var redis = ConnectionMultiplexer.Connect("localhost"))
            {
                var db = redis.GetDatabase();
                db.StringSet("mykey", "some value");
                var value = db.StringGet("mykey");
                Assert.Equal(value, "some value");
            }
        }

        [Fact]
        public async void LogWrite()
        {
            RedisLogger.Init();
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                await RedisLogger.WriteLogMessage(54, $"[{i}] my special message");
            }

            stopwatch.Stop();
            _testOutputHelper.WriteLine(stopwatch.ElapsedMilliseconds.ToString());
        }

        [Fact]
        public void GetAllKeysById_WriteToFile()
        {
            RedisLogger.Init();
            var key = 54;
            var logs = RedisLogger.GetAllLogs(key);
            using (var writetext = new StreamWriter("log_54.txt"))
            {
                logs.ForEach(log =>
                {
                    var keyparts = log.Key.Split(new[] {"_"}, StringSplitOptions.None);
                    writetext.WriteLine($"[{new DateTime(long.Parse(keyparts[1]))}][{keyparts[3]}] {log.Value}");
                });
            }
        }

        [Fact]
        public void WriteAllLogsToFile()
        {
            RedisLogger.Init();
            var logs = RedisLogger.GetAllLogs();
            using (var writetext = new StreamWriter("all_logs_output.txt"))
            {
                logs.ForEach(log =>
                {
                    var keyparts = log.Key.Split(new[] {"_"}, StringSplitOptions.None);
                    writetext.WriteLine($"[{new DateTime(long.Parse(keyparts[1]))}][{keyparts[3]}] {log.Value}");
                });
            }
        }

        [Fact]
        public void DeleteAllKeysById()
        {
            RedisLogger.Init();
            var key = 54;
            RedisLogger.DeleteLogByActionId(key);
        }
    }
}