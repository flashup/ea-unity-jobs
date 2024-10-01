using System;
using System.Collections.Generic;
using System.Linq;

namespace Helpers
{
	public static class Extensions
	{
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			for (int i = 0; i < source.Count(); i++)
				action(source.ElementAt(i));
		}

		public static void ForEachIndex<T>(this IEnumerable<T> source, Action<T, int> action)
		{
			for (int i = 0; i < source.Count(); i++)
				action(source.ElementAt(i), i);
		}

		public static IEnumerable<T> Slice<T>(this IEnumerable<T> source, int start, int end = 0)
		{
			if (end == 0) end = source.Count();
			
			if (end < 0) end = source.Count() + end;
			
			var len = end - start;

			var res = new T[len];
			for (int i = 0; i < len; i++)
				res[i] = source.ElementAt(i + start);

			return res;
		}

		public static string Join<T>(this IEnumerable<T> source, string delimiter = ",")
		{
			return string.Join(delimiter, source);
		}
	}
}