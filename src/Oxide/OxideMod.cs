﻿/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Oxide.Core.Extensions;
using Oxide.Plugins;
using Logger = Carbon.Logger;

namespace Oxide.Core;

public class OxideMod
{
	public DataFileSystem DataFileSystem { get; private set; } = new DataFileSystem(Defines.GetDataFolder());
	public PluginManager RootPluginManager { get; private set; }

	public Permission Permission { get; private set; }

	public string RootDirectory { get; private set; }
	public string InstanceDirectory { get; private set; }
	public string PluginDirectory { get; private set; }
	public string ConfigDirectory { get; private set; }
	public string DataDirectory { get; private set; }
	public string LangDirectory { get; private set; }
	public string LogDirectory { get; private set; }
	public string TempDirectory { get; private set; }
	public string ExtensionDirectory { get; private set; }

	public bool IsShuttingDown { get; private set; }

	private ExtensionManager extensionManager = new();
	public IEnumerable<PluginLoader> GetPluginLoaders() => extensionManager.GetPluginLoaders();
	public OxideConfig Config { get; private set; } = new(Path.Combine(Defines.GetRootFolder(), "oxide.config.json"));

	internal static readonly Version _assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
	internal List<Extension> _extensions = new();

	public float Now => UnityEngine.Time.realtimeSinceStartup;

	public void Load()
	{
		InstanceDirectory = Defines.GetRootFolder();
		RootDirectory = Environment.CurrentDirectory;
		if (RootDirectory.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
			RootDirectory = AppDomain.CurrentDomain.BaseDirectory;

		ConfigDirectory = Defines.GetConfigsFolder();
		DataDirectory = Defines.GetDataFolder();
		LangDirectory = Defines.GetLangFolder();
		LogDirectory = Defines.GetLogsFolder();
		PluginDirectory = Defines.GetScriptsFolder();
		TempDirectory = Defines.GetTempFolder();
		ExtensionDirectory = Defines.GetExtensionsFolder();

		DataFileSystem = new DataFileSystem(DataDirectory);
		RootPluginManager = new PluginManager();

		switch (Community.Runtime.Config.Permissions.PermissionSerialization)
		{
			case Permission.SerializationMode.Storeless:
				Permission = new PermissionStoreless();
				break;

			case Permission.SerializationMode.Protobuf:
				Permission = new Permission();
				break;

			case Permission.SerializationMode.SQL:
				Permission = new PermissionSql();
				break;
		}

		_extensions.Add(new Extension { Name = "Rust", Author = "Carbon Community LTD", Branch = "none", Filename = "Carbon.dll", Version = new VersionNumber(1, 0, 0) });

		CovalencePlugin.PlayerManager.RefreshDatabase(Permission.userdata);
	}

	public void NextTick(Action callback)
	{
		var processor = Community.Runtime.CarbonProcessor;

		lock (processor.CurrentFrameLock)
		{
			processor.CurrentFrameQueue.Add(callback);
		}
	}

	public void NextFrame(Action callback)
	{
		var processor = Community.Runtime.CarbonProcessor;

		lock (processor.CurrentFrameLock)
		{
			processor.CurrentFrameQueue.Add(callback);
		}
	}

	public void LoadPlugin(string name)
	{
		var path = CorePlugin.GetPluginPath(name);

		Community.Runtime.ScriptProcessor.Prepare(path.Key, path.Value);
	}

	public void ReloadPlugin(string name)
	{
		LoadPlugin(name);
	}

	public bool UnloadPlugin(string name)
	{
		if (Community.Runtime.ScriptProcessor.InstanceBuffer.TryGetValue(name, out var instance))
		{
			instance?.Clear();
			instance?.Dispose();
			Community.Runtime.ScriptProcessor.Remove(name);
			return true;
		}

		return true;
	}

	public void UnloadAllPlugins(IList<string> skip = null)
	{
		Community.Runtime.ScriptProcessor.Clear(skip);
		Community.Runtime.ZipScriptProcessor.Clear(skip);
	}

	public void OnSave()
	{

	}

	public void OnShutdown()
	{
		if (!IsShuttingDown)
		{
			IsShuttingDown = true;
		}
	}

	public IEnumerable<Extension> GetAllExtensions()
	{
		return _extensions;
	}

	public object CallHook(string hookName, params object[] args)
	{
		var hookId = HookStringPool.GetOrAdd(hookName);

		return args?.Length switch
		{
			1 => HookCaller.CallStaticHook(hookId, args[0]),
			2 => HookCaller.CallStaticHook(hookId, args[0], args[1]),
			3 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2]),
			4 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3]),
			5 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4]),
			6 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5]),
			7 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5], args[6]),
			8 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]),
			9 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]),
			10 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]),
			11 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]),
			12 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]),
			13 => HookCaller.CallStaticHook(hookId, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12]),
			_ => HookCaller.CallStaticHook(hookId, args, true)
		};
	}

	public object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, params object[] args)
	{
		var oldHookId = HookStringPool.GetOrAdd(oldHook);
		var newHookId = HookStringPool.GetOrAdd(newHook);

		return args.Length switch
		{
			1 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0]),
			2 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1]),
			3 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2]),
			4 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3]),
			5 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4]),
			6 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5]),
			7 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5], args[6]),
			8 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]),
			9 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]),
			10 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]),
			11 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]),
			12 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]),
			13 => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12]),
			_ => HookCaller.CallStaticDeprecatedHook(oldHookId, newHookId, expireDate)
		};
	}

	internal static ConcurrentDictionary<string, object> _libraryCache = new();

	public T GetLibrary<T>(string name = null) where T : Library
	{
		var type = typeof(T);

		if (type == typeof(Permission)) return Community.Runtime.CorePlugin.permission as T;
		else if (type == typeof(Lang)) return Community.Runtime.CorePlugin.lang as T;
		else if (type == typeof(Command)) return Community.Runtime.CorePlugin.cmd as T;
		else if (type == typeof(Game.Rust.Libraries.Rust)) return Community.Runtime.CorePlugin.rust as T;
		else if (type == typeof(WebRequests)) return Community.Runtime.CorePlugin.webrequest as T;
		else if (type == typeof(Timers)) return Community.Runtime.CorePlugin.timer as T;

		name ??= type.Name;

		if (!_libraryCache.TryGetValue(name, out var instance))
		{
			try { instance = Activator.CreateInstance<T>(); }
			catch
			{
				try { instance = FormatterServices.GetUninitializedObject(typeof(T)) as T; }
				catch { }
			}

			_libraryCache.TryAdd(name, instance);
		}

		return instance as T;
	}

	public Extension GetExtension(string name = null)
	{
		return null;
	}

	public void LoadExtension(string name)
	{
	}

	public void LoadAllPlugins(bool init = false)
	{

	}

	public static readonly VersionNumber Version = new(_assemblyVersion.Major, _assemblyVersion.Minor, _assemblyVersion.Build);

	#region Logging

	/// <summary>
	/// Outputs to the game's console a message with severity level 'DEBUG'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	public void LogDebug(string message, params object[] args)
		=> Logger.Debug(args != null && args.Length > 0 ? string.Format(message, args) : message);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void LogError(string message, params object[] args)
		=> Logger.Error(args != null && args.Length > 0 ? string.Format(message, args) : message);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="ex"></param>
	public void LogException(string message, Exception ex)
		=> Logger.Error(message, ex);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'NOTICE'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void LogInfo(string message, params object[] args)
		=> Logger.Log(args != null && args.Length > 0 ? string.Format(message, args) : message);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'WARNING'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void LogWarning(string message, params object[] args)
		=> Logger.Warn(args != null && args.Length > 0 ? string.Format(message, args) : message);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'WARNING'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void PrintWarning(string message, params object[] args)
		=> Logger.Warn(args != null && args.Length > 0 ? string.Format(message, args) : message);

	/// <summary>
	/// Outputs to the game's console a message with severity level 'ERROR'.
	/// NOTE: Oxide compatibility layer.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void PrintError(string message, params object[] args)
		=> Logger.Error(args != null && args.Length > 0 ? string.Format(message, args) : message);

	#endregion
}
