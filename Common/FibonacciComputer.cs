using System;
using System.Collections.Concurrent;

namespace Common
{
    /// <summary>
    /// Represent algorithm of computing next Fibonacci number.
    /// </summary>
    public class FibonacciComputer
    {
        private ConcurrentDictionary<int, int> cashedResults = new();

        public FibonacciComputer()
        {
            cashedResults[0] = 0;
            cashedResults[1] = 1;
        }

        /// <summary>
        /// Computes next Fibonacci number by sum <paramref name="current"/> with previous number.
        /// </summary>
        /// <param name="current"></param>
        /// <returns>Next Fibonacci number</returns>
        public FibonnaciValue ComputeNext(FibonnaciValue current)
        {
            cashedResults.AddOrUpdate(current.n, current.value, (k, v) => v);

            if (cashedResults.TryGetValue(current.n + 1, out var next))
            {
                return new FibonnaciValue(current.id, current.n + 1, next);
            }
            else if (cashedResults.TryGetValue(current.n - 1, out var prev))
            {
                var nextValue = prev + current.value;
                cashedResults.AddOrUpdate(current.n + 1, nextValue, (k, v) => v);
                return new FibonnaciValue(current.id, current.n + 1, nextValue);
            }
            else
            {
                throw new InvalidOperationException($"Invalid {nameof(FibonacciComputer)} state for '{current}'!");
            }
        }
    }
}
