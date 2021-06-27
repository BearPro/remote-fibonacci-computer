using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ;
using Common;
using System.Threading;
using System.Diagnostics;

namespace Client
{

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
