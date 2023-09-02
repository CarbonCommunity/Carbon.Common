namespace Carbon;

using System;
using System.Linq;

public class Build
{
	public class Git
	{
		static Git()
		{
			try
			{
				var changes = @"M Carbon.Core/Carbon.Core.sln";
				var newlineSplit = new string[] { "\n" };
				var spaceSplit = new char[] { ' ' };
				var lines = changes.Split(newlineSplit, StringSplitOptions.RemoveEmptyEntries);
				var temp = new List<AssetChange>();

				foreach(var line in lines)
				{
					var lineSplit = line.Split(spaceSplit);
					var type = lineSplit[0];
					var path = lineSplit.Skip(1).ToString(" ");
					var changeType = (AssetChange.ChangeTypes)default;

					switch(type)
					{
						case "A":
							changeType = AssetChange.ChangeTypes.Added;
							break;
						case "M":
						case "C":
							changeType = AssetChange.ChangeTypes.Modified;
							break;
						case "D":
							changeType = AssetChange.ChangeTypes.Deleted;
							break;
					}

					temp.Add(new AssetChange(path, changeType));

					Array.Clear(lineSplit, 0, lineSplit.Length);
				}

				Changes = temp.ToArray();

				Array.Clear(lines, 0, lines.Length);
				Array.Clear(newlineSplit, 0, newlineSplit.Length);
				Array.Clear(spaceSplit, 0, spaceSplit.Length);

				temp.Clear();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed processing build commit file changelog.", ex);
			}
		}

		public static readonly string Branch = @"rust_beta/staging";

		public static readonly string Author = @"raul";
		public static readonly string Comment = @"SLN++";
		public static readonly string Date = @"2023-09-02 00:15:43 +0200";

		public static readonly string HashShort = @"52dc130a";
		public static readonly string HashLong = @"52dc130a6811be511e7a9586473e9ee3abd0d1dc";

		public static readonly string Url = @"https://github.com/CarbonCommunity/Carbon.Core.git/commit/52dc130a6811be511e7a9586473e9ee3abd0d1dc";		

		public static readonly AssetChange[] Changes;

		public struct AssetChange
		{
			public string Path { get; private set; }
			public ChangeTypes Type { get; private set; }

			internal AssetChange(string path, ChangeTypes type)
			{
				Path = path;
				Type = type;
			}

			public enum ChangeTypes
			{
				Added,
				Modified,
				Deleted
			}
		}
	}
}
