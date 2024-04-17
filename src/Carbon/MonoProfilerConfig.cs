using UnityEngine.Serialization;

namespace Carbon.Profiler;

[Serializable]
public class MonoProfilerConfig
{
	public bool Enabled = false;
	public bool Allocations = false;
	public List<string> ProfiledAssemblies = new();
	public List<string> ProfiledPlugins = new();

	public bool AddAssembly(string assembly)
	{
		if (ProfiledAssemblies.Contains(assembly))
		{
			return false;
		}

		ProfiledAssemblies.Add(assembly);
		return true;
	}
	public bool RemoveAssembly(string assembly)
	{
		if (!ProfiledAssemblies.Contains(assembly))
		{
			return false;
		}

		ProfiledAssemblies.Remove(assembly);
		return true;
	}

    public bool AddPlugin(string plugin)
    {
        if (ProfiledPlugins.Contains(plugin))
        {
            return false;
        }

        ProfiledPlugins.Add(plugin);
        return true;
    }
    public bool RemovePlugin(string plugin)
    {
        if (!ProfiledPlugins.Contains(plugin))
        {
            return false;
        }

        ProfiledPlugins.Remove(plugin);
        return true;
    }

	public bool IsPluginWhitelisted(string assembly)
	{
		return ProfiledPlugins.Contains("*") || ProfiledPlugins.Contains(assembly);
	}
}
