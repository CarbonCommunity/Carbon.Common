﻿using API.Events;
using Carbon.Profiler;
using Newtonsoft.Json;

namespace Carbon.Core;

public static partial class ModLoader
{
	public static bool IsBatchComplete { get; set; }
	public static PackageBank Packages = new();
	public static Dictionary<string, CompilationResult> FailedCompilations = new();

	internal static Dictionary<string, Type> TypeDictionaryCache { get; } = new();
	internal static Dictionary<string, List<string>> PendingRequirees { get; } = new();
	internal static List<string> PostBatchFailedRequirees { get; } = new();
	internal static bool FirstLoadSinceStartup { get; set; } = true;

	internal const string CARBON_PLUGIN = "CarbonPlugin";
	internal const string RUST_PLUGIN = "RustPlugin";
	internal const string COVALENCE_PLUGIN = "CovalencePlugin";

	public static CompilationResult GetOrCreateFailedCompilation(string file, bool clear = false)
	{
		if (!FailedCompilations.TryGetValue(file, out var result))
		{
			FailedCompilations[file] = result = CompilationResult.Create(file);
		}

		if (clear)
		{
			result.Clear();
		}

		return result;
	}
	public static void RegisterPackage(Package package)
	{
		if (!Packages.Contains(package))
		{
			Packages.Add(package);
		}
	}
	public static Package GetPackage(string name)
	{
		return Packages.FirstOrDefault(mod => mod.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
	}
	public static RustPlugin FindPlugin(string name)
	{
		return Packages.SelectMany(package => package.Plugins).FirstOrDefault(plugin => plugin.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	static ModLoader()
	{
		Community.Runtime.Events.Subscribe(CarbonEvent.OnServerInitialized, _ => OnPluginProcessFinished());
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
		if (PostBatchFailedRequirees.Contains(requiree))
		{
			return;
		}

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

		var list = Facepunch.Pool.GetList<Package>();
		list.AddRange(Packages);

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

	public static void InitializePlugins(Package mod)
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
	public static void UninitializePlugins(Package mod)
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

	public static bool InitializePlugin(Type type, out RustPlugin plugin, Package package = default, Action<RustPlugin> preInit = null, bool precompiled = false)
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

		var title = info.Title;
		var author = info.Author;
		var version = info.Version;
		var description = desc == null ? string.Empty : desc.Description;

		var existentPlugin = FindPlugin(title) ?? FindPlugin(type.Name);

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
			HookCaller.CallStaticHook(2684549964, plugin, ex);

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

		plugin.IProcessPatches();
		plugin.ILoad();

		if (!plugin.ManualCommands)
		{
			ProcessCommands(type, plugin);
		}

		Interface.Oxide.RootPluginManager.AddPlugin(plugin);

		var isProfiled = MonoProfiler.IsRecording && Community.Runtime.MonoProfilerConfig.IsWhitelisted(MonoProfilerConfig.ProfileTypes.Plugin, Path.GetFileNameWithoutExtension(plugin.FileName));

		Logger.Log($"{(precompiled ? "Preloaded" : "Loaded")} plugin {plugin.ToPrettyString()}" +
		           $"{(precompiled ? string.Empty : $" [{plugin.CompileTime.TotalMilliseconds:0}ms]")}" +
		           $"{(isProfiled ? " [PROFILING]" : string.Empty)}");

		return true;
	}
	public static bool UninitializePlugin(RustPlugin plugin, bool premature = false)
	{
		if (!premature && !plugin.IsLoaded)
		{
			return true;
		}

		plugin.IProcessUnpatches();
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
			HookCaller.CallStaticHook(1250294368, plugin);
		}

		plugin.Dispose();

		if (!premature)
		{
			if (!plugin.IsPrecompiled)
			{
				Assemblies.Plugins.Eliminate(Path.GetFileNameWithoutExtension(plugin.FilePath));
			}

			Logger.Log($"Unloaded plugin {plugin.ToPrettyString()}");
			Interface.Oxide.RootPluginManager.RemovePlugin(plugin);

			Plugin.InternalApplyAllPluginReferences();
		}

		// plugin.IClearMemory();

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
					Community.Runtime.Core.cmd.AddChatCommand(name, hookable, method, help: string.Empty, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
					Community.Runtime.Core.cmd.AddConsoleCommand(name, hookable, method, help: string.Empty, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
				}
			}

			foreach (var chatCommand in chatCommands)
			{
				Community.Runtime.Core.cmd.AddChatCommand(string.IsNullOrEmpty(prefix) ? chatCommand.Name : $"{prefix}.{chatCommand.Name}", hookable, method, help: chatCommand.Help, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
			}

			foreach (var consoleCommand in consoleCommands)
			{
				Community.Runtime.Core.cmd.AddConsoleCommand(string.IsNullOrEmpty(prefix) ? consoleCommand.Name : $"{prefix}.{consoleCommand.Name}", hookable, method, help: consoleCommand.Help, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: hidden, silent: true);
			}

			foreach (var protectedCommand in protectedCommands)
			{
				Community.Runtime.Core.cmd.AddConsoleCommand(Community.Protect(string.IsNullOrEmpty(prefix) ? protectedCommand.Name : $"{prefix}.{protectedCommand.Name}"), hookable, method, help: protectedCommand.Help, reference: method, permissions: ps, groups: gs, authLevel: authLevel, cooldown: cooldownTime, isHidden: true, silent: true);
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
				var perm = Interface.Oxide.Permission;

				foreach (var permission in ps)
				{
					if (!perm.PermissionExists(permission, hookable))
					{
						perm.RegisterPermission(permission, hookable);
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
				Community.Runtime.Core.cmd.AddConsoleCommand(string.IsNullOrEmpty(prefix) ? var.Name : $"{prefix}.{var.Name}", hookable, (player, command, args) =>
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
				Community.Runtime.Core.cmd.AddConsoleCommand(string.IsNullOrEmpty(prefix) ? var.Name : $"{prefix}.{var.Name}", hookable, (player, command, args) =>
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

		if (temp.Count == 0)
		{
			IsBatchComplete = true;
		}

		temp.Clear();
		Facepunch.Pool.FreeList(ref temp);

		if (ConVar.Global.skipAssetWarmup_crashes)
		{
			Community.Runtime.MarkServerInitialized(true);
		}

		if (!Community.IsServerInitialized)
		{
			return;
		}

		var counter = 0;
		var plugins = Facepunch.Pool.GetList<RustPlugin>();
		plugins.AddRange(Packages.SelectMany(mod => mod.Plugins));

		foreach (var plugin in plugins)
		{
			try
			{
				plugin.InternalApplyPluginReferences();
			}
			catch(Exception exception)
			{
				Logger.Error($"Failed applying PluginReferences for '{plugin.ToPrettyString()}'", exception);
			}
		}

		foreach (var plugin in plugins.Where(plugin => !plugin.HasInitialized))
		{
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
