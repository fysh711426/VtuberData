using System;
using System.Collections.Generic;

namespace VtuberData.Extensions
{
    internal static class PipeExtension
    {
        internal static TResult Pipe<TSource, TResult>(this TSource source, Func<TSource, TResult> selector)
        {
            return selector(source);
        }

        internal static IEnumerable<TResult> Pipes<TSource, TResult>(this IEnumerable<TSource> sources, Func<TSource, TResult> selector)
        {
            foreach (var source in sources)
            {
                yield return selector(source);
            }
        }
    }
}
