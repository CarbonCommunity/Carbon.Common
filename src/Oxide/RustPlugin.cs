using Cysharp.Text;
using Logger = Carbon.Logger;
using Player = Oxide.Game.Rust.Libraries.Player;

namespace Oxide.Plugins;

public class RustPlugin : Plugin
{
	private sealed class OnlinePlayerFieldTracker
	{
		public FieldInfo Field;
		public Type ValueType;
		public ConstructorInfo ValueConstructor;
		public FieldInfo PlayerField;
		public MethodInfo AddMethod;
		public MethodInfo RemoveMethod;
	}

	private static readonly object OnlinePlayerTrackingLock = new();
	private static readonly HashSet<RustPlugin> OnlinePlayerTrackingPlugins = [];

	private readonly List<OnlinePlayerFieldTracker> _onlinePlayerFields = [];
	private bool _onlinePlayerTrackingInitialized;

	public bool IsPrecompiled { get; set; }
	public bool IsExtension { get; set; }

	public Lang lang;
	public Server server;
	public Oxide.Core.Libraries.Plugins plugins;
	public Timers timer;
	public OxideMod mod;
	public WebRequests webrequest;
	public Oxide.Game.Rust.Libraries.Rust rust;
	public Covalence covalence;

	public Player Player { get { return rust.Player; } private set { } }
	public Server Server { get { return rust.Server; } private set { } }

	private string _cachedLogFolder;
	private string _cachedDateStr;
	private int _cachedDay;
	private HashSet<string> _createdLogFolders;

	public override bool IInit()
	{
		InitializeOnlinePlayerTracking();
		return base.IInit();
	}

	public override void IUnload()
	{
		DisableOnlinePlayerTracking();
		base.IUnload();
	}

	public virtual void SetupMod(ModLoader.Package mod, string name, string author, VersionNumber version, string description)
	{
		Package = mod;
		Setup(name, author, version, description);
	}

	private void InitializeOnlinePlayerTracking()
	{
		if (_onlinePlayerTrackingInitialized)
		{
			return;
		}

		_onlinePlayerTrackingInitialized = true;
		_onlinePlayerFields.Clear();

		foreach (var field in GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
		{
			if (field.GetCustomAttribute<OnlinePlayersAttribute>() == null)
			{
				continue;
			}

			if (!TryCreateOnlinePlayerFieldTracker(field, out var tracker, out var reason))
			{
				Puts($"The {field.Name} field {reason} (online players will not be tracked)");
				continue;
			}

			_onlinePlayerFields.Add(tracker);
		}

		if (_onlinePlayerFields.Count == 0)
		{
			return;
		}

		foreach (var player in BasePlayer.activePlayerList)
		{
			AddOnlinePlayer(player);
		}

		lock (OnlinePlayerTrackingLock)
		{
			OnlinePlayerTrackingPlugins.Add(this);
		}
	}

	private void DisableOnlinePlayerTracking()
	{
		lock (OnlinePlayerTrackingLock)
		{
			OnlinePlayerTrackingPlugins.Remove(this);
		}

		_onlinePlayerFields.Clear();
		_onlinePlayerTrackingInitialized = false;
	}

	private bool TryCreateOnlinePlayerFieldTracker(FieldInfo field, out OnlinePlayerFieldTracker tracker, out string reason)
	{
		tracker = null;

		var genericArguments = field.FieldType.GetGenericArguments();
		if (genericArguments.Length != 2 || genericArguments[0] != typeof(BasePlayer))
		{
			reason = "is not a Hash with a BasePlayer key";
			return false;
		}

		var addMethod = field.FieldType.GetMethod("Add", [typeof(BasePlayer), genericArguments[1]]);
		if (addMethod == null)
		{
			reason = "does not support adding BasePlayer keys";
			return false;
		}

		var removeMethod = field.FieldType.GetMethod("Remove", [typeof(BasePlayer)]);
		if (removeMethod == null)
		{
			reason = "does not support removing BasePlayer keys";
			return false;
		}

		var playerField = genericArguments[1].GetField("Player", BindingFlags.Instance | BindingFlags.Public);
		if (playerField == null || !playerField.FieldType.IsAssignableFrom(typeof(BasePlayer)))
		{
			reason = $"is using a class without a public Player field";
			return false;
		}

		var valueConstructor = genericArguments[1].GetConstructor([typeof(BasePlayer)])
			?? genericArguments[1].GetConstructor(Type.EmptyTypes);
		if (valueConstructor == null)
		{
			reason = "is using a class which contains no valid constructor";
			return false;
		}

		if (field.GetValue(this) == null && field.FieldType.GetConstructor(Type.EmptyTypes) == null)
		{
			reason = "is null and cannot be instantiated";
			return false;
		}

		tracker = new OnlinePlayerFieldTracker
		{
			Field = field,
			ValueType = genericArguments[1],
			ValueConstructor = valueConstructor,
			PlayerField = playerField,
			AddMethod = addMethod,
			RemoveMethod = removeMethod
		};

		reason = string.Empty;
		return true;
	}

	private object GetOnlinePlayerFieldValue(OnlinePlayerFieldTracker field)
	{
		var value = field.Field.GetValue(this);
		if (value != null)
		{
			return value;
		}

		var constructor = field.Field.FieldType.GetConstructor(Type.EmptyTypes);
		if (constructor == null)
		{
			return null;
		}

		value = constructor.Invoke(null);
		field.Field.SetValue(this, value);
		return value;
	}

	private void AddOnlinePlayer(BasePlayer player)
	{
		if (player == null || _onlinePlayerFields.Count == 0)
		{
			return;
		}

		foreach (var trackedField in _onlinePlayerFields)
		{
			try
			{
				var fieldValue = GetOnlinePlayerFieldValue(trackedField);
				if (fieldValue == null)
				{
					continue;
				}

				var onlinePlayer = trackedField.ValueConstructor.GetParameters().Length == 0
					? Activator.CreateInstance(trackedField.ValueType)
					: trackedField.ValueConstructor.Invoke([player]);

				trackedField.PlayerField.SetValue(onlinePlayer, player);
				trackedField.AddMethod.Invoke(fieldValue, [player, onlinePlayer]);
			}
			catch (Exception ex)
			{
				Logger.Error($"[{Title}] Failed tracking online player connect for field '{trackedField.Field.Name}'", ex);
			}
		}
	}

	private void RemoveOnlinePlayer(BasePlayer player)
	{
		if (player == null || _onlinePlayerFields.Count == 0)
		{
			return;
		}

		foreach (var trackedField in _onlinePlayerFields)
		{
			try
			{
				var fieldValue = trackedField.Field.GetValue(this);
				if (fieldValue == null)
				{
					continue;
				}

				trackedField.RemoveMethod.Invoke(fieldValue, [player]);
			}
			catch (Exception ex)
			{
				Logger.Error($"[{Title}] Failed tracking online player disconnect for field '{trackedField.Field.Name}'", ex);
			}
		}
	}

	private static RustPlugin[] GetOnlinePlayerTrackingPlugins()
	{
		lock (OnlinePlayerTrackingLock)
		{
			return OnlinePlayerTrackingPlugins.Count == 0
				? []
				: [..OnlinePlayerTrackingPlugins];
		}
	}

	internal static void HandleOnlinePlayerConnected(BasePlayer player)
	{
		if (player == null)
		{
			return;
		}

		foreach (var plugin in GetOnlinePlayerTrackingPlugins())
		{
			if (plugin == null || !plugin.IsLoaded)
			{
				continue;
			}

			plugin.AddOnlinePlayer(player);
		}
	}

	internal static void HandleOnlinePlayerDisconnected(BasePlayer player)
	{
		if (player == null)
		{
			return;
		}

		var trackedPlugins = GetOnlinePlayerTrackingPlugins();
		if (trackedPlugins.Length == 0)
		{
			return;
		}

		if (Community.Runtime?.Core != null)
		{
			Community.Runtime.Core.NextTick(RemoveFromTrackedPlugins);
		}
		else
		{
			RemoveFromTrackedPlugins();
		}

		return;

		void RemoveFromTrackedPlugins()
		{
			foreach (var plugin in trackedPlugins)
			{
				if (plugin == null || !plugin.IsLoaded)
				{
					continue;
				}

				plugin.RemoveOnlinePlayer(player);
			}
		}
	}

	public virtual void Setup(string name, string author, VersionNumber version, string description)
	{
		Name = GetType().Name;
		Title = name.Replace(":", string.Empty);
		Version = version;
		Author = author;
		Description = description;

		permission = Interface.Oxide.Permission;
		cmd = new Command();
		server = new Server();
		Manager = new PluginManager();
		plugins = new Oxide.Core.Libraries.Plugins(Manager);
		timer = new Timers(this);
		lang = new Lang(this);
		mod = Interface.Oxide;
		rust = new Game.Rust.Libraries.Rust();
		webrequest = new WebRequests();
		persistence = new GameObject($"Script_{name}").AddComponent<Persistence>();
		UnityEngine.Object.DontDestroyOnLoad(persistence.gameObject);
		covalence = new Covalence();

		HookableType = GetType();
	}

	public override void Dispose()
	{
		permission.UnregisterPermissions(this);

		timer?.Clear();
		timer = null;

		_createdLogFolders = null;
		_cachedLogFolder = null;
		_cachedDateStr = null;

		if (persistence != null)
		{
			var go = persistence.gameObject;
			UnityEngine.Object.DestroyImmediate(persistence);
			UnityEngine.Object.Destroy(go);
		}

		base.Dispose();
	}

	public static T Singleton<T>()
	{
		foreach (var mod in ModLoader.Packages)
		{
			foreach (var plugin in mod.Plugins)
			{
				if (plugin is T result)
				{
					return result;
				}
			}
		}

		return default;
	}

	#region Logging

	/// <summary>
	/// Outputs to the game's console a message with severity level 'NOTICE'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void Puts(object message) => Logger.Log($"[{Title}] {message}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'NOTICE'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void Puts(object message, params object[] args) => Logger.Log($"[{Title}] {(args == null || args.Length == 0 ? message : string.Format(message?.ToString() ?? string.Empty, args))}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'NOTICE'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void Puts(string message, params object[] args) => Puts((object)message, args: args);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'NOTICE'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void Log(object message) => Logger.Log($"[{Title}] {message}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'NOTICE'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void Log(object message, params object[] args) => Logger.Log($"[{Title}] {(args == null || args.Length == 0 ? message : string.Format(message?.ToString() ?? string.Empty, args))}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'WARNING'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void LogWarning(object message) => Logger.Warn($"[{Title}] {message}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'WARNING'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void LogWarning(object message, params object[] args) => Logger.Warn($"[{Title}] {(args == null || args.Length == 0 ? message : string.Format(message?.ToString() ?? string.Empty, args))}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="ex"></param>
	public void LogError(object message, Exception ex) => Logger.Error($"[{Title}] {message}", ex);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="ex"></param>
	public void LogError(object message, Exception ex, params object[] args) => Logger.Error($"[{Title}] {(args == null || args.Length == 0 ? message : string.Format(message?.ToString() ?? string.Empty, args))}", ex);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void LogError(object message) => Logger.Error($"[{Title}] {message}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void LogError(object message, params object[] args) => Logger.Error($"[{Title}] {(args == null || args.Length == 0 ? message : string.Format(message?.ToString() ?? string.Empty, args))}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'WARNING'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void PrintWarning(object format, params object[] args) => Logger.Warn($"[{Title}] {(args == null || args.Length == 0 ? format : string.Format(format?.ToString() ?? string.Empty, args))}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void PrintError(object format, params object[] args) => Logger.Error($"[{Title}] {(args == null || args.Length == 0 ? format : string.Format(format?.ToString() ?? string.Empty, args))}");

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void RaiseError(object message) => Logger.Error($"[{Title}] {message}", null);

	protected void LogToFile(string filename, string text, Plugin plugin = null, bool timeStamp = true, bool anotherBool = false)
	{
		if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(text))
			return;

		DateTime now = DateTime.Now;

		if (_cachedDay != now.Day)
		{
			_cachedDay = now.Day;
			_cachedDateStr = now.ToString("yyyy-MM-dd");
		}

		string logFolder, finalFileName;

		if (plugin == null)
		{
			string subFolder = Path.GetDirectoryName(filename);
			string fileOnly = Path.GetFileNameWithoutExtension(filename);

			logFolder = string.IsNullOrEmpty(subFolder)
				? Defines.GetLogsFolder()
				: Path.Combine(Defines.GetLogsFolder(), subFolder);

			finalFileName = timeStamp
				? ZString.Concat(fileOnly, "-", _cachedDateStr, ".txt")
				: ZString.Concat(fileOnly, ".txt");
		}
		else
		{
			logFolder = _cachedLogFolder ??= Path.Combine(Defines.GetLogsFolder(), plugin.Name);

			finalFileName = timeStamp
				? ZString.Concat(plugin.Name, "_", filename, "-", _cachedDateStr, ".txt").ToLower()
				: ZString.Concat(plugin.Name, "_", filename, ".txt").ToLower();
		}

		_createdLogFolders ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (_createdLogFolders.Add(logFolder))
		{
			OsEx.Folder.Create(logFolder);
		}

		string fullPath = Path.Combine(logFolder, Utility.CleanPath(finalFileName));

		string logEntry = timeStamp
			? ZString.Concat("[", _cachedDateStr, " ", now.ToString("HH:mm:ss"), "] ", text, Environment.NewLine)
			: ZString.Concat(text, Environment.NewLine);

		OsEx.File.Append(fullPath, logEntry);
	}

	#endregion

	#region Library

	public static T GetLibrary<T>(string name = null) where T : Library
	{
		return Interface.Oxide.GetLibrary<T>();
	}

	#endregion

	public void ILoadConfig()
	{
		try
		{
			LoadConfig();
		}
		catch (Exception ex)
		{
			LogError($"Failed ILoadConfig", ex);
		}
	}

	private bool loadedDefaultMessages;

	public void ILoadDefaultMessages()
	{
		if (loadedDefaultMessages)
		{
			return;
		}

		CallHook("LoadDefaultMessages");

		loadedDefaultMessages = true;
	}

	public override string ToPrettyString()
	{
		return $"{Title} v{Version} by {Author}";
	}

	#region Printing

	protected void PrintWarning(object message)
	{
		LogWarning(message);
	}

	protected void PrintWarning(string format, params object[] args)
	{
		LogWarning(format, args);
	}

	protected void PrintToConsole(BasePlayer player, string format, params object[] args)
	{
		if (player == null || player.net == null)
		{
			return;
		}
		player.SendConsoleCommand("echo " + ((args.Length != 0) ? string.Format(format, args) : format));
	}

	protected void PrintToConsole(string format, params object[] args)
	{
		if (BasePlayer.activePlayerList.Count >= 1)
		{
			ConsoleNetwork.BroadcastToAllClients("echo " + ((args.Length != 0) ? string.Format(format, args) : format));
		}
	}

	protected void PrintToChat(BasePlayer player, string format, params object[] args)
	{
		if (player == null || player.net == null)
		{
			return;
		}

#if !MINIMAL
		player.SendConsoleCommand("chat.add", 2, Community.Runtime.Core.DefaultServerChatId, (args.Length != 0) ? string.Format(format, args) : format);
#else
		player.SendConsoleCommand("chat.add", 2, 0, (args.Length != 0) ? string.Format(format, args) : format);
#endif
	}

	protected void PrintToChat(string format, params object[] args)
	{
		if (BasePlayer.activePlayerList.Count == 0)
		{
			return;
		}
#if !MINIMAL
			ConsoleNetwork.BroadcastToAllClients("chat.add", 2, Community.Runtime.Core.DefaultServerChatId, (args.Length != 0) ? string.Format(format, args) : format);
#else
			ConsoleNetwork.BroadcastToAllClients("chat.add", 2, 0, (args.Length != 0) ? string.Format(format, args) : format);
#endif
	}

	protected void PrintToChat(BasePlayer player, string format, long chatId, params object[] args)
	{
		if (player == null || player.net == null)
		{
			return;
		}
		player.SendConsoleCommand("chat.add", 2, chatId, (args.Length != 0) ? string.Format(format, args) : format);
	}

	protected void PrintToChat(string format, long chatId, params object[] args)
	{
		if (BasePlayer.activePlayerList.Count > 0)
		{
			ConsoleNetwork.BroadcastToAllClients("chat.add", 2, chatId, (args.Length != 0) ? string.Format(format, args) : format);
		}
	}

	protected void SendReply(ConsoleSystem.Arg arg, string format, params object[] args)
	{
		if (arg != null || arg.Connection != null)
		{
			var connection = arg.Connection;
			var basePlayer = connection?.player as BasePlayer;

			if (basePlayer != null && basePlayer.net != null)
			{
				basePlayer.SendConsoleCommand($"echo {((args != null && args.Length != 0) ? string.Format(format, args) : format)}");
				return;
			}
		}
		Puts(format, args);
	}

	protected void SendReply(BasePlayer player, string format, params object[] args)
	{
		PrintToChat(player, format, args);
	}

	protected void SendWarning(ConsoleSystem.Arg arg, string format, params object[] args)
	{
		var connection = arg.Connection;
		var basePlayer = connection?.player as BasePlayer;

		if (basePlayer != null && basePlayer.net != null)
		{
			basePlayer.SendConsoleCommand($"echo {((args != null && args.Length != 0) ? string.Format(format, args) : format)}");
			return;
		}
		PrintWarning(format, args);;
	}

	protected void SendError(ConsoleSystem.Arg arg, string format, params object[] args)
	{
		var connection = arg.Connection;
		var basePlayer = connection?.player as BasePlayer;
		if (basePlayer != null && basePlayer.net != null)
		{
			basePlayer.SendConsoleCommand($"echo {((args != null && args.Length != 0) ? string.Format(format, args) : format)}");
			return;
		}
		PrintError(format, args);;
	}

	#endregion

	protected void ForcePlayerPosition(BasePlayer player, Vector3 destination)
	{
		player.MovePosition(destination);

		if (!player.IsSpectating() || (double)Vector3.Distance(player.transform.position, destination) > 25.0)
		{
			player.ClientRPC(RpcTarget.Player("ForcePositionTo", player), destination);
			return;
		}

		player.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
	}

	#region Covalence

	protected void AddCovalenceCommand(string command, string callback, params string[] perms)
	{
		cmd.AddCovalenceCommand(command, this, callback, permissions: perms);

		if (perms != null)
		{
			foreach (var permission in perms)
			{
				if (!this.permission.PermissionExists(permission))
				{
					this.permission.RegisterPermission(permission, this);
				}
			}
		}
	}

	protected void AddCovalenceCommand(string[] commands, string callback, params string[] perms)
	{
		foreach (var command in commands)
		{
			cmd.AddCovalenceCommand(command, this, callback, permissions: perms);
		}

		if (perms != null)
		{
			foreach (var permission in perms)
			{
				if (!this.permission.PermissionExists(permission))
				{
					this.permission.RegisterPermission(permission, this);
				}
			}
		}
	}

	protected void AddUniversalCommand(string command, string callback, params string[] perms)
	{
		cmd.AddCovalenceCommand(command, this, callback, permissions: perms);

		if (perms != null)
		{
			foreach (var permission in perms)
			{
				if (!this.permission.PermissionExists(permission))
				{
					this.permission.RegisterPermission(permission, this);
				}
			}
		}
	}

	protected void AddUniversalCommand(string[] commands, string callback, params string[] perms)
	{
		foreach (var command in commands)
		{
			cmd.AddCovalenceCommand(command, this, callback, permissions: perms);
		}

		if (perms != null)
		{
			foreach (var permission in perms)
			{
				if (!this.permission.PermissionExists(permission))
				{
					this.permission.RegisterPermission(permission, this);
				}
			}
		}
	}

	#endregion
}
