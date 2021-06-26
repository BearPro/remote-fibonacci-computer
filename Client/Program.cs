using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EasyNetQ;
using Common;
using System.Net.Http.Json;

namespace Client
{
    class FibonacciReciever
    {
        private LinkedList<(int n, TaskCompletionSource<FibonnaciValue> tcs)> pendingRequests = new();

        public void Recieve(FibonnaciValue result)
        {
            var (n, tcs) = pendingRequests.FirstOrDefault(x => x.n == result.n);
            if (tcs != null)
            {
                tcs.SetResult(result);
            }
            else
            {
                Console.WriteLine($"Unexpected result for n={result.n}");
            }

        }

        public async Task<FibonnaciValue> RequestNext(FibonnaciValue current)
        {
            var content = JsonContent.Create(current);
            var request = new HttpRequestMessage(HttpMethod.Post, "fibonacci") { Content = content };

            var tcs = new TaskCompletionSource<FibonnaciValue>();
            pendingRequests.AddLast((current.n + 1, tcs));

            var uri = new Uri("http://localhost:5000");
            using var client = new HttpClient() { BaseAddress = uri };
            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Unexpected status code {response.StatusCode}");
            }
            
            var result = await tcs.Task;
            return result;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var reciever = new FibonacciReciever();
            var computer = new FibonacciComputerService();

            using var bus = RabbitHutch.CreateBus("host=localhost;username=guest;password=guest");
            bus.PubSub.Subscribe<FibonnaciValue>("fibbonachy_values", reciever.Recieve);

            await Run(reciever, computer);

            Console.WriteLine("All done");
        }

        static async Task Run(FibonacciReciever reciever, FibonacciComputerService computer)
        {
            var value = new FibonnaciValue(1, 1);

            while (value.value > 0)
            {
                value = await reciever.RequestNext(value);
                Console.WriteLine($"Remote: fib({value.n}) = {value.value}");
                value = computer.ComputeNext(value);
                Console.WriteLine($"Local: fib({value.n}) = {value.value}");
                // await Task.Delay(500);
            }
        }
    }
}
