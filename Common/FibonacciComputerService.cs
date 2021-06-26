using System;
using System.Collections.Concurrent;

namespace Common
{
    public class FibonacciComputerService
    {
        private ConcurrentDictionary<int, int> cashedResults = new();

        public FibonacciComputerService()
        {
            cashedResults[0] = 0;
            cashedResults[1] = 1;
        }

        private void Set(FibonnaciValue value)
        {
            cashedResults.AddOrUpdate(value.n, value.value, (k, v) => v);
        }

        public FibonnaciValue ComputeNext(FibonnaciValue current)
        {
            Set(current);
            if (cashedResults.TryGetValue(current.n + 1, out var next))
            {
                return new FibonnaciValue(current.n + 1, next);
            }
            else if (cashedResults.TryGetValue(current.n - 1, out var prev))
            {
                var nextValue = prev + current.value;
                cashedResults.AddOrUpdate(current.n + 1, nextValue, (k, v) => v);
                return new FibonnaciValue(current.n + 1, nextValue);
            }
            else
            {
                throw new InvalidOperationException("Invalid FibonacciComputerService state!");
            }
        }
    }
}
