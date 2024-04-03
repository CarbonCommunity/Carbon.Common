using API.Events;
using Newtonsoft.Json;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public static class ModLoader
{
	public static bool IsBatchComplete { get; set; }
	public static List<ModPackage> LoadedPackages = new();
	public static Dictionary<string, FailedCompilation> FailedCompilations = new();

	internal static Dictionary<string, Type> TypeDictionaryCache { get; } = new();
	internal static Dictionary<string, List<string>> PendingRequirees { get; } = new();
	internal static List<string> PostBatchFailedRequirees { get; } = new();
	internal static bool FirstLoadSinceStartup { get; set; } = true;

	internal const string CARBON_PLUGIN = "CarbonPlugin";
	internal const string RUST_PLUGIN = "RustPlugin";
	internal const string COVALENCE_PLUGIN = "CovalencePlugin";

	public static FailedCompilation GetOrCreateFailedCompilation(string file, bool clear = false)
	{
		if (!FailedCompilations.TryGetValue(file, out var result))
		{
			FailedCompilations[file] = result = new()
			{
				File = file
			};
		}

		if (clear)
		{
			result.Clear();
		}

		return result;
	}
	public static void RegisterPackage(ModPackage package)
	{
		if (!LoadedPackages.Contains(package))
		{
			LoadedPackages.Add(package);
		}
	}
	public static ModPackage GetPackage(string name)
	{
		return LoadedPackages.FirstOrDefault(mod => mod.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
	}
	public static RustPlugin FindPlugin(string name)
	{
		return LoadedPackages.SelectMany(package => package.Plugins).FirstOrDefault(plugin => plugin.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	static ModLoader()
	{
		Community.Runtime.Events.Subscribe(
			CarbonEvent.OnServerInitialized,
			x => OnPluginProcessFinished()
		);
	}

	public static List<string> GetRequirees(Plugin initial)
	{
		if (string.IsNullOrEmpty(initial.FilePath)) return null;

		if (PendingRequirees.TryGetValue(initial.FilePath, out var requirees))
		{
			return requirees;
		}

		return null;
	}

	public static void AddPendingRequiree(string initial, string requiree)
	{
		if (!PendingRequirees.TryGetValue(initial, out var requirees))
		{
			PendingRequirees.Add(initial, requirees = new List<string>(20));
		}

		if (!requirees.Contains(requiree))
		{
			requirees.Add(requiree);
		}
	}
	public static void AddPendingRequiree(Plugin initial, Plugin requiree)
	{
		AddPendingRequiree(initial.FilePath, requiree.FilePath);
	}
	public static void AddPostBatchFailedRequiree(string requiree)
	{
		PostBatchFailedRequirees.Add(requiree);
	}

	public static void ClearPendingRequirees(Plugin initial)
	{
		if (PendingRequirees.TryGetValue(initial.FilePath, out var requirees))
		{
			requirees.Clear();
			PendingRequirees[initial.FilePath] = null;
			PendingRequirees.Remove(initial.FilePath);
		}
	}
	public static void ClearAllRequirees()
	{
		var requirees = new Dictionary<string, List<string>>();
		foreach (var requiree in PendingRequirees) requirees.Add(requiree.Key, requiree.Value);

		foreach (var requiree in requirees)
		{
			requiree.Value.Clear();
			PendingRequirees[requiree.Key] = null;
		}

		PendingRequirees.Clear();
		requirees.Clear();
		requirees = null;
	}
	public static void ClearAllErrored()
	{
		foreach (var mod in FailedCompilations.Values)
		{
			mod.Clear();
		}
	}

	public static Type GetRegisteredType(string key)
	{
		if (TypeDictionaryCache.TryGetValue(key, out var type))
		{
			return type;
		}

		return null;
	}
	public static void RegisterType(string key, Type assembly)
	{
		TypeDictionaryCache[key] = assembly;
	}

	public static void UnloadCarbonMods(bool includeCore = false)
	{
		ClearAllRequirees();

		var list = Facepunch.Pool.GetList<ModPackage>();
		list.AddRange(LoadedPackages);

		foreach (var mod in list)
		{
			if (!includeCore && mod.IsCoreMod) continue;

			UnloadCarbonMod(mod.Name);
		}

		Facepunch.Pool.FreeList(ref list);
	}
	public static bool UnloadCarbonMod(string name)
	{
		var mod = GetPackage(name);

		if (!mod.IsValid)
		{
			return false;
		}

		UninitializePlugins(mod);
		return true;
	}

	public static void InitializePlugins(ModPackage mod)
	{
		Logger.Warn($"Initializing mod '{mod.Name}'");

		foreach (var type in mod.AllTypes)
		{
			try
			{
				if (!(type.Namespace.Equals("Oxide.Plugins") || type.Namespace.Equals("Carbon.Plugins"))) continue;

				if (!IsValidPlugin(type, true)) continue;

				if (!InitializePlugin(type, out var plugin, mod)) continue;
				plugin.HasInitialized = true;

				OnPluginProcessFinished();
			}
			catch (Exception ex) { Logger.Error($"Failed loading '{mod.Name}'", ex); }
		}
	}
	public static void UninitializePlugins(ModPackage mod)
	{
		var plugins = Facepunch.Pool.GetList<RustPlugin>();
		plugins.AddRange(mod.Plugins);

		foreach (var plugin in plugins)
		{
			try
			{
				UninitializePlugin(plugin);
			}
			catch (Exception ex) { Logger.Error($"Failed unloading '{mod.Name}'", ex); }
		}

		Facepunch.Pool.FreeList(ref plugins);
	}

	public static bool InitializePlugin(Type type, out RustPlugin plugin, ModPackage package = default, Action<RustPlugin> preInit = null, bool precompiled = false)
	{
		var constructor = type.GetConstructor(Type.EmptyTypes);
		var instance = FormatterServices.GetUninitializedObject(type);
		plugin = instance as RustPlugin;
		var info = type.GetCustomAttribute<InfoAttribute>();
		var desc = type.GetCustomAttribute<DescriptionAttribute>();

		if (info == null)
		{
			Logger.Warn($"Failed loading '{type.Name}'. The plugin doesn't have the Info attribute.");
			return false;
		}

		var title = info.Title?.Replace(" ", string.Empty);
		var author = info.Author;
		var version = info.Version;
		var description = desc == null ? string.Empty : desc.Description;

		var existentPlugin = FindPlugin(title);

		if (existentPlugin != null)
		{
			UninitializePlugin(existentPlugin);
		}

		plugin.SetProcessor(Community.Runtime.ScriptProcessor);
		plugin.SetupMod(package, title, author, version, description);

		plugin.IsPrecompiled = precompiled;

		try
		{
			constructor?.Invoke(instance, null);
		}
		catch (Exception ex)
		{
			Analytics.plugin_constructor_failure(plugin);

			// OnConstructorFail
			HookCaller.CallStaticHook(937285752, plugin, ex);

			Logger.Error($"Failed executing constructor for {plugin.ToPrettyString()}. This is fatal! Unloading plugin.", ex);
			return false;
		}

		if (precompiled)
		{
			ProcessPrecompiledType(plugin);
		}

		if(precompiled || !IsValidPlugin(type.BaseType, false))
		{
			plugin.InternalCallHookOverriden = false;
		}

		package.AddPlugin(plugin);

		preInit?.Invoke(plugin);

		plugin.ILoadConfig();
		plugin.ILoadDefaultMessages();

		if (!plugin.IInit())
		{
			if (UninitializePlugin(plugin, true))
			{
				package.RemovePlugin(plugin);
				return false;
			}
		}

		plugin.ILoad();

		ProcessCommands(type, plugin);

		Interface.Oxide.RootPluginManager.AddPlugin(plugin);

		Logger.Log($"{(precompiled ? "Preloaded" : "Loaded")} plugin {plugin.ToPrettyString()}{(precompiled ? string.Empty : $" [{plugin.CompileTime.TotalMilliseconds:0}ms]")}");
		return true;
	}
	public static bool UninitializePlugin(RustPlugin plugin, bool premature = false)
	{
		if (!premature && !plugin.IsLoaded)
		{
			return true;
		}

		plugin.IUnloadDependantPlugins();

		if (!premature)
		{
			plugin.CallHook("Unload");
		}

		RemoveCommands(plugin);
		plugin.IUnload();

		if (!premature)
		{
			// OnPluginUnloaded
			HookCaller.CallStaticHook(3843290135, plugin);
		}

		plugin.Dispose();

		if (!premature)
		{
			Logger.Log($"Unloaded plugin {plugin.ToPrettyString()}");
			Interface.Oxide.RootPluginManager.RemovePlugin(plugin);

			Plugin.InternalApplyAllPluginReferences();
		}
		return true;
	}

	public static void ProcessPrecompiledType(RustPlugin plugin)
	{
		try
		{
			var type = plugin.GetType();
			var hooks = plugin.Hooks ??= new();
			var hookMethods = plugin.HookMethods ??= new();
			var pluginReferences = plugin.PluginReferences ??= new();

			foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
			{
				var hash = HookStringPool.GetOrAdd(method.Name);

				if (Community.Runtime.HookManager.IsHook(method.Name))
				{
					if (!hooks.Contains(hash)) hooks.Add(hash);
				}
				else
				{
					var attribute = method.GetCustomAttribute<HookMethodAttribute>();
					if (attribute == null) continue;

					attribute.Method = method;
					hookMethods.Add(attribute);
				}
			}

			foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				var attribute = field.GetCustomAttribute<PluginReferenceAttribute>();
				if (attribute == null) continue;

				attribute.Field = field;
				pluginReferences.Add(attribute);
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed ProcessPrecompiledType for plugin '{plugin.ToPrettyString()}'", ex);
		}
	}

	public static bool IsValidPlugin(Type type, bool recursive)
	{
		if (type == null) return false;
		if (type.Name is CARBON_PLUGIN or RUST_PLUGIN or COVALENCE_PLUGIN) return true;
		return recursive && IsValidPlugin(type.BaseType, recursive);
	}

	public static void ProcessCommands(Type type, BaseHookable hookable = null, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance, string prefix = null, bool hidden = false)
	{
		var methods = type.GetMethods(flags);
		var fields = type.GetFields(flags | BindingFlags.Public);
		var properties = type.GetProperties(flags | BindingFlags.Public);

		foreach (var method in methods)
		{
			var chatCommands = method.GetCustomAttributes<ChatCommandAttribute>();
			var consoleCommands = method.GetCustomAttributes<ConsoleCommandAttribute>();
			var rconCommands = method.GetCustomAttributes<RConCommandAttribute>();
			var protectedCommands = method.GetCustomAttributes<ProtectedCommandAttribute>();
			var commands = method.GetCustomAttributes<CommandAttribute>();
			var permissions = method.GetCustomAttributes<PermissionAttribute>();
			var groups = method.GetCustomAttributes<GroupAttribute>();
			var authLevelAttribute = method.GetCustomAttribute<AuthLevelAttribute>();
			var cooldown = method.GetCustomAttribute<CooldownAttribute>();
			var authLevel = authLevelAttribute == null ? -1 : authLevelAttribute.AuthLevel;
			var ps = permissions.Count() == 0 ? null : permissions?.Select(x => x.Name).ToArray();
			var gs = groups.Count() == 0 ? null : groups?.Select(x => x.Name).ToArray();
			var cooldownTime = cooldown == null ? 0 : cooldown.Miliseconds;

			foreach (var command in commands)
			{
				foreach (var commandName in command.Names)
				{
					var name = string.IsNullOrEmpty(prefix) ? commandName : $"{prefix}.{commandName}";
					Community.Runtime.CorePlugin.cmd.AddChatCommand(name, hookable, method, help: string.Empty, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
					Community.Runtime.CorePlugin.cmd.AddConsoleCommand(name, hookable, method, help: string.Empty, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
				}
			}

			foreach (var chatCommand in chatCommands)
			{
				Community.Runtime.CorePlugin.cmd.AddChatCommand(string.IsNullOrEmpty(prefix) ? chatCommand.Name : $"{prefix}.{chatCommand.Name}", hookable, method, help: chatCommand.Help, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
			}

			foreach (var consoleCommand in consoleCommands)
			{
				Community.Runtime.CorePlugin.cmd.AddConsoleCommand(string.IsNullOrEmpty(prefix) ? consoleCommand.Name : $"{prefix}.{consoleCommand.Name}", hookable, method, help: consoleCommand.Help, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
			}

			foreach (var protectedCommand in protectedCommands)
			{
				Community.Runtime.CorePlugin.cmd.AddConsoleCommand(Community.Protect(string.IsNullOrEmpty(prefix) ? protectedCommand.Name : $"{prefix}.{protectedCommand.Name}"), hookable, method, help: protectedCommand.Help, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: true, silent: true);
			}

			foreach (var rconCommand in rconCommands)
			{
				var cmd = new API.Commands.Command.RCon
				{
					Name = string.IsNullOrEmpty(prefix) ? rconCommand.Name : $"{prefix}.{rconCommand.Name}",
					Reference = hookable,
					Callback = arg =>
					{
						var result = method.Invoke(hookable, new object[] { arg });

						if (result != null)
						{
							Logger.Log(result);
						}
					},
					Help = rconCommand.Help,
					Token = rconCommand,
					CanExecute = (cmd, arg) => true
				};

				Community.Runtime.CommandManager.RegisterCommand(cmd, out var reason);
			}

			if (ps != null && ps.Length > 0)
			{
				foreach (var permission in ps)
				{
					if (hookable is RustPlugin plugin && !plugin.permission.PermissionExists(permission, hookable))
					{
						plugin.permission.RegisterPermission(permission, hookable);
					}
				}
			}
		}

		foreach (var field in fields)
		{
			var var = field.GetCustomAttribute<CommandVarAttribute>();
			var permissions = field.GetCustomAttributes<PermissionAttribute>();
			var groups = field.GetCustomAttributes<GroupAttribute>();
			var authLevelAttribute = field.GetCustomAttribute<AuthLevelAttribute>();
			var cooldown = field.GetCustomAttribute<CooldownAttribute>();
			var authLevel = authLevelAttribute == null ? -1 : authLevelAttribute.AuthLevel;
			var ps = permissions.Count() == 0 ? null : permissions?.Select(x => x.Name).ToArray();
			var gs = groups.Count() == 0 ? null : groups?.Select(x => x.Name).ToArray();
			var cooldownTime = cooldown == null ? 0 : cooldown.Miliseconds;

			if (var != null)
			{
				Community.Runtime.CorePlugin.cmd.AddConsoleCommand(string.IsNullOrEmpty(prefix) ? var.Name : $"{prefix}.{var.Name}", hookable, (player, command, args) =>
				{
					var value = field.GetValue(hookable);

					if (args != null && args.Length > 0)
					{
						var rawString = args.ToString(" ");

						try
						{
							if (field.FieldType == typeof(string))
							{
								value = rawString;
							}
							else if (field.FieldType == typeof(bool))
							{
								value = rawString.ToBool();
							}
							if (field.FieldType == typeof(int))
							{
								value = rawString.ToInt();
							}
							if (field.FieldType == typeof(uint))
							{
								value = rawString.ToUint();
							}
							else if (field.FieldType == typeof(float))
							{
								value = rawString.ToFloat();
							}
							else if (field.FieldType == typeof(long))
							{
								value = rawString.ToLong();
							}
							else if (field.FieldType == typeof(ulong))
							{
								value = rawString.ToUlong();
							}

							field.SetValue(hookable, value);
						}
						catch { }
					}

					value = field.GetValue(hookable);
					if (value != null && var.Protected) value = new string('*', value.ToString().Length);

					Community.LogCommand($"{command}: \"{value}\"", player);
				}, help: var.Help, reference: field, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, @protected: var.Protected, isHidden: hidden, silent: true);
			}
		}

		foreach (var property in properties)
		{
			var var = property.GetCustomAttribute<CommandVarAttribute>();
			var permissions = property.GetCustomAttributes<PermissionAttribute>();
			var groups = property.GetCustomAttributes<GroupAttribute>();
			var authLevelAttribute = property.GetCustomAttribute<AuthLevelAttribute>();
			var cooldown = property.GetCustomAttribute<CooldownAttribute>();
			var authLevel = authLevelAttribute == null ? -1 : authLevelAttribute.AuthLevel;
			var ps = permissions.Count() == 0 ? null : permissions?.Select(x => x.Name).ToArray();
			var gs = groups.Count() == 0 ? null : groups?.Select(x => x.Name).ToArray();
			var cooldownTime = cooldown == null ? 0 : cooldown.Miliseconds;

			if (var != null)
			{
				Community.Runtime.CorePlugin.cmd.AddConsoleCommand(string.IsNullOrEmpty(prefix) ? var.Name : $"{prefix}.{var.Name}", hookable, (player, command, args) =>
				{
					var value = property.GetValue(hookable);

					if (args != null && args.Length > 0)
					{
						var rawString = args.ToString(" ");

						try
						{
							if (property.PropertyType == typeof(string))
							{
								value = rawString;
							}
							else if (property.PropertyType == typeof(bool))
							{
								value = rawString.ToBool();
							}
							if (property.PropertyType == typeof(int))
							{
								value = rawString.ToInt();
							}
							if (property.PropertyType == typeof(uint))
							{
								value = rawString.ToUint();
							}
							else if (property.PropertyType == typeof(float))
							{
								value = rawString.ToFloat();
							}
							else if (property.PropertyType == typeof(long))
							{
								value = rawString.ToLong();
							}
							else if (property.PropertyType == typeof(ulong))
							{
								value = rawString.ToUlong();
							}

							property.SetValue(hookable, value);
						}
						catch { }
					}

					value = property.GetValue(hookable);
					if (value != null && var.Protected) value = new string('*', value.ToString().Length);

					Community.LogCommand($"{command}: \"{value}\"", player);
				}, help: var.Help, reference: property, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, @protected: var.Protected, isHidden: hidden, silent: true);
			}
		}

		Array.Clear(methods, 0, methods.Length);
		Array.Clear(fields, 0, fields.Length);
		Array.Clear(properties, 0, properties.Length);
	}
	public static void RemoveCommands(BaseHookable hookable)
	{
		if (hookable == null) return;

		Community.Runtime.CommandManager.ClearCommands(command => command.Reference == hookable);
	}

	public static void OnPluginProcessFinished()
	{
		var temp = Facepunch.Pool.GetList<string>();
		temp.AddRange(PostBatchFailedRequirees);

		foreach (var plugin in temp)
		{
			var file = System.IO.Path.GetFileNameWithoutExtension(plugin);
			Community.Runtime.ScriptProcessor.ClearIgnore(file);
			Community.Runtime.ScriptProcessor.Prepare(file, plugin);
		}

		PostBatchFailedRequirees.Clear();

		if (PostBatchFailedRequirees.Count == 0)
		{
			IsBatchComplete = true;
		}

		temp.Clear();
		Facepunch.Pool.FreeList(ref temp);

		if (ConVar.Global.skipAssetWarmup_crashes)
		{
			Community.Runtime.MarkServerInitialized(true);
		}

		if (Community.IsServerInitialized)
		{
			var counter = 0;
			var plugins = Facepunch.Pool.GetList<RustPlugin>();

			foreach (var mod in LoadedPackages)
			{
				foreach (var plugin in mod.Plugins)
				{
					plugins.Add(plugin);
				}
			}

			foreach (var plugin in plugins)
			{
				try { plugin.InternalApplyPluginReferences(); } catch { }
			}

			foreach (var plugin in plugins)
			{
				if (plugin.HasInitialized) continue;
				counter++;

				plugin.HasInitialized = true;
				plugin.CallHook("OnServerInitialized", FirstLoadSinceStartup);
			}

			FirstLoadSinceStartup = false;

			Facepunch.Pool.FreeList(ref plugins);

			if (counter > 1)
			{
				Analytics.batch_plugin_types();

				Logger.Log($" Batch completed! OSI on {counter:n0} {counter.Plural("plugin", "plugins")}.");
			}

			Community.Runtime.Events.Trigger(CarbonEvent.AllPluginsLoaded, EventArgs.Empty);
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public struct ModPackage
	{
		public Assembly Assembly;
		public Type[] AllTypes;

		[JsonProperty] public string Name;
		[JsonProperty] public string File;
		[JsonProperty] public bool IsCoreMod;
		[JsonProperty] public List<RustPlugin> Plugins;

		public bool IsValid { get; internal set; }
		public readonly int PluginCount => IsValid ? Plugins.Count : default;

		public ModPackage AddPlugin(RustPlugin plugin)
		{
			if (!IsValid || Plugins == null || Plugins.Contains(plugin))
			{
				return this;
			}

			Plugins.Add(plugin);
			return this;
		}
		public ModPackage RemovePlugin(RustPlugin plugin)
		{
			if (!IsValid || Plugins == null || !Plugins.Contains(plugin))
			{
				return this;
			}

			Plugins.Remove(plugin);
			return this;
		}

		public static ModPackage Get(string name, bool isCoreMod, string file = null)
		{
			ModPackage package = default;

			package.Name = name;
			package.File = file;
			package.IsCoreMod = isCoreMod;
			package.Plugins = new();
			package.IsValid = true;

			return package;
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class FailedCompilation
	{
		[JsonProperty] public string File;
		[JsonProperty] public List<Trace> Errors = new();
		[JsonProperty] public List<Trace> Warnings = new();
		public Type RollbackType;

		public void AppendErrors(IEnumerable<Trace> traces)
		{
			Errors.AddRange(traces);
		}
		public void AppendWarnings(IEnumerable<Trace> traces)
		{
			Warnings.AddRange(traces);
		}

		public void SetRollbackType(Type type)
		{
			RollbackType = type;
		}
		public void LoadRollbackType()
		{
			if (RollbackType == null)
			{
				return;
			}

			var existentPlugin = FindPlugin(GetRollbackTypeName());

			if (existentPlugin != null)
			{
				return;
			}

			InitializePlugin(RollbackType, out var plugin, Community.Runtime.Plugins, plugin =>
			{
				Logger.Warn($"Rollback for plugin '{plugin.ToPrettyString()}' due to compilation failure");
			}, precompiled: true);
			plugin.InternalCallHookOverriden = true;
			plugin.IsPrecompiled = false;
		}

		public string GetRollbackTypeName()
		{
			if (RollbackType == null)
			{
				return string.Empty;
			}

			return  RollbackType.GetCustomAttribute<InfoAttribute>()?.Title?.Replace(" ", string.Empty);
		}

		public bool IsValid()
		{
			return Errors != null && Errors.Count > 0;
		}
		public void Clear()
		{
			Errors?.Clear();
			Warnings?.Clear();
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public struct Trace
	{
		[JsonProperty] public string Number;
		[JsonProperty] public string Message;
		[JsonProperty] public int Column;
		[JsonProperty] public int Line;
	}
}
