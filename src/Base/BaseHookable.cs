using System.Diagnostics;
using System.Runtime.CompilerServices;
using Facepunch.Extend;
using Newtonsoft.Json;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
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
	public Dictionary<uint, List<CachedHook>> HookMethodAttributeCache = new();
	public HashSet<uint> IgnoredHooks = new();

	public class CachedHook
	{
		public MethodInfo Method;
		public Type[] Parameters;
		public object[] DefaultParameterValues;
		public bool IsByRef;
		public bool IsAsync;

		public double HookTime;
		public double MemoryUsage;

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
				DefaultParameterValues = parameters.Select(x => x.DefaultValue).ToArray()
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
			HookTimeAverage = new(Community.Runtime.Config.PluginTrackingTime);
		}

		if (MemoryAverage == null)
		{
			MemoryAverage = new(Community.Runtime.Config.PluginTrackingTime);
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
		HookMethodAttributeCache.Clear();

		var methods = Type.GetMethods(flag);

		foreach (var method in methods)
		{
			var id = HookStringPool.GetOrAdd(method.Name);

			if (!hooksPresent)
			{
				if (Community.Runtime.HookManager.IsHookLoaded(method.Name) && !Hooks.Contains(id))
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

			if (method.HasAttribute(typeof(HookMethodAttribute)))
			{
				if (!HookMethodAttributeCache.TryGetValue(id, out var hooks2))
				{
					HookMethodAttributeCache.Add(id, hooks2 = new());
				}

				if (hooks2.Count > 0 && hooks2[0].Parameters.Length < hook.Parameters.Length)
				{
					hooks2.Insert(0, hook);
				}
				else
				{
					hooks2.Add(hook);
				}
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

	public void Unsubscribe(string hook)
	{
		if (IgnoredHooks == null) return;

		var hash = HookStringPool.GetOrAdd(hook);

		if (IgnoredHooks.Contains(hash)) return;

		IgnoredHooks.Add(hash);
	}
	public void Subscribe(string hook)
	{
		if (IgnoredHooks == null) return;

		var hash = HookStringPool.GetOrAdd(hook);

		if (!IgnoredHooks.Contains(hash)) return;

		IgnoredHooks.Remove(hash);
	}
	public bool IsHookIgnored(uint hook)
	{
		return IgnoredHooks != null && IgnoredHooks.Contains(hook);
	}

	public void SubscribeAll(Func<string, bool> condition = null)
	{
		foreach (var hook in Hooks)
		{
			var name = HookStringPool.GetOrAdd(hook);
			if (condition != null && !condition(name)) continue;

			Subscribe(name);
		}
	}
	public void UnsubscribeAll(Func<string, bool> condition = null)
	{
		foreach (var hook in Hooks)
		{
			var name = HookStringPool.GetOrAdd(hook);
			if (condition != null && !condition(name)) continue;

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
}
