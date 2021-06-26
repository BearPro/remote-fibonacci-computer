using System;

namespace Common
{
    /// <summary>
    /// Представляет результат вычисления числа Фибоначчи под номер n.
    /// </summary>
    /// <remarks>
    /// Параметр передаётся для сопоставления ответа с запросом.
    /// </remarks>
    public record FibonnaciValue(int n, int value);
}
