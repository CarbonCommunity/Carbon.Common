namespace Carbon;

public class Assemblies
{
	public static RuntimeAssemblyBank Plugins { get; } = new();
	public static RuntimeAssemblyBank Modules { get; } = new();
	public static RuntimeAssemblyBank Extensions { get; } = new();

	public class RuntimeAssembly
	{
		public Assembly CurrentAssembly { get; internal set; }

		public List<Assembly> History { get; } = new();
	}

	public class RuntimeAssemblyBank : Dictionary<string, RuntimeAssembly>
	{
		public void Inject(string key, Assembly assembly)
		{
			if (!TryGetValue(key, out var existent))
			{
				existent = new RuntimeAssembly();
				existent.CurrentAssembly = assembly;
				Add(key, existent);
			}
			else if (existent.CurrentAssembly != null)
			{
				existent.History.Add(existent.CurrentAssembly);
				existent.CurrentAssembly = assembly;
			}
		}
		public void Eject(string key)
		{
			if (!TryGetValue(key, out var existent) || existent.CurrentAssembly == null)
			{
				return;
			}

			existent.History.Add(existent.CurrentAssembly);
			existent.CurrentAssembly = null;
		}
	}
}
