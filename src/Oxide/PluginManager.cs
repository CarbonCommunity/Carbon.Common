/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

public delegate void PluginEvent(Plugin plugin);

public class PluginManager
{
	public string ConfigPath => Defines.GetConfigsFolder();

	public event PluginEvent OnPluginAdded;
	public event PluginEvent OnPluginRemoved;

	public bool AddPlugin(RustPlugin plugin)
	{
		OnPluginAdded?.Invoke(plugin);

		if (!Community.Runtime.Plugins.Plugins.Contains(plugin))
		{
			Community.Runtime.Plugins.Plugins.Add(plugin);
			return true;
		}

		return false;
	}
	public bool RemovePlugin(RustPlugin plugin)
	{
		OnPluginRemoved?.Invoke(plugin);

		if (Community.Runtime.Plugins.Plugins.Contains(plugin))
		{
			Community.Runtime.Plugins.Plugins.Remove(plugin);
			return true;
		}

		return false;
	}

	public Plugin GetPlugin(string name)
	{
		if (name == "RustCore") return Community.Runtime.CorePlugin;

		return Community.Runtime.Plugins.Plugins.FirstOrDefault(x => x.Name == name);
	}
	public IEnumerable<Plugin> GetPlugins()
	{
		return Community.Runtime.Plugins.Plugins.AsEnumerable();
	}
}
