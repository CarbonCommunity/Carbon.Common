/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public class Suggestions
{
	public static BufferBank Buffer = new();

	public static SuggestionResult Lookup(string input, IEnumerable<string> options, int threshold = -1)
	{
		var minDistance = int.MaxValue;
		var closestMatch = string.Empty;

		foreach (string option in options)
		{
			var distance = Compute(input, option);

			if (distance >= minDistance) continue;
			minDistance = distance;
			closestMatch = option;
		}

		if (threshold != -1 && minDistance > threshold)
		{
			return default;
		}

		SuggestionResult suggestionResult = default;
		suggestionResult.Result = closestMatch;
		suggestionResult.Confidence = minDistance;

		return suggestionResult;
	}

	public class BufferBank : List<BufferInstance>
	{
		public int[,] Get(int x, int y)
		{
			var result = this.FirstOrDefault(instance => instance.X == x && instance.Y == y);

			if (result.Value == null)
			{
				BufferInstance buffer = default;
				buffer.X = x;
				buffer.Y = y;
				buffer.Value = new int[x, y];
				this.Add(buffer);

				return buffer.Value;
			}

			for (int xx = 0; xx < x; xx++)
			{
				for (int yy = 0; yy < y; yy++)
				{
					result.Value[xx, yy] = default;
				}
			}

			return result.Value;
		}
	}
	public struct BufferInstance
	{
		public int X;
		public int Y;
		public int[,] Value;
	}
	public struct SuggestionResult
	{
		public string Result;
		public int Confidence;
	}

	/*
	 * Copyright (c) 2024 ChatGPT 3.5
	 */
	internal static int Compute(string s, string t)
	{
		int n = s.Length;
		int m = t.Length;
		int[,] d = Buffer.Get(n + 1, m + 1);

		if (n == 0)
		{
			return m;
		}

		if (m == 0)
		{
			return n;
		}

		for (int x = 0; x <= n; d[x, 0] = x++) ;
		for (int y = 0; y <= m; d[0, y] = y++) ;

		for (int x = 1; x <= n; x++)
		{
			for (int y = 1; y <= m; y++)
			{
				int cost = (t[y - 1] == s[x - 1]) ? 0 : 1;
				d[x, y] = Math.Min(Math.Min(d[x - 1, y] + 1, d[x, y - 1] + 1), d[x - 1, y - 1] + cost);
			}
		}

		return d[n, m];
	}
}
