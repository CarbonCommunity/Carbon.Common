﻿using System.Diagnostics;
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

	public struct CachedHook
	{
		public MethodInfo Method;
		public Type[] Parameters;
		public bool IsByRef;
		public bool IsAsync;

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
	public HookTimeAverage MemoryAverage { get; protected set; }
#endif

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

		var timeElapsed = stopwatch.Elapsed.TotalMilliseconds;
		var memoryUsed = (CurrentMemory - _currentMemory).Clamp(0, long.MaxValue);

#if DEBUG
		// if (Community.Runtime.Config.PluginTrackingTime != 0)
		// {
		// 	HookTimeAverage?.Increment(timeElapsed);
		// 	MemoryAverage?.Increment(memoryUsed);
		// }
#endif

		TotalHookTime += timeElapsed;
		TotalMemoryUsed += memoryUsed;
		stopwatch.Stop();
		stopwatch.Reset();
	}

#endregion

	public virtual async ValueTask OnAsyncServerShutdown()
	{
		await Task.CompletedTask;
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
