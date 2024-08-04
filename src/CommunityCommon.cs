using API.Analytics;
using API.Assembly;
using API.Commands;
using API.Contracts;
using API.Events;
using API.Hooks;
using Facepunch;
using Newtonsoft.Json;
using Application = UnityEngine.Application;
using Carbon.Profiler;

namespace Carbon;

public class Community
{
	public static Community Runtime
	{ get; set; }

	public static GameObject GameObject
	{ get => _gameObject.Value; }

	private static readonly Lazy<GameObject> _gameObject = new(() =>
	{
		GameObject gameObject = GameObject.Find("Carbon");
		return gameObject == null ? throw new Exception("Carbon GameObject not found") : gameObject;
	});

	public IAnalyticsManager Analytics
	{ get => _analyticsManager.Value; }

	public IAssemblyManager AssemblyEx
	{ get => _assemblyEx.Value; }

	public ICommandManager CommandManager
	{ get => _commandManager.Value; }

	public IDownloadManager Downloader
	{ get => _downloadManager.Value; }

	public IEventManager Events
	{ get => _eventManager.Value; }

	public ICompatManager Compat
	{ get => _compatManager.Value; }

	private readonly Lazy<IAnalyticsManager> _analyticsManager
		= new(GameObject.GetComponent<IAnalyticsManager>);

	private readonly Lazy<IAssemblyManager> _assemblyEx
		= new(GameObject.GetComponent<IAssemblyManager>);

	private readonly Lazy<ICommandManager> _commandManager
		= new(GameObject.GetComponent<ICommandManager>);

	private readonly Lazy<IDownloadManager> _downloadManager
		= new(GameObject.GetComponent<IDownloadManager>);

	private readonly Lazy<IEventManager> _eventManager
		= new(GameObject.GetComponent<IEventManager>);

	private readonly Lazy<ICompatManager> _compatManager
		= new(GameObject.GetComponent<ICompatManager>);

#if EXPERIMENTAL
	public IThreadManager Threads
	{ get => _threadManager.Value; }

	private readonly Lazy<IThreadManager> _threadManager
		= new(GameObject.GetComponent<IThreadManager>);
#endif

	public IPatchManager HookManager
	{ get; set; }

	public IScriptProcessor ScriptProcessor
	{ get; set; }

	public IModuleProcessor ModuleProcessor
	{ get; set; }

	public IZipScriptProcessor ZipScriptProcessor
	{ get; set; }

#if DEBUG
	public IZipDevScriptProcessor ZipDevScriptProcessor
	{ get; set; }
#endif

	public ICarbonProcessor CarbonProcessor
	{ get; set; }

	public static bool IsServerInitialized { get; internal set; }

	public static bool IsConfigReady => Runtime != null && Runtime.Config != null;

	public static bool AllProcessorsFinalized => Runtime.ScriptProcessor.AllPendingScriptsComplete() &&
	                                             Runtime.ZipScriptProcessor.AllPendingScriptsComplete()
#if !MINIMAL && DEBUG
	                                             && Runtime.ZipDevScriptProcessor.AllPendingScriptsComplete()
#endif
	                                             ;

	public Config Config
	{ get; set; }

	public Carbon.Profiler.MonoProfilerConfig MonoProfilerConfig
	{ get; set; }

	public CorePlugin Core
	{ get; set; }

	public ModLoader.Package Plugins
	{ get; set; }

	public ModLoader.Package ZipPlugins
	{ get; set; }

	public Entities Entities
	{ get; set; }

	internal static string _runtimeId;

	public static string RuntimeId
	{
		get
		{
			if (string.IsNullOrEmpty(_runtimeId))
			{
				var date = DateTime.Now;
				_runtimeId = date.Year.ToString() + date.Month + date.Day +
				             date.Hour + date.Minute + date.Second + date.Millisecond;

			}

			return _runtimeId;
		}
	}

	public static string Protect(string name)
	{
		if (string.IsNullOrEmpty(name)) return string.Empty;

		using var split = TempArray<string>.New(name.Split(' '));
		var command = split.Array[0];
		var arguments = split.Array.Skip(1).ToString(" ");

		return $"carbonprotecc_{RandomEx.GetRandomString(command.Length, command + RuntimeId, command.Length)} {arguments}".TrimEnd();
	}

	public void MarkServerInitialized(bool wants, bool hookCall = true)
	{
		IsServerInitialized = wants;
	}

	public Community()
	{
		try
		{
			Events.Subscribe(CarbonEvent.CarbonStartup, args =>
			{
				Logger.Log($"Carbon fingerprint: {Analytics.ClientID}");
				Logger.Log($"System fingerprint: {Analytics.SystemID}");
				Analytics.SessionStart();
			});

			Events.Subscribe(CarbonEvent.CarbonStartupComplete, args =>
			{
				Components.Analytics.on_server_startup();
			});

			var newlineSplit = new char[] { '\n' };

			Application.logMessageReceived += (string condition, string stackTrace, LogType type) =>
			{
				switch (type)
				{
					case LogType.Error:
					case LogType.Exception:
					case LogType.Assert:
						if (!string.IsNullOrEmpty(condition) &&
						(condition.StartsWith("Null") || condition.StartsWith("Index")))
						{
							var trace = stackTrace.Split(newlineSplit, StringSplitOptions.RemoveEmptyEntries);
							var resultTrace = string.Empty;

							for (int i = 0; i < trace.Length; i++)
							{
								var t = trace[i];
								if (string.IsNullOrEmpty(t)) continue;

								resultTrace += $"  at {t}\n";
							}

							Array.Clear(trace, 0, trace.Length);

							resultTrace = resultTrace.TrimEnd();
							Logger.Write(API.Logger.Severity.Error, $"Unhandled error occurred ({condition})\n{resultTrace}", nativeLog: false);
							Console.WriteLine(resultTrace);
						}
						break;
				}
			};
		}
		catch (Exception ex)
		{
			Logger.Error("Critical error", ex);
		}
	}

	public void ClearCommands(bool all = false)
	{
		CommandManager.ClearCommands(command => all || command.Reference is RustPlugin plugin && !plugin.IsCorePlugin);
	}

	#region Config

	public void LoadConfig(bool postProcess = true)
	{
		var needsSave = false;

		if (!OsEx.File.Exists(Defines.GetConfigFile()))
		{
			needsSave = true;
			Config ??= new();
		}
		else
		{
			Config = JsonConvert.DeserializeObject<Config>(OsEx.File.ReadText(Defines.GetConfigFile()));
		}

		if (postProcess)
		{
			if (Config.Compiler.ConditionalCompilationSymbols == null)
			{
				Config.Compiler.ConditionalCompilationSymbols = new();
				needsSave = true;
			}

			if (string.IsNullOrEmpty(Config.Permissions.AdminDefaultGroup))
				Config.Permissions.AdminDefaultGroup = "admin";

			if (string.IsNullOrEmpty(Config.Permissions.PlayerDefaultGroup))
				Config.Permissions.PlayerDefaultGroup = "default";

			if (!Config.Compiler.ConditionalCompilationSymbols.Contains("CARBON"))
				Config.Compiler.ConditionalCompilationSymbols.Add("CARBON");

			if (!Config.Compiler.ConditionalCompilationSymbols.Contains("RUST"))
				Config.Compiler.ConditionalCompilationSymbols.Add("RUST");

			Config.Compiler.ConditionalCompilationSymbols =
				Config.Compiler.ConditionalCompilationSymbols.Distinct().ToList();

			if (Config.Prefixes == null)
			{
				Config.Prefixes = new();
				needsSave = true;
			}

			if (Config.Aliases == null)
			{
				Config.Aliases = new();
				needsSave = true;
			}
			else
			{
				var invalidAliases = Pool.GetList<string>();
				invalidAliases.AddRange(from alias in Config.Aliases
					where !Config.IsValidAlias(alias.Key, out _)
					select alias.Key);

				foreach (var invalidAlias in invalidAliases)
				{
					Config.Aliases.Remove(invalidAlias);
					Logger.Warn($" Removed invalid alias: {invalidAlias}");
				}

				if (invalidAliases.Count > 0)
				{
					needsSave = true;
				}

				Pool.FreeList(ref invalidAliases);
			}

			if (Config.Prefixes.Count == 0)
			{
				Config.Prefixes.Add(new()
				{
					Value = "/", PrintToChat = false, PrintToConsole = false, SuggestionAuthLevel = 2
				});
			}

			if (Config.Aliases.Count == 0)
			{
				Config.Aliases["carbon"] = "c.version";
				Config.Aliases["harmony.load"] = "c.harmonyload";
				Config.Aliases["harmony.unload"] = "c.harmonyunload";
			}

			// Mandatory for across the board access
			API.Commands.Command.Prefixes = Config.Prefixes;

			Logger.CoreLog.SplitSize = (int)(Config.Logging.LogSplitSize * 1000000f);

			if (needsSave) SaveConfig();
		}

		if (Config.Analytics.Enabled)
		{
			Logger.Warn($"Carbon Analytics are ON. They're entirely anonymous and help us to further improve.");
		}
		else
		{
			Logger.Error($"Carbon Analytics are OFF.");
		}
	}

	public void LoadMonoProfilerConfig()
	{
		var needsSave = false;

		if (!OsEx.File.Exists(Defines.GetMonoProfilerConfigFile()))
		{
			MonoProfilerConfig ??= new();
			needsSave = true;
		}
		else
		{
			MonoProfilerConfig = JsonConvert.DeserializeObject<MonoProfilerConfig>(OsEx.File.ReadText(Defines.GetMonoProfilerConfigFile()));
		}

		if(needsSave) SaveMonoProfilerConfig();
	}

	public void SaveConfig()
	{
		if (Config == null) Config = new Config();

		OsEx.File.Create(Defines.GetConfigFile(), JsonConvert.SerializeObject(Config, Formatting.Indented));
	}

	public void SaveMonoProfilerConfig()
	{
		MonoProfilerConfig ??= new();

		OsEx.File.Create(Defines.GetMonoProfilerConfigFile(), JsonConvert.SerializeObject(MonoProfilerConfig, Formatting.Indented));
	}

	#endregion

	#region Plugins

	public static bool InitialPluginLoad { get; internal set; }

	public virtual void ReloadPlugins(IEnumerable<string> except = null)
	{
		InitialPluginLoad = true;

		ModLoader.IsBatchComplete = false;
		ModLoader.ClearAllErrored();
		ModLoader.ClearAllRequirees();
	}
	public void ClearPlugins(bool full = false)
	{
		Runtime.ClearCommands(full);
		ModLoader.UnloadCarbonMods();
	}

	#endregion

	public void RefreshConsoleInfo()
	{
#if WIN
		if (!IsConfigReady || !Config.Misc.ShowConsoleInfo) return;

		if (!IsServerInitialized) return;
		if (ServerConsole.Instance.input.statusText.Length != 4) ServerConsole.Instance.input.statusText = new string[4];

		var version =
#if DEBUG
			Analytics.InformationalVersion;
#else
            Analytics.Version;
#endif

		ServerConsole.Instance.input.statusText[3] = $" Carbon" +
#if MINIMAL
			$" Minimal" +
#endif
			$" v{version}, {ModLoader.Packages.Count:n0} mods, {ModLoader.Packages.Sum(x => x.Plugins.Count):n0} plgs, {ModuleProcessor.Modules.Count(x => x is BaseModule module && module.IsEnabled()):n0}/{ModuleProcessor.Modules.Count:n0} mdls, {AssemblyEx.Extensions.Loaded.Count:n0} exts";
#endif
	}

	#region Logging

	public static void LogCommand(object message, BasePlayer player = null)
	{
		if (player == null)
		{
			Carbon.Logger.Log(message);
			return;
		}

		player.SendConsoleCommand($"echo {message}");
	}

	#endregion

	public virtual void Initialize()
	{

	}
	public virtual void Uninitialize()
	{
		Runtime = null;
	}
}
