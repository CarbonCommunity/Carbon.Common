﻿using Carbon.Base.Interfaces;
using Defines = Carbon.Core.Defines;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Base;

public abstract class BaseModule : BaseHookable
{
	public string Context { get; set; }

	public virtual bool EnabledByDefault => false;
	public virtual bool ForceModded => false;
	public virtual bool ForceEnabled => false;

	public abstract void OnServerInit(bool initial);
	public abstract void OnPostServerInit(bool initial);
	public abstract void OnServerSaved();
	public abstract void Load();
	public abstract void Save();
	public abstract void Unload();
	public abstract void Reload();
	public abstract bool GetEnabled();
	public abstract void SetEnabled(bool enable);
	public abstract void Shutdown();

	public static T GetModule<T>()
	{
		foreach (var module in Community.Runtime.ModuleProcessor.Modules)
		{
			if (module.GetType() == typeof(T) && module is T result) return result;
		}

		return default;
	}
	public static BaseModule FindModule(string name)
	{
		return Community.Runtime.ModuleProcessor.Modules.FirstOrDefault(x => x.Type.Name == name) as BaseModule;
	}
}

public class EmptyModuleConfig { }
public class EmptyModuleData { }

public abstract class CarbonModule<C, D> : BaseModule, IModule
{
	public Configuration ModuleConfiguration { get; set; }
	public DynamicConfigFile Config { get; private set; }
	public DynamicConfigFile Data { get; private set; }
	public Lang Lang { get; private set; }

	public new virtual Type Type => default;

	public D DataInstance { get; private set; }
	public C ConfigInstance { get; private set; }

	public new virtual string Name => "Not set";
	public bool HasOSI { get; set; }

	protected void Puts(object message)
		=> Logger.Log($"[{Name}] {message}");
	protected void PutsError(object message, Exception ex = null)
		=> Logger.Error($"[{Name}] {message}", ex);
	protected void PutsWarn(object message)
		=> Logger.Warn($"[{Name}] {message}");

	public virtual void Dispose()
	{
		Config = null;
		ModuleConfiguration = null;
	}

	public virtual void Init()
	{
		base.Hooks ??= new();
		base.Name ??= Name;
		base.Type ??= Type;

		TrackInit();

		InternalCallHookOverriden = false;
	}
	public virtual bool InitEnd()
	{
		if (HasInitialized) return false;

		Community.Runtime.HookManager.LoadHooksFromType(Type);

		BuildHookCache(BindingFlags.Instance | BindingFlags.NonPublic);

		foreach (var method in Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
		{
			if (Community.Runtime.HookManager.IsHook(method.Name))
			{
				Community.Runtime.HookManager.Subscribe(method.Name, Name);

				var hash = HookStringPool.GetOrAdd(method.Name);
				if (!Hooks.Contains(hash)) Hooks.Add(hash);
			}
		}

		var phrases = GetDefaultPhrases();

		if (phrases != null)
		{
			foreach (var language in phrases)
			{
				Lang.RegisterMessages(language.Value, this, language.Key);
			}
		}

		Puts("Initialized.");
		HasInitialized = true;

		return true;
	}
	public override void Load()
	{
		var shouldSave = false;

		Config ??= new DynamicConfigFile(GetConfigPath());
		Data ??= new DynamicConfigFile(GetDataPath());
		Lang ??= new(this);

		var newConfig = !Config.Exists();
		var newData = !Data.Exists();

		if (!Config.Exists())
		{
			ModuleConfiguration = new Configuration
			{
				Config = Activator.CreateInstance<C>()
			};

			if (EnabledByDefault)
			{
				ModuleConfiguration.Enabled = true;
			}
			shouldSave = true;
		}
		else
		{
			try
			{
				ModuleConfiguration = Config.ReadObject<Configuration>();

				if (ModuleConfiguration.HasConfigStructureChanged())
				{
					shouldSave = true;
				}
			}
			catch (Exception exception)
			{
				Logger.Error($"Failed loading config. JSON file is corrupted and/or invalid.", exception);
			}
		}

		ConfigInstance = ModuleConfiguration.Config;
		if (ForceEnabled) ModuleConfiguration.Enabled = true;

		if (typeof(D) != typeof(EmptyModuleData))
		{
			if (!Data.Exists())
			{
				DataInstance = Activator.CreateInstance<D>();
				shouldSave = true;
			}
			else
			{
				try
				{
					DataInstance = Data.ReadObject<D>();
				}
				catch (Exception exception)
				{
					Logger.Error($"Failed loading data. JSON file is corrupted and/or invalid.", exception);
				}
			}
		}

		if (PreLoadShouldSave(newConfig, newData)) shouldSave = true;

		if (shouldSave) Save();

		OnEnableStatus();
	}
	public virtual bool PreLoadShouldSave(bool newConfig, bool newData)
	{
		return false;
	}
	public override void Save()
	{
		if (ModuleConfiguration == null)
		{
			ModuleConfiguration = new Configuration { Config = Activator.CreateInstance<C>() };
			ConfigInstance = ModuleConfiguration.Config;
		}

		if (DataInstance == null && typeof(D) != typeof(EmptyModuleData))
		{
			DataInstance = Activator.CreateInstance<D>();
		}

		if (ForceEnabled)
		{
			ModuleConfiguration.Enabled = true;
		}

		Config.WriteObject(ModuleConfiguration);
		if (DataInstance != null) Data?.WriteObject(DataInstance);
	}
	public override void Reload()
	{
		try
		{
			Unload();
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed module Unload for {Name} [Reload Request]", ex);
		}

		try
		{
			Load();
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed module Load for {Name} [Reload Request]", ex);
		}

		try
		{
			OnServerInit(false);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed module OnServerInit for {Name} [Reload Request]", ex);
		}

		try
		{
			OnPostServerInit(false);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed module OnPostServerInit for {Name} [Reload Request]", ex);
		}

		HookCaller.CallHook(this, 1330569572, Community.IsServerInitialized);

		HasOSI = true;
	}
	public override void Unload()
	{
		HasOSI = false;
	}
	public override void Shutdown()
	{
		Unload();

		Community.Runtime.ModuleProcessor.Uninstall(this);
	}

	public virtual string GetConfigPath()
	{
		return Path.Combine(Defines.GetModulesFolder(), Name, "config.json");
	}
	public virtual string GetDataPath()
	{
		return Path.Combine(Defines.GetModulesFolder(), Name, "data.json");
	}

	public override void SetEnabled(bool enable)
	{
		if (ModuleConfiguration != null)
		{
			ModuleConfiguration.Enabled = enable;
			OnEnableStatus();
		}
	}
	public override bool GetEnabled()
	{
		return ModuleConfiguration != null && ModuleConfiguration.Enabled;
	}

	public virtual void OnDisabled(bool initialized)
	{
		if (initialized) ModLoader.RemoveCommands(this);

		UnsubscribeAll();
		UnregisterPermissions();

		if (Hooks.Count > 0) Puts($"Unsubscribed from {Hooks.Count:n0} {Hooks.Count.Plural("hook", "hooks")}.");
	}
	public virtual void OnEnabled(bool initialized)
	{
		if (initialized) ModLoader.ProcessCommands(Type, this, flags: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		SubscribeAll();

		if (Hooks.Count > 0) Puts($"Subscribed to {Hooks.Count:n0} {Hooks.Count.Plural("hook", "hooks")}.");

		if (InitEnd())
		{
			if (initialized) OnServerInit(true);
		}
	}

	public void OnEnableStatus()
	{
		try
		{
			if (ModuleConfiguration == null) return;

			if (ModuleConfiguration.Enabled) OnEnabled(Community.IsServerInitialized);
			else OnDisabled(Community.IsServerInitialized);
		}
		catch (Exception ex) { Logger.Error($"Failed {(ModuleConfiguration.Enabled ? "Enable" : "Disable")} initialization.", ex); }
	}

	public override void OnServerSaved()
	{

	}

	public override void OnServerInit(bool initial)
	{
		if (initial)
		{
			OnEnableStatus();
		}
	}
	public override void OnPostServerInit(bool initial)
	{

	}

	#region Permission

	public virtual bool GroupExists(string group)
	{
		return Community.Runtime.CorePlugin.permission.GroupExists(group);
	}
	public virtual bool HasGroup(string userId, string group)
	{
		return Community.Runtime.CorePlugin.permission.UserHasGroup(userId, group);
	}
	public virtual bool HasGroup(BasePlayer player, string group)
	{
		return HasGroup(player.UserIDString, group);
	}

	public virtual bool PermissionExists(string permission)
	{
		return Community.Runtime.CorePlugin.permission.PermissionExists(permission, this);
	}
	public virtual void RegisterPermission(string permission)
	{
		if (PermissionExists(permission)) return;

		Community.Runtime.CorePlugin.permission.RegisterPermission(permission, this);
	}
	public virtual void UnregisterPermissions()
	{
		Community.Runtime.CorePlugin.permission.UnregisterPermissions(this);
	}
	public virtual bool HasPermission(string userId, string permission)
	{
		return Community.Runtime.CorePlugin.permission.UserHasPermission(userId, permission);
	}
	public virtual bool HasPermission(BasePlayer player, string permission)
	{
		return HasPermission(player.UserIDString, permission);
	}

	#endregion

	#region Localisation

	public virtual Dictionary<string, Dictionary<string, string>> GetDefaultPhrases() => null;

	public virtual string GetPhrase(string key)
	{
		return Lang.GetMessage(key, this);
	}
	public virtual string GetPhrase(string key, string playerId)
	{
		return Lang.GetMessage(key, this, playerId);
	}
	public virtual string GetPhrase(string key, ulong playerId)
	{
		return Lang.GetMessage(key, this, playerId == 0 ? string.Empty : playerId.ToString());
	}

	#endregion

	public void NextFrame(Action callback)
	{
		Community.Runtime.CorePlugin.NextFrame(callback);
	}

	public class Configuration : IModuleConfig
	{
		public bool Enabled { get; set; }
		public C Config { get; set; }
		public string Version { get; set; }

		public string GetVersion()
		{
			if (Config == null)
			{
				return null;
			}

			var type = Config.GetType();
			var content = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.PropertyType.FullName + x.Name)
				.Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(x => x.FieldType.FullName + x.Name))
				.Select(x => x).ToString(string.Empty);

			return StringPool.Add(content).ToString();
		}

		public bool HasConfigStructureChanged()
		{
			var version = GetVersion();
			var isValid = version == Version;

			if (!isValid)
			{
				Version = version;
			}

			return !isValid;
		}
	}
}
