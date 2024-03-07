using Facepunch;

namespace Carbon.Components;

public partial struct Analytics
{
	public static void on_server_startup()
	{
		if (!Enabled)
		{
			return;
		}

		Singleton.
			Include("carbon", $"{Community.Runtime.Analytics.Version}/{Community.Runtime.Analytics.Platform}/{Community.Runtime.Analytics.Protocol}").
			Include("carbon_informational", Community.Runtime.Analytics.InformationalVersion).
			Include("carbon_build", $"{Build.Git.Author} on {Build.Git.Branch} [{Build.Git.HashLong}]").
			Include("rust", $"{BuildInfo.Current.Build.Number}/{Rust.Protocol.printable}").
			Submit("on_server_startup");

	}
	public static void on_server_initialized()
	{
		if (!Enabled)
		{
			return;
		}

		Singleton.
			Include("plugin_count", ModLoader.LoadedPackages.Sum(x => x.Plugins.Count)).
			Include("plugins_totalmemoryused", $"{ByteEx.Format(ModLoader.LoadedPackages.Sum(x => x.Plugins.Sum(y => y.TotalMemoryUsed)), valueFormat: "0", stringFormat: "{0}{1}").ToLower()}").
			Include("plugins_totalhooktime", $"{ModLoader.LoadedPackages.Sum(x => x.Plugins.Sum(y => y.TotalHookTime)).RoundUpToNearestCount(100):0}ms").
			Include("extension_count", Community.Runtime.AssemblyEx.Extensions.Loaded.Count).
			Include("module_count", Community.Runtime.AssemblyEx.Modules.Loaded.Count).
			Include("hook_count", Community.Runtime.HookManager.LoadedDynamicHooks.Count(x => x.IsInstalled) + Community.Runtime.HookManager.LoadedStaticHooks.Count(x => x.IsInstalled)).
			Submit("on_server_initialized");
	}
	public static void plugin_constructor_failure(RustPlugin plugin)
	{
		if (!Enabled)
		{
			return;
		}

		Singleton.
			Include("plugin", $"{plugin.Name} v{plugin.Version} by {plugin.Author}")
			.Submit("plugin_constructor_failure");
	}
	public static void batch_plugin_types()
	{
		if (!Enabled)
		{
			return;
		}

		var rustPluginCount = 0;
		var covalencePluginCount = 0;
		var carbonPluginCount = 0;

		foreach (var plugin in ModLoader.LoadedPackages.SelectMany(package => package.Plugins))
		{
			if (plugin.Type.BaseType == typeof(CovalencePlugin)) covalencePluginCount++;
			else if (plugin.Type.BaseType == typeof(RustPlugin)) rustPluginCount++;
			else if (plugin.Type.BaseType == typeof(CarbonPlugin)) carbonPluginCount++;
		}

		Singleton.
			Include("rustplugin", $"{rustPluginCount:n0}").
			Include("covalenceplugin", $"{covalencePluginCount:n0}").
			Include("carbonplugin", $"{carbonPluginCount:n0}").
			Submit("batch_plugin_types");
	}
	public static void o_command_attempt(string command, ConsoleSystem.Option option)
	{
		if (!Enabled)
		{
			return;
		}

		Singleton.
			Include("command", command).
			Include("from_player", option.Connection?.player != null).
			Include("from_server", option.FromRcon).
			Submit("o_command_attempt");
	}
	public static void plugin_time_warn(string readableHook, Plugin basePlugin, double afterHookTime, double totalMemory, BaseHookable.CachedHook cachedHook, BaseHookable hookable)
	{
		if (!Enabled)
		{
			return;
		}

		Singleton.
			Include("name", $"{readableHook} ({basePlugin.ToPrettyString()})").
			Include("time", $"{afterHookTime.RoundUpToNearestCount(50)}ms").
			Include("memory", $"{ByteEx.Format(totalMemory, shortName: true).ToLower()}").
			Include("fires", $"{cachedHook.TimesFired}").
			Include("hasgc", hookable.HasGCCollected).
			Submit("plugin_time_warn");
	}
	public static void plugin_native_compile_fail(ISource initialSource, Exception ex)
	{
		if (!Enabled)
		{
			return;
		}

		Singleton.
			Include("file", $"{initialSource.ContextFilePath}").
			Include("stacktrace", $"({ex.Message}) {ex.StackTrace}").
			Submit("plugin_native_compile_fail");
	}
	public static void carbon_client_init()
	{
		if (!Enabled)
		{
			return;
		}

		var clientConfig = Community.Runtime.ClientConfig;

		Singleton.
			Include("nomap", clientConfig.Environment.NoMap).
			Include("oldrecoil", clientConfig.Client.UseOldRecoil).
			Include("clientgravity", clientConfig.Client.ClientGravity.ToString("0.0")).
			Submit("carbon_client_init");
	}
	public static void admin_module_wizard(WizardProgress progress)
	{
		if (!Enabled)
		{
			return;
		}

		Singleton.
			Include("walkthrough", progress == WizardProgress.Walkthrough).
			Include("skipped", progress == WizardProgress.Skipped).
			Submit("admin_module_wizard");
	}

	public enum WizardProgress
	{
		Walkthrough,
		Skipped
	}
}
