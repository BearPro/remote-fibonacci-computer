using System;

namespace Common
{
    /// <remarks>
    /// Параметр ID передаётся для сопоставления ответа с запросом.
    /// </remarks>
    public record FibonnaciValue(Guid id, int n, int value);
}
