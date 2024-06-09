﻿using System.Diagnostics;
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

	public class HookCachePool : Dictionary<uint, CachedHookInstance>
	{
		public void Reset()
		{
			foreach (var hook in Values.SelectMany(value => value.Hooks))
			{
				hook.Reset();
			}
		}

		public void EnableDebugging(bool wants)
		{
			foreach (var hook in Values.SelectMany(value => value.Hooks))
			{
				hook.EnableDebugging(wants);
			}
		}
	}

	public class CachedHookInstance
	{
		public CachedHook PrimaryHook;
		public List<CachedHook> Hooks;

		public bool IsValid() => Hooks != null && Hooks.Any();
		public void RefreshPrimary()
		{
			PrimaryHook = Hooks.OrderByDescending(x => x.Parameters.Length).FirstOrDefault();
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

		public void OnFired(BaseHookable hookable, TimeSpan hookTime, double memoryUsed)
		{
			hookable.TotalHookTime += hookTime;
			hookable.TotalMemoryUsed += memoryUsed;
			hookable.TotalHookFires++;

			HookTime += hookTime;
			MemoryUsage += memoryUsed;

			TimesFired++;

			if (IsDebugged)
			{
				Logger.Log($" {Name}[{Id}] fired on {HookableName} {Hookable.ToPrettyString()} [{TimesFired:n0}|{HookTime.TotalMilliseconds:0}ms|{ByteEx.Format(MemoryUsage, shortName: true, stringFormat: "{0}{1}").ToLower()}]");
			}
		}
		public void OnLagSpike(BaseHookable hookable)
		{
			hookable.TotalHookLagSpikes++;
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
#if DEBUG
				IsDebugged = CorePlugin.EnforceHookDebugging,
#endif
			};

			return hook;
		}
	}

	public virtual bool ManualSubscriptions => false;

	[JsonProperty]
	public string Name { get; set; }

	[JsonProperty]
	public virtual VersionNumber Version { get; set; }

	[JsonProperty]
	public TimeSpan TotalHookTime { get; internal set; }

	[JsonProperty]
	public int TotalHookFires { get; internal set; }

	[JsonProperty]
	public double TotalMemoryUsed { get; internal set; }

	[JsonProperty]
	public int TotalHookLagSpikes { get; internal set; }

	[JsonProperty]
	public double Uptime => _initializationTime.GetValueOrDefault();

	public bool HasBuiltHookCache { get; internal set; }
	public bool HasInitialized { get; internal set; }
	public Type HookableType { get; internal set; }
	public bool InternalCallHookOverriden { get; internal set; } = true;

	#region Tracking

	internal Stopwatch _trackStopwatch = new();
	internal int _currentGcCount;
	internal TimeSince? _initializationTime;

	public TimeSpan CurrentHookTime { get; internal set; }
	public static long CurrentMemory => GC.GetTotalMemory(false);
	public static int CurrentGcCount => GC.CollectionCount(0);
	public bool HasGCCollected => _currentGcCount != CurrentGcCount;

	public virtual void TrackInit()
	{
		if (_initializationTime == null)
		{
			_initializationTime = 0;
		}
	}
	public virtual void TrackStart()
	{
		if (!Community.IsServerInitialized || _trackStopwatch.IsRunning)
		{
			return;
		}

		_trackStopwatch.Start();
		_currentGcCount = CurrentGcCount;
	}
	public virtual void TrackEnd()
	{
		if (!Community.IsServerInitialized || !_trackStopwatch.IsRunning)
		{
			return;
		}

		CurrentHookTime = _trackStopwatch.Elapsed;

#if DEBUG
		// if (Community.Runtime.Config.PluginTrackingTime != 0)
		// {
		// 	HookTimeAverage?.Increment(timeElapsed);
		// 	MemoryAverage?.Increment(memoryUsed);
		// }
#endif

		_trackStopwatch.Reset();
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

		var methods = HookableType.GetMethods(flag);

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

			if (!HookPool.TryGetValue(id, out var instance))
			{
				instance = new();
				instance.Hooks = new(5);

				HookPool.Add(id, instance);
			}

			instance.Hooks.Add(CachedHook.Make(method.Name, id, this, method));
			instance.RefreshPrimary();
		}

		var methodAttributes = HookableType.GetMethods(flag | BindingFlags.Public);

		foreach (var method in methodAttributes)
		{
			var methodAttribute = method.GetCustomAttribute<HookMethodAttribute>();

			if (methodAttribute == null) continue;

			var id = HookStringPool.GetOrAdd(string.IsNullOrEmpty(methodAttribute.Name) ? method.Name : methodAttribute.Name);

			if (!HookPool.TryGetValue(id, out var instance))
			{
				instance = new();
				instance.Hooks = new(5);

				HookPool.Add(id, instance);
			}

			if(instance.Hooks.Any(x => x.Method == method))
			{
				continue;
			}

			instance.Hooks.Add(CachedHook.Make(method.Name, id, this, method));
			instance.RefreshPrimary();
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
