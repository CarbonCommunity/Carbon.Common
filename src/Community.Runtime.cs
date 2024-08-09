using API.Analytics;
using API.Assembly;
using API.Commands;
using API.Contracts;
using API.Events;
using API.Hooks;

namespace Carbon;

public partial class Community
{
	public static GameObject GameObject => _gameObject.Value;

	private static readonly Lazy<GameObject> _gameObject = new(() =>
	{
		var gameObject = GameObject.Find("Carbon");
		return gameObject == null ? throw new Exception("Carbon GameObject not found") : gameObject;
	});

	public IAnalyticsManager Analytics => _analyticsManager.Value;
	public IAssemblyManager AssemblyEx => _assemblyEx.Value;
	public ICommandManager CommandManager => _commandManager.Value;
	public IDownloadManager Downloader => _downloadManager.Value;
	public IEventManager Events => _eventManager.Value;
	public ICompatManager Compat => _compatManager.Value;

	public IPatchManager HookManager { get; set; }
	public IScriptProcessor ScriptProcessor { get; set; }
	public IModuleProcessor ModuleProcessor { get; set; }
	public IZipScriptProcessor ZipScriptProcessor { get; set; }

#if DEBUG
	public IZipDevScriptProcessor ZipDevScriptProcessor { get; set; }
#endif

	public ICarbonProcessor CarbonProcessor { get; set; }

	public static bool IsServerInitialized { get; internal set; }
	public static bool IsConfigReady => Runtime != null && Runtime.Config != null;
	public static bool AllProcessorsFinalized => Runtime.ScriptProcessor.AllPendingScriptsComplete() &&
												 Runtime.ZipScriptProcessor.AllPendingScriptsComplete()
#if !MINIMAL && DEBUG
												 && Runtime.ZipDevScriptProcessor.AllPendingScriptsComplete()
#endif
		;

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
		var command = split.array[0];
		var arguments = split.array.Skip(1).ToString(" ");

		return $"carbonprotecc_{RandomEx.GetRandomString(command.Length, command + RuntimeId, command.Length)} {arguments}".TrimEnd();
	}
}
