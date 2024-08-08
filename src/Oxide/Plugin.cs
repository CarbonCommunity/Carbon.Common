using Facepunch;
using Newtonsoft.Json;
using Logger = Carbon.Logger;

namespace Oxide.Core.Plugins;

[JsonObject(MemberSerialization.OptIn)]
public class Plugin : BaseHookable, IDisposable
{
	public PluginManager Manager { get; set; }
	public Persistence persistence;

	public Command cmd;
	public Permission permission;

	public class Persistence : FacepunchBehaviour { }

	public bool IsCorePlugin { get; set; }

	[JsonProperty]
	public string Title { get; set; }
	[JsonProperty]
	public string Description { get; set; }
	[JsonProperty]
	public string Author { get; set; }
	public int ResourceId { get; set; }
	public bool HasConfig { get; set; }
	public bool HasMessages { get; set; }
	public bool HasConditionals { get; set; }

	[JsonProperty]
	public TimeSpan CompileTime { get; set; }
	[JsonProperty]
	public TimeSpan InternalCallHookGenTime { get; set; }
	[JsonProperty]
	public ModLoader.Trace[] CompileWarnings { get; set; }

	public string InternalCallHookSource { get; set; }

	[JsonProperty]
	public string FilePath { get; set; }
	[JsonProperty]
	public string FileName { get; set; }

	public string Filename => FileName;

	public virtual bool ManualCommands => false;

	public override void TrackStart()
	{
		if (IsCorePlugin) return;

		base.TrackStart();
	}
	public override void TrackEnd()
	{
		if (IsCorePlugin) return;

		base.TrackEnd();
	}

	public Plugin[] Requires { get; set; }

	public ModLoader.Package Package;
	public IBaseProcessor Processor;
	public IBaseProcessor.IProcess ProcessorProcess;

	public static implicit operator bool(Plugin target)
	{
		return target != null && target.HasInitialized;
	}

	public virtual bool IInit()
	{
		BuildHookCache(BindingFlags.NonPublic | BindingFlags.Instance);

		using (TimeMeasure.New($"Processing PluginReferences on '{this}'"))
		{
			if (!InternalApplyPluginReferences())
			{
				Logger.Warn($"Failed vibe check {ToPrettyString()}");
				return false;
			}
		}
		Carbon.Logger.Debug(Name, "Assigned plugin references");

		if (Hooks != null && !ManualSubscriptions)
		{
			string requester = FileName ?? $"{this}";
			using (TimeMeasure.New($"Processing Hooks on '{ToPrettyString()}'"))
			{
				foreach (var hook in Hooks)
				{
					Community.Runtime.HookManager.Subscribe(HookStringPool.GetOrAdd(hook), requester);
				}

			}
			Carbon.Logger.Debug(Name, "Processed hooks");
		}

		CallHook("Init");

		TrackInit();

		return true;
	}
	internal virtual void ILoad()
	{
		using (TimeMeasure.New($"Load on '{ToPrettyString()}'"))
		{
			IsLoaded = true;
			CallHook("OnLoaded");
			CallHook("Loaded");
		}

		using (TimeMeasure.New($"Load.PendingRequirees on '{ToPrettyString()}'"))
		{
			var requirees = ModLoader.GetRequirees(this);

			if (requirees != null)
			{
				foreach (var requiree in requirees)
				{
					Logger.Warn($" [{Name}] Loading '{Path.GetFileNameWithoutExtension(requiree)}' to parent's request: '{ToPrettyString()}'");
					Community.Runtime.ScriptProcessor.Prepare(requiree);
				}

				ModLoader.ClearPendingRequirees(this);
			}
		}

		Load();
	}
	public virtual void Load()
	{

	}
	public virtual void IUnload()
	{
		try
		{
			using (TimeMeasure.New($"IUnload.UnprocessHooks on '{this}'"))
			{
				if (Hooks != null)
				{
					foreach (var hook in Hooks)
					{
						Community.Runtime.HookManager.Unsubscribe(HookStringPool.GetOrAdd(hook), FileName);
					}
					Carbon.Logger.Debug(Name, $"Unprocessed hooks");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed calling Plugin.IUnload.UnprocessHooks on {this}", ex);
		}

		try
		{
			using (TimeMeasure.New($"IUnload.Disposal on '{this}'"))
			{
				IgnoredHooks?.Clear();
				HookPool?.Clear();
				Hooks?.Clear();
				HookMethods?.Clear();
				PluginReferences?.Clear();

				IgnoredHooks = null;
				HookPool = null;
				Hooks = null;
				HookMethods = null;
				PluginReferences = null;
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed calling Plugin.IUnload.Disposal on {this}", ex);
		}

		HasInitialized = false;
	}

	internal bool InternalApplyPluginReferences()
	{
		if (PluginReferences == null) return true;

		foreach (var attribute in PluginReferences)
		{
			var field = attribute.Field;
			var name = string.IsNullOrEmpty(attribute.Name) ? field.Name : attribute.Name;
			var path = Path.Combine(Defines.GetScriptsFolder(), $"{name}.cs");

			try
			{
				var plugin = (Plugin)null;
				if (field.FieldType.Name != nameof(Plugin) &&
				    field.FieldType.Name != nameof(RustPlugin) &&
				    field.FieldType.Name != nameof(CovalencePlugin) &&
				    field.FieldType.Name != nameof(CarbonPlugin))
				{
					var info = field.FieldType.GetCustomAttribute<InfoAttribute>();
					if (info == null)
					{
						Carbon.Logger.Warn(
							$"You're trying to reference a non-plugin instance: {name}[{field.FieldType.Name}]");
						continue;
					}

					plugin = Community.Runtime.Core.plugins.Find(info.Title);
				}
				else
				{
					plugin = Community.Runtime.Core.plugins.Find(name);
				}

				if (plugin != null)
				{
					var version = new VersionNumber(attribute.MinVersion);

					if (version.IsValid() && plugin.Version < version)
					{
						Logger.Warn(
							$"Plugin '{Name} by {Author} v{Version}' references a required plugin which is outdated: {plugin.Name} by {plugin.Author} v{plugin.Version} < v{version}");
						return false;
					}
					else
					{
						field.SetValue(this, plugin);

						if (attribute.IsRequired)
						{
							ModLoader.AddPendingRequiree(plugin, this);
						}
					}
				}
				else
				{
					field.SetValue(this, null);

					if (attribute.IsRequired)
					{
						ModLoader.PostBatchFailedRequirees.Add(FilePath);
						ModLoader.AddPendingRequiree(path, FilePath);
						Logger.Warn(
							$"Plugin '{Name} by {Author} v{Version}' references a required plugin which is not loaded: {name}");
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Plugin '{ToPrettyString()}' failed to assign PluginReference on field {name} ({field.FieldType.Name})", ex);
			}
		}

		return true;
	}
	internal bool IUnloadDependantPlugins()
	{
		try
		{
			using (TimeMeasure.New($"IUnload.UnloadRequirees on '{ToPrettyString()}'"))
			{
				var mods = Pool.GetList<ModLoader.Package>();
				mods.AddRange(ModLoader.Packages);
				var plugins = Pool.GetList<Plugin>();

				foreach (var mod in ModLoader.Packages)
				{
					plugins.Clear();
					plugins.AddRange(mod.Plugins);

					foreach (Plugin plugin in plugins.Where(plugin => plugin.Requires != null && plugin.Requires.Contains(this)))
					{
						switch (plugin.Processor)
						{
							case IScriptProcessor script:
							{
								Logger.Warn($" [{Name}] Unloading '{plugin.ToPrettyString()}' because parent '{ToPrettyString()}' has been unloaded.");
								ModLoader.AddPendingRequiree(this, plugin);

								script.Get<IScriptProcessor.IScript>(plugin.FileName)?.Dispose();

								if (plugin is RustPlugin rustPlugin)
								{
									ModLoader.UninitializePlugin(rustPlugin);
								}

								break;
							}
						}
					}
				}

				Pool.FreeList(ref mods);
				Pool.FreeList(ref plugins);
			}

			return true;
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed calling Plugin.IUnload.UnloadRequirees on {ToPrettyString()}", ex);
			return false;
		}
	}
	internal bool IClearMemory()
	{
		try
		{
			foreach (var member in HookableType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (member.IsLiteral) continue;

				member.SetValue(null, null);
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Plugin '{ToPrettyString()}' failed clearing memory.", ex);
		}

		return true;
	}

	public static void InternalApplyAllPluginReferences()
	{
		var list = Pool.GetList<RustPlugin>();

		foreach (var package in ModLoader.Packages)
		{
			foreach (var plugin in package.Plugins)
			{
				if (!plugin.InternalApplyPluginReferences())
				{
					list.Add(plugin);
				}
			}
		}

		foreach (var plugin in list)
		{
			ModLoader.UninitializePlugin(plugin);
		}

		Pool.FreeList(ref list);
	}

	public void SetProcessor(IBaseProcessor processor)
	{
		Processor = processor;
	}

	#region Calls

	public T Call<T>(string hook)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook));
	}
	public T Call<T>(string hook, object arg1)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1);
	}
	public T Call<T>(string hook, object arg1, object arg2)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}
	public T Call<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}
	public T Call<T>(string hook, object[] args)
	{
		return args.Length switch
		{
			1 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0]),
			2 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1]),
			3 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2]),
			4 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3]),
			5 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4]),
			6 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5]),
			7 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6]),
			8 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]),
			9 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]),
			10 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]),
			11 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]),
			12 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]),
			13 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[13]),
			_ => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook)),
		};
	}

	public object Call(string hook)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook));
	}
	public object Call(string hook, object arg1)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1);
	}
	public object Call(string hook, object arg1, object arg2)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2);
	}
	public object Call(string hook, object arg1, object arg2, object arg3)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}
	public object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
	}
	public object Call(string hook, object[] args)
	{
		return args?.Length switch
		{
			1 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0]),
			2 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1]),
			3 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2]),
			4 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3]),
			5 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4]),
			6 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5]),
			7 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6]),
			8 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]),
			9 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]),
			10 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]),
			11 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]),
			12 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]),
			13 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12]),
			_ => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook)),
		};
	}

	public T CallHook<T>(string hook)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook));
	}
	public T CallHook<T>(string hook, object arg1)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1);
	}
	public T CallHook<T>(string hook, object arg1, object arg2)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}
	public T CallHook<T>(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		return HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
	}
	public T CallHook<T>(string hook, object[] args)
	{
		return args.Length switch
		{
			1 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0]),
			2 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1]),
			3 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2]),
			4 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3]),
			5 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4]),
			6 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5]),
			7 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6]),
			8 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]),
			9 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]),
			10 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]),
			11 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]),
			12 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]),
			13 => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[13]),
			_ => HookCaller.CallHook<T>(this, HookStringPool.GetOrAdd(hook)),
		};
	}

	public object CallHook(string hook)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook));
	}
	public object CallHook(string hook, object arg1)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1);
	}
	public object CallHook(string hook, object arg1, object arg2)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}
	public object CallHook(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		return HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
	}
	public object CallHook(string hook, object[] args)
	{
		return args?.Length switch
		{
			1 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0]),
			2 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1]),
			3 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2]),
			4 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3]),
			5 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4]),
			6 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5]),
			7 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6]),
			8 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]),
			9 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]),
			10 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]),
			11 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]),
			12 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]),
			13 => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook), args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12]),
			_ => HookCaller.CallHook(this, HookStringPool.GetOrAdd(hook)),
		};
	}

	#endregion

	#region Compatibility

	public object OnAddedToManager;
	public object OnRemovedFromManager;

	public virtual void HandleAddedToManager(PluginManager manager) { }
	public virtual void HandleRemovedFromManager(PluginManager manager) { }

	protected void AddCovalenceCommand(string[] commands, string callback, string perm)
	{
		foreach (var command in commands)
		{
			cmd.AddCovalenceCommand(command, this, callback, permissions: new string[] { perm });
		}

		if (!string.IsNullOrEmpty(perm))
		{
			if (!this.permission.PermissionExists(perm))
			{
				this.permission.RegisterPermission(perm, this);
			}
		}
	}

	#endregion

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
	public void QueueWorkerThread(Action<object> callback)
	{
		ThreadPool.QueueUserWorkItem(context =>
		{
			try
			{
				callback(context);
			}
			catch (Exception ex)
			{
				Carbon.Logger.Error($"Worker thread callback failed in '{Name} v{Version}'", ex);
			}
		});
	}

	public DynamicConfigFile Config { get; internal set; }

	public bool IsLoaded { get; set; }

	protected virtual void LoadConfig()
	{
		Config = new DynamicConfigFile(Path.Combine(Manager.ConfigPath, Name + ".json"));

		if (!Config.Exists(null))
		{
			LoadDefaultConfig();
			SaveConfig();
		}
		try
		{
			if (Config.Exists(null)) Config.Load(null);
		}
		catch (Exception ex)
		{
			Carbon.Logger.Error("Failed to load config file (is the config file corrupt?)", ex);
		}
	}
	protected virtual void LoadDefaultConfig()
	{
	}
	protected virtual void SaveConfig()
	{
		if (Config == null)
		{
			return;
		}
		try
		{
			if (Config.Count() > 0) Config.Save(null);
		}
		catch (Exception ex)
		{
			Carbon.Logger.Error("Failed to save config file (does the config have illegal objects in it?) (" + ex.Message + ")", ex);
		}
	}

	protected virtual void LoadDefaultMessages()
	{

	}

	public virtual void Dispose()
	{
		IsLoaded = false;
	}
}
