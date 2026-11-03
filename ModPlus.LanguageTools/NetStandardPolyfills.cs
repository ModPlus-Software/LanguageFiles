// Полифилы для сборки под netstandard2.0 (расширение ModPlusVSTools работает на .NET Framework).
// В net8.0 эти типы есть в самой платформе, поэтому код компилируется только для netstandard2.0.
#if NETSTANDARD2_0

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Нужен компилятору для init-аксессоров и позиционных record
    /// </summary>
    internal static class IsExternalInit
    {
    }
}

namespace System.Collections.Generic
{
    /// <summary>
    /// Деконструкция пары словаря в <c>foreach (var (key, value) in dictionary)</c>
    /// </summary>
    internal static class KeyValuePairDeconstruct
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}

#endif
