/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using Object = System.Object;

namespace Carbon.Extensions;

public static class EnumerableEx
{
	public static int IndexOf<T>(this IEnumerable<T> enumerable, T value)
	{
		if (value == null)
		{
			return default;
		}

		var index = 0;

		foreach (var iteration in enumerable)
		{
			if (iteration.Equals(value))
			{
				return index;
			}

			index++;
		}

		return index;
	}
	public static int FindIndex<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
	{
		var index = 0;

		if (predicate == null)
		{
			foreach (var iteration in enumerable)
			{
				index++;
			}
		}
		else
		{
			foreach (var iteration in enumerable)
			{
				if (predicate(iteration))
				{
					index++;
				}
			}
		}

		return index;
	}
	public static T FindAt<T>(this IEnumerable<T> enumerable, int index)
	{
		var currentIndex = 0;

		foreach (var iteration in enumerable)
		{
			if (currentIndex == index)
			{
				return iteration;
			}

			currentIndex++;
		}

		return default;
	}
}
