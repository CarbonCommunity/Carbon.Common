namespace Oxide.Core.Extensions;

public class ExtensionManager
{
	private List<PluginLoader> pluginloaders = new();

	public IEnumerable<PluginLoader> GetPluginLoaders() => pluginloaders;
	public void RegisterPluginLoader(Oxide.Core.Plugins.PluginLoader loader)
	{
		pluginloaders.Add(loader);
	}
}
