using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VtuberData.Extensions
{
    internal static class MathExtension
    {
        internal static long Median<TSource>(this IEnumerable<TSource> items, Func<TSource, long> selector)
        {
            if (!items.Any())
                return 0;

            var order = items
                .Select(selector)
                .OrderBy(it => it)
                .ToList();

            var mindex = 0;
            var count = order.Count();

            if (count % 2 == 0)
            {
                mindex = count / 2;
                return (order[mindex - 1] + order[mindex]) / 2;
            }

            mindex = (count + 1) / 2;
            return order[mindex - 1];
        }
    }
}
