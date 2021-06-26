using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EasyNetQ;
using Common;
using System.Net.Http.Json;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Client
{
    /// <summary>
    /// Need to quickly disable all debug WriteLine statements.
    /// </summary>
    class Log
    {
        public static void Debug(string s)
        {
            //Console.WriteLine(s);
        }

        public static void Info(string s)
        {
            Console.WriteLine(s);
        }
    }

    class RemoteFibonacciComputer
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<FibonnaciValue>> pendingRequests = new();
        
        private readonly HttpClient client = new HttpClient() { 
            BaseAddress = new Uri("http://localhost:5000") 
        };

        public void Recieve(FibonnaciValue result)
        {
            if (pendingRequests.TryRemove(result.id, out var tcs))
            {
                Log.Debug($"Receiving {result}");
                tcs.SetResult(result);
            }
            else
            {
                // Just discard unexpected message.
                Log.Debug($"Discarding unexpected {result}");
            }
        }

        public Task<FibonnaciValue> RequestNext(FibonnaciValue current) => 
            RequestNext(current, CancellationToken.None);

        public async Task<FibonnaciValue> RequestNext(
            FibonnaciValue current, CancellationToken cancel)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "fibonacci") 
            { 
                Content = JsonContent.Create(current)
            };

            var tcs = new TaskCompletionSource<FibonnaciValue>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            
            cancel.Register(() => tcs.TrySetCanceled(cancel));

            if (pendingRequests.TryAdd(current.id, tcs))
            {
                var response = await client.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception($"Unexpected status code {response.StatusCode}");

                var result = await tcs.Task;
                return result;
            }
            else
            {
                throw new Exception($"Can't add tcs for {(current.id, current.n)}");
            }
        }
    }

    class Program
    {
        const bool stopOnOverflow = true;

        static async Task Main(string[] args)
        {
            var parrallelCalculationsCount = Convert.ToInt32(args[0]);

            var remoteFib = new RemoteFibonacciComputer();
            var localFib = new FibonacciComputer();

            // Different subscription ID's for different processes needed because of RabbitMQ
            // distributes queue consumers with equal ID's. For calculation algorithm needs, copy
            // of each message need to be received by each app instance.
            var subscriptionId = $"fib_{Environment.ProcessId}_{Environment.MachineName}";

            using var bus = RabbitHutch.CreateBus("host=localhost;username=guest;password=guest");
            await bus.PubSub.SubscribeAsync<FibonnaciValue>(subscriptionId, remoteFib.Recieve);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var tasks = Enumerable
                .Range(0, parrallelCalculationsCount)
                // Actually, running task int thread pool is not necessary here, because 
                // most execution time spend on awaiting single network connection, so scheduling
                // tasks in thread pool introduces cost without profit.
                .Select(jobNumber => Task.Run(() => Run(jobNumber, remoteFib, localFib)))
                .ToArray();

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Log.Info($"All done in {stopwatch.Elapsed}");
        }

        static async Task Run(int jobNumber, RemoteFibonacciComputer remoteFib, FibonacciComputer localFib)
        {
            Log.Info($"Job {jobNumber} strted in thread {Thread.CurrentThread.ManagedThreadId}");
            var value = new FibonnaciValue(Guid.NewGuid(), 1, 1);

            while (!stopOnOverflow || !(stopOnOverflow && value.value < 0))
            {
                try
                {
                    var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    value = await remoteFib.RequestNext(value, tcs.Token);
                }
                catch (Exception e)
                {
                    // Manually dropping stack to compact output.
                    throw e;
                }
                Log.Debug($"{jobNumber} - Remote: fib({value.n}) = {value.value}");
                value = localFib.ComputeNext(value);
                Log.Debug($"{jobNumber} - Local: fib({value.n}) = {value.value}");
                value = value with { id = Guid.NewGuid() };
            }
            Log.Info($"Job {jobNumber} finished in thread {Thread.CurrentThread.ManagedThreadId}");
        }
    }
}
