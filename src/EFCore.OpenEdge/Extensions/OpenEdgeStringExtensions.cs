using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkCore.OpenEdge.Extensions
{
    public static class OpenEdgeStringExtensions
    {
        public static StringBuilder AppendJoin<T, TParam>(
            this StringBuilder stringBuilder,
            IEnumerable<T> values,
            TParam param,
            Action<StringBuilder, T, TParam> joinAction,
            string separator = ", ")
        {
            var appended = false;

            foreach (var value in values)
            {
                joinAction(stringBuilder, value, param);
                stringBuilder.Append(separator);
                appended = true;
            }

            if (appended)
            {
                stringBuilder.Length -= separator.Length;
            }

            return stringBuilder;
        }
    }
}