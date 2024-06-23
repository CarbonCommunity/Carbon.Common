namespace Carbon;

public class Assemblies
{
	public static RuntimeAssemblyBank Plugins { get; } = new();
	public static RuntimeAssemblyBank Modules { get; } = new();
	public static RuntimeAssemblyBank Extensions { get; } = new();
	public static RuntimeAssemblyBank Harmony { get; } = new();

	public class RuntimeAssembly
	{
		public Assembly CurrentAssembly { get; internal set; }
		public string Location { get; internal set; }

		public List<Assembly> History { get; } = new();
	}

	public class RuntimeAssemblyBank : Dictionary<string, RuntimeAssembly>
	{
		public RuntimeAssembly Get(string key)
		{
			TryGetValue(key, out var existent);
			return existent;
		}
		public void Update(string key, Assembly assembly, string location)
		{
			if (string.IsNullOrEmpty(key))
			{
				Logger.Warn($"RuntimeAssemblyBank.Update key == null");
				return;
			}

			if (assembly == null)
			{
				Logger.Warn($"RuntimeAssemblyBank.Update assembly == null");
				return;
			}

			if (!TryGetValue(key, out var existent))
			{
				existent = new RuntimeAssembly();
				existent.CurrentAssembly = assembly;
				Add(key, existent);
			}
			else
			{
				if (existent.CurrentAssembly != null)
				{
					existent.History.Add(existent.CurrentAssembly);
				}
				
				existent.CurrentAssembly = assembly;
			}

			existent.Location = location;
		}
		public void Eliminate(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				Logger.Warn($"RuntimeAssemblyBank.Eliminate key == null");
				return;
			}

			if (!TryGetValue(key, out var existent) || existent.CurrentAssembly == null)
			{
				Logger.Warn($"RuntimeAssemblyBank.Eliminate: No key with '{key}'");
				return;
			}

			existent.History.Add(existent.CurrentAssembly);
			existent.CurrentAssembly = null;
		}
	}
}
