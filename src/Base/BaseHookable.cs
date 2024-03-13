using System.Diagnostics;
using System.Runtime.CompilerServices;
using Facepunch.Extend;
using Newtonsoft.Json;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Base;

public class BaseHookable
{
	public List<uint> Hooks;
	public List<HookMethodAttribute> HookMethods;
	public List<PluginReferenceAttribute> PluginReferences;

	public Dictionary<uint, List<CachedHook>> HookCache = new();
	public HashSet<uint> IgnoredHooks = new();

	public class CachedHook
	{
		public MethodInfo Method;
		public Type[] Parameters;
		public ParameterInfo[] InfoParameters;
		public bool IsByRef;
		public bool IsAsync;

		public int TimesFired;
		public double HookTime;
		public double MemoryUsage;

		public void Tick()
		{
			TimesFired++;
		}

		public static CachedHook Make(MethodInfo method)
		{
			var parameters = method.GetParameters();
			var isByRef = parameters.Any(x => x.ParameterType.IsByRef);
			var hook = new CachedHook
			{
				Method = method,
				IsByRef = isByRef,
				IsAsync = method.ReturnType?.GetMethod("GetAwaiter") != null ||
						  method.GetCustomAttribute<AsyncStateMachineAttribute>() != null,
				Parameters = parameters.Select(x => x.ParameterType).ToArray(),
				InfoParameters = parameters
			};

			return hook;
		}
	}

	[JsonProperty]
	public string Name { get; set; }

	[JsonProperty]
	public virtual VersionNumber Version { get; set; }

	[JsonProperty]
	public double TotalHookTime { get; internal set; }

	[JsonProperty]
	public double TotalMemoryUsed { get; internal set; }

	[JsonProperty]
	public double Uptime => _initializationTime.GetValueOrDefault();

	public bool HasBuiltHookCache { get; internal set; }
	public bool HasInitialized { get; internal set; }
	public Type Type { get; internal set; }
	public bool InternalCallHookOverriden { get; internal set; } = true;

	#region Tracking

	internal Stopwatch _trackStopwatch = new();
	internal long _currentMemory;
	internal int _currentGcCount;
	internal TimeSince? _initializationTime;

#if DEBUG
	public HookTimeAverage HookTimeAverage { get; protected set; }
	public MemoryAverage MemoryAverage { get; protected set; }
#endif

	public double CurrentHookTime { get; internal set; }
	public static long CurrentMemory => GC.GetTotalMemory(false);
	public static int CurrentGcCount => GC.CollectionCount(0);
	public int CurrentHookFires => HookCache.Sum(x => x.Value.Sum(y => y.TimesFired));
	public bool HasGCCollected => _currentGcCount != CurrentGcCount;

	public virtual void TrackInit()
	{
		if (_initializationTime == null)
		{
			_initializationTime = 0;
		}

#if DEBUG
		if (HookTimeAverage == null)
		{
			HookTimeAverage = new(Community.Runtime.Config.Debugging.PluginTrackingTime);
		}

		if (MemoryAverage == null)
		{
			MemoryAverage = new(Community.Runtime.Config.Debugging.PluginTrackingTime);
		}
#endif
	}
	public virtual void TrackStart()
	{
		if (!Community.IsServerInitialized)
		{
			return;
		}

		var stopwatch = _trackStopwatch;
		if (stopwatch.IsRunning)
		{
			return;
		}
		stopwatch.Start();
		_currentMemory = CurrentMemory;
		_currentGcCount = CurrentGcCount;
	}
	public virtual void TrackEnd()
	{
		if (!Community.IsServerInitialized)
		{
			return;
		}

		var stopwatch = _trackStopwatch;

		if (!stopwatch.IsRunning)
		{
			return;
		}

		CurrentHookTime = stopwatch.Elapsed.TotalMilliseconds;
		var memoryUsed = (CurrentMemory - _currentMemory).Clamp(0, long.MaxValue);

#if DEBUG
		// if (Community.Runtime.Config.PluginTrackingTime != 0)
		// {
		// 	HookTimeAverage?.Increment(timeElapsed);
		// 	MemoryAverage?.Increment(memoryUsed);
		// }
#endif

		TotalHookTime += CurrentHookTime;
		TotalMemoryUsed += memoryUsed;
		stopwatch.Reset();
	}

#endregion

	public virtual async ValueTask OnAsyncServerShutdown()
	{
		await Task.CompletedTask;
	}

	public void BuildHookCache(BindingFlags flag)
	{
		if (HasBuiltHookCache)
		{
			return;
		}

		var hooksPresent = Hooks.Count != 0;

		HookCache.Clear();

		var methods = Type.GetMethods(flag);

		foreach (var method in methods)
		{
			var id = HookStringPool.GetOrAdd(method.Name);

			if (!hooksPresent)
			{
				if (Community.Runtime.HookManager.IsHook(method.Name) && !Hooks.Contains(id))
				{
					Hooks.Add(id);
				}
			}

			if (!HookCache.TryGetValue(id, out var hooks))
			{
				HookCache.Add(id, hooks = new());
			}

			var hook = CachedHook.Make(method);

			if (hooks.Count > 0 && hooks[0].Parameters.Length < hook.Parameters.Length)
			{
				hooks.Insert(0, hook);
			}
			else
			{
				hooks.Add(hook);
			}
		}

		var methodAttributes = Type.GetMethods(flag | BindingFlags.Public);

		foreach (var method in methodAttributes)
		{
			var methodAttribute = method.GetCustomAttribute<HookMethodAttribute>();

			if (methodAttribute == null) continue;

			var id = HookStringPool.GetOrAdd(string.IsNullOrEmpty(methodAttribute.Name) ? method.Name : methodAttribute.Name);

			if (!HookCache.TryGetValue(id, out var hooks))
			{
				HookCache.Add(id, hooks = new());
			}

			if(hooks.Any(x => x.Method == method))
			{
				continue;
			}

			var hook = CachedHook.Make(method);

			if (hooks.Count > 0 && hooks[0].Parameters.Length < hook.Parameters.Length)
			{
				hooks.Insert(0, hook);
			}
			else
			{
				hooks.Add(hook);
			}
		}

		HasBuiltHookCache = true;
		Logger.Debug(Name, $"Built hook cache", 2);
	}
	public virtual object InternalCallHook(uint hook, object[] args)
	{
		InternalCallHookOverriden = false;
		return null;
	}

	public void Subscribe(string hook)
	{
		if (IgnoredHooks == null) return;

		var hash = HookStringPool.GetOrAdd(hook);

		if (!IgnoredHooks.Contains(hash)) return;

		Community.Runtime.HookManager.Subscribe(hook, Name);
		IgnoredHooks.Remove(hash);
	}
	public void Unsubscribe(string hook)
	{
		if (IgnoredHooks == null) return;

		var hash = HookStringPool.GetOrAdd(hook);

		if (IgnoredHooks.Contains(hash)) return;

		Community.Runtime.HookManager.Unsubscribe(hook, Name);
		IgnoredHooks.Add(hash);
	}
	public bool IsHookIgnored(uint hook)
	{
		return IgnoredHooks != null && IgnoredHooks.Contains(hook);
	}

	public void SubscribeAll(Func<string, bool> condition = null)
	{
		foreach (var name in Hooks.Select(hook => HookStringPool.GetOrAdd(hook)).Where(name => condition == null || condition(name)))
		{
			Subscribe(name);
		}
	}
	public void UnsubscribeAll(Func<string, bool> condition = null)
	{
		foreach (var name in Hooks.Select(hook => HookStringPool.GetOrAdd(hook)).Where(name => condition == null || condition(name)))
		{
			Unsubscribe(name);
		}
	}

	public T To<T>()
	{
		if (this is T result)
		{
			return result;
		}

		return default;
	}

	public override string ToString()
	{
		return GetType().FullName;
	}

	public virtual string ToPrettyString()
	{
		return $"{Name} v{Version}";
	}
}
