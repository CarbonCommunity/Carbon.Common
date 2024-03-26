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

	public HookCachePool HookPool = new();
	public List<uint> IgnoredHooks = new();

	public class HookCachePool : Dictionary<uint, List<CachedHook>>
	{
		public void Reset()
		{
			foreach (var hook in Values.SelectMany(value => value))
			{
				hook.Reset();
			}
		}

		public void EnableDebugging(bool wants)
		{
			foreach (var hook in Values.SelectMany(value => value))
			{
				hook.EnableDebugging(wants);
			}
		}
	}

	public class CachedHook
	{
		public string Name;
		public uint Id;
		public BaseHookable Hookable;
		public string HookableName;
		public MethodInfo Method;
		public Type[] Parameters;
		public ParameterInfo[] InfoParameters;
		public bool IsByRef;
		public bool IsAsync;
		public bool IsDebugged;
		public bool IsValid;

		public int LagSpikes;
		public int TimesFired;
		public TimeSpan HookTime;
		public double MemoryUsage;

		public void EnableDebugging(bool wants)
		{
			IsDebugged = wants;
		}

		public void Reset()
		{
			LagSpikes = 0;
			TimesFired = 0;
			HookTime = default;
			MemoryUsage = 0;
		}

		public void OnFired(TimeSpan hookTime, double memoryUsed)
		{
			if (!IsValid)
			{
				return;
			}

			HookTime += hookTime;
			MemoryUsage += memoryUsed;

			TimesFired++;

			if (IsDebugged)
			{
				Logger.Log($" {Name}[{Id}] fired on {HookableName} {Hookable.ToPrettyString()} [{TimesFired:n0}|{HookTime.TotalMilliseconds:0}ms|{ByteEx.Format(MemoryUsage, shortName: true, stringFormat: "{0}{1}").ToLower()}]");
			}
		}
		public void OnLagSpike()
		{
			LagSpikes++;
		}

		public static CachedHook Make(string hookName, uint hookId, BaseHookable hookable, MethodInfo method)
		{
			var parameters = method.GetParameters();
			var hook = new CachedHook
			{
				Name = hookName,
				Id = hookId,
				Hookable = hookable,
				HookableName = hookable is BaseModule ? "module" : "plugin",
				Method = method,
				IsByRef = parameters.Any(x => x.ParameterType.IsByRef),
				IsAsync = method.ReturnType?.GetMethod("GetAwaiter") != null ||
						  method.GetCustomAttribute<AsyncStateMachineAttribute>() != null,
				Parameters = parameters.Select(x => x.ParameterType).ToArray(),
				InfoParameters = parameters,
				IsDebugged = CorePlugin.EnforceHookDebugging,
				IsValid = true
			};

			return hook;
		}
	}

	[JsonProperty]
	public string Name { get; set; }

	[JsonProperty]
	public virtual VersionNumber Version { get; set; }

	[JsonProperty]
	public TimeSpan TotalHookTime { get; internal set; }

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

	public TimeSpan CurrentHookTime { get; internal set; }
	public static long CurrentMemory => GC.GetTotalMemory(false);
	public static int CurrentGcCount => GC.CollectionCount(0);
	public int CurrentHookFires => HookPool.Sum(x => x.Value.Sum(y => y.TimesFired));
	public int CurrentLagSpikes => HookPool.Sum(x => x.Value.Sum(y => y.LagSpikes));
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

		CurrentHookTime = stopwatch.Elapsed;
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

		HookPool.Clear();

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

			if (!HookPool.TryGetValue(id, out var hooks))
			{
				HookPool.Add(id, hooks = new());
			}

			hooks.Add(CachedHook.Make(method.Name, id, this, method));
		}

		var methodAttributes = Type.GetMethods(flag | BindingFlags.Public);

		foreach (var method in methodAttributes)
		{
			var methodAttribute = method.GetCustomAttribute<HookMethodAttribute>();

			if (methodAttribute == null) continue;

			var id = HookStringPool.GetOrAdd(string.IsNullOrEmpty(methodAttribute.Name) ? method.Name : methodAttribute.Name);

			if (!HookPool.TryGetValue(id, out var hooks))
			{
				HookPool.Add(id, hooks = new());
			}

			if(hooks.Any(x => x.Method == method))
			{
				continue;
			}

			hooks.Add(CachedHook.Make(method.Name, id, this, method));
		}

		HasBuiltHookCache = true;
		Logger.Debug(Name, $"Built hook cache", 2);

		InternalCallHook(0, null);
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
