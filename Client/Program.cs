using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EasyNetQ;
using MqMessages;

namespace Client
{
    class Fibonacci
    {
        private LinkedList<(int n, TaskCompletionSource<FibonnaciValue> tcs)> pendingRequests = new();

        public void Recieve(FibonnaciValue result)
        {
            // Console.WriteLine($"Recieved fib({result.n}) -> {result.value}");
            var (n, tcs) = pendingRequests.FirstOrDefault(x => x.n == result.n);
            if (tcs != null)
            {
                tcs.SetResult(result);
            }
            else
            {
                System.Console.WriteLine($"Unexpected result for n={result.n}");
            }

        }

        public async Task<int> Request(int n)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"fibonacci?n={n}");
            
            var tcs = new TaskCompletionSource<FibonnaciValue>();
            pendingRequests.AddLast((n, tcs));

            var uri = new Uri("http://localhost:5000");
            using var client = new HttpClient() { BaseAddress = uri };
            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Unexpected status code {response.StatusCode}");
            }
            
            // Console.WriteLine(await response.Content.ReadAsStringAsync());

            var result = await tcs.Task;
            return result.value;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            using var bus = RabbitHutch.CreateBus("host=localhost;username=guest;password=guest");
            
            var fib = new Fibonacci();
            
            bus.PubSub.Subscribe<FibonnaciValue>("fibbonachy_values", fib.Recieve);
            
            var task1 = fib.Request(10);
            var task2 = fib.Request(20);
            await Task.WhenAll(task1, task2);
            Console.WriteLine($"fib(10) = {task1.Result}, fib(20) = {task2.Result}");
            Console.WriteLine("All done");
        }
    }
}
