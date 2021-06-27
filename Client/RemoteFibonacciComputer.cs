using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using System.Net.Http.Json;
using System.Threading;
using System.Collections.Concurrent;

namespace Client
{
    /// <summary>
    /// Provides methods for computing next Fibonacci sequence number using remote app.
    /// </summary>
    class RemoteFibonacciComputer
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<FibonnaciValue>> pendingRequests = new();
        
        private readonly HttpClient client = new HttpClient() { 
            BaseAddress = new Uri("http://localhost:5000") 
        };

        /// <summary>
        /// Used as callback for rabbitmq messages.
        /// </summary>
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

        /// <summary>
        /// Starts task for requesting next Fibonacci number from app 2 (Server) by http and 
        /// waiting for this number transfered by rabbitmq.
        /// </summary>
        /// <param name="current">Current</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>Task representing this operations</returns>
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
}
