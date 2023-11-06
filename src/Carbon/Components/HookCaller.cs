using System.Diagnostics;
using System.Text;
using Carbon.Base.Interfaces;
using ConVar;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Carbon.HookCallerCommon;
using Pool = Facepunch.Pool;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon;

public class HookCallerCommon
{
	public Dictionary<int, object[]> _argumentBuffer = new();
	public Dictionary<uint, double> _hookTimeBuffer = new();
	public Dictionary<uint, double> _hookTotalTimeBuffer = new();
	public Dictionary<uint, DateTime> _lastDeprecatedWarningAt = new();

	public virtual void AppendHookTime(uint hook, double time) { }
	public virtual void ClearHookTime(uint hook) { }

	public virtual object[] AllocateBuffer(int count) => null;
	public virtual object[] RescaleBuffer(object[] oldBuffer, int newScale, BaseHookable.CachedHook hook) => null;
	public virtual void ProcessDefaults(object[] buffer, BaseHookable.CachedHook hook) { }
	public virtual void ClearBuffer(object[] buffer) { }

	public virtual object CallHook<T>(T hookable, uint hookId, BindingFlags flags, object[] args, bool keepArgs = false) where T : BaseHookable => null;
	public virtual object CallDeprecatedHook<T>(T plugin, uint oldHookId, uint newHookId, DateTime expireDate, BindingFlags flags, object[] args) where T : BaseHookable => null;

	public struct Conflict
	{
		public BaseHookable Hookable;
		public uint Hook;
		public object Result;

		public static Conflict Make(BaseHookable hookable, uint hook, object result) => new()
		{
			Hookable = hookable,
			Hook = hook,
			Result = result
		};
	}
}

public static class HookCaller
{
	public static HookCallerCommon Caller { get; set; }

	public static List<Conflict> ConflictCache = new(10);

	#region Internals

	public static readonly string[] InternalHooks = new string[]
	{
		"OnPluginLoaded",
		"OnPluginUnloaded",
		"CanClientLogin",
		"CanUserLogin",
		"OnUserApprove",
		"OnUserApproved",
		"OnPlayerChat",
		"OnUserChat",
		"OnPlayerOfflineChat",
		"OnPermissionRegistered",
		"OnPermissionsUnregistered",
		"OnGroupPermissionGranted",
		"OnGroupPermissionRevoked",
		"OnGroupCreated",
		"OnGroupDeleted",
		"OnGroupTitleSet",
		"OnGroupRankSet",
		"OnGroupParentSet",
		"CanUseUI",
		"OnDestroyUI",
		"OnUserNameUpdated"
	};

	#endregion

	public static double GetHookTime(uint hook)
	{
		if (!Caller._hookTimeBuffer.TryGetValue(hook, out var total))
		{
			return 0;
		}

		return total;
	}
	public static double GetHookTotalTime(uint hook)
	{
		if (!Caller._hookTotalTimeBuffer.TryGetValue(hook, out var total))
		{
			return 0;
		}

		return total;
	}

	private static object CallStaticHook(uint hookId, BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, object[] args = null, bool keepArgs = false)
	{
		if (Community.Runtime == null || Community.Runtime.ModuleProcessor == null) return null;

		Caller.ClearHookTime(hookId);

		var result = (object)null;
		var array = args == null ? null : keepArgs ? args : args.ToArray();

		for(int i = 0; i < Community.Runtime.ModuleProcessor.Modules.Count; i++)
		{
			var hookable = Community.Runtime.ModuleProcessor.Modules[i];

			if (hookable is IModule modules && !modules.GetEnabled()) continue;

			var methodResult = Caller.CallHook(hookable, hookId, flags: flag, args: array, keepArgs);

			if (methodResult == null) continue;

			result = methodResult;
			ResultOverride(hookable, hookId, result);
		}

		for (int i = 0; i < ModLoader.LoadedPackages.Count; i++)
		{
			var package = ModLoader.LoadedPackages[i];

			for(int o = 0; o < package.Plugins.Count; o++)
			{
				var plugin = package.Plugins[o];

				try
				{
					var methodResult = Caller.CallHook(plugin, hookId, flags: flag, args: array, keepArgs);

					if (methodResult == null) continue;

					result = methodResult;
					ResultOverride(plugin, hookId, result);
				}
				catch (Exception ex)
				{
					var exception = ex.InnerException ?? ex;
					var readableHook = HookStringPool.GetOrAdd(hookId);
					Logger.Error($"Failed to call hook '{readableHook}' on plugin '{plugin.Name} v{plugin.Version}'", exception);
				}
			}
		}

		ConflictCheck(ref result, hookId);

		if (array != null && !keepArgs) Array.Clear(array, 0, array.Length);

		return result;
	}
	private static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Static, object[] args = null)
	{
		if (expireDate < DateTime.Now)
		{
			return null;
		}

		DateTime now = DateTime.Now;

		if (!Caller._lastDeprecatedWarningAt.TryGetValue(oldHookId, out DateTime lastWarningAt) || (now - lastWarningAt).TotalSeconds > 3600f)
		{
			Caller._lastDeprecatedWarningAt[oldHookId] = now;

			Carbon.Logger.Warn($"A plugin is using deprecated hook '{oldHookId}', which will stop working on {expireDate.ToString("D")}. Please ask the author to update to '{newHookId}'");
		}

		return CallStaticHook(oldHookId, flag, args);
	}

	public static void ResultOverride(BaseHookable hookable, uint hookId, object result)
	{
		ConflictCache.Add(Conflict.Make(hookable, hookId, result));
	}
	public static void ConflictCheck(ref object result, uint hookId)
	{
		if (ConflictCache.Count <= 1) return;

		var localResult = result = ConflictCache[0].Result;
		var differentResults =  ConflictCache.Any(conflict => conflict.Result != null && localResult != null && conflict.Result.ToString() != localResult.ToString());

		if (differentResults)
		{
			var readableHook = HookStringPool.GetOrAdd(hookId);
			Logger.Warn($" Hook conflict while calling '{readableHook}[{hookId}]': {ConflictCache.Where(x => x.Result != null).Select(x => $"{x.Hookable.Name} {x.Hookable.Version} [{x.Result}]").ToString(", ", " and ")}");
			result = null;
		}

		ConflictCache.Clear();
	}

	#region Hook Overrides

	public static object CallHook(BaseHookable plugin, uint hookId)
	{
		var buffer = Caller.AllocateBuffer(0);
		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId)
	{
		var buffer = Caller.AllocateBuffer(0);
		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate)
	{
		var buffer = Caller.AllocateBuffer(0);
		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1)
	{
		var buffer = Caller.AllocateBuffer(1);
		buffer[0] = arg1;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1)
	{
		var buffer = Caller.AllocateBuffer(1);
		buffer[0] = arg1;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1)
	{
		var buffer = Caller.AllocateBuffer(1);
		buffer[0] = arg1;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2)
	{
		var buffer = Caller.AllocateBuffer(2);
		buffer[0] = arg1;
		buffer[1] = arg2;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2)
	{
		var buffer = Caller.AllocateBuffer(2);
		buffer[0] = arg1;
		buffer[1] = arg2;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2)
	{
		var buffer = Caller.AllocateBuffer(2);
		buffer[0] = arg1;
		buffer[1] = arg2;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3)
	{
		var buffer = Caller.AllocateBuffer(3);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3)
	{
		var buffer = Caller.AllocateBuffer(3);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3)
	{
		var buffer = Caller.AllocateBuffer(3);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4)
	{
		var buffer = Caller.AllocateBuffer(4);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4)
	{
		var buffer = Caller.AllocateBuffer(4);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4)
	{
		var buffer = Caller.AllocateBuffer(4);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		var buffer = Caller.AllocateBuffer(5);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		var buffer = Caller.AllocateBuffer(5);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		var buffer = Caller.AllocateBuffer(5);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		var buffer = Caller.AllocateBuffer(6);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		var buffer = Caller.AllocateBuffer(6);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		var buffer = Caller.AllocateBuffer(6);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		var buffer = Caller.AllocateBuffer(7);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		var buffer = Caller.AllocateBuffer(7);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		var buffer = Caller.AllocateBuffer(7);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[6] = arg6;
		buffer[7] = arg7;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		var buffer = Caller.AllocateBuffer(8);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		var buffer = Caller.AllocateBuffer(8);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		var buffer = Caller.AllocateBuffer(8);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		var buffer = Caller.AllocateBuffer(9);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		var buffer = Caller.AllocateBuffer(9);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		var buffer = Caller.AllocateBuffer(9);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		var buffer = Caller.AllocateBuffer(10);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		var buffer = Caller.AllocateBuffer(10);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		var buffer = Caller.AllocateBuffer(10);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		var buffer = Caller.AllocateBuffer(11);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		var buffer = Caller.AllocateBuffer(11);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		var buffer = Caller.AllocateBuffer(11);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		var buffer = Caller.AllocateBuffer(12);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		var buffer = Caller.AllocateBuffer(12);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		var buffer = Caller.AllocateBuffer(12);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static object CallHook(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		var buffer = Caller.AllocateBuffer(13);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;
		buffer[12] = arg13;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static T CallHook<T>(BaseHookable plugin, uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		var buffer = Caller.AllocateBuffer(13);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;
		buffer[12] = arg13;

		var result = Caller.CallHook(plugin, hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}
	public static T CallDeprecatedHook<T>(BaseHookable plugin, uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		var buffer = Caller.AllocateBuffer(13);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;
		buffer[12] = arg13;
		buffer[12] = arg13;

		var result = Caller.CallDeprecatedHook(plugin, oldHookId, newHookId, expireDate, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, buffer);

		Caller.ClearBuffer(buffer);
		return (T)result;
	}

	#endregion

	#region Static Hook Overrides

	public static object CallStaticHook(uint hookId)
	{
		var buffer = Caller.AllocateBuffer(0);
		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate)
	{
		var buffer = Caller.AllocateBuffer(0);
		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1)
	{
		var buffer = Caller.AllocateBuffer(1);
		buffer[0] = arg1;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1)
	{
		var buffer = Caller.AllocateBuffer(1);
		buffer[0] = arg1;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2)
	{
		var buffer = Caller.AllocateBuffer(2);
		buffer[0] = arg1;
		buffer[1] = arg2;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2)
	{
		var buffer = Caller.AllocateBuffer(2);
		buffer[0] = arg1;
		buffer[1] = arg2;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3)
	{
		var buffer = Caller.AllocateBuffer(3);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3)
	{
		var buffer = Caller.AllocateBuffer(3);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4)
	{
		var buffer = Caller.AllocateBuffer(4);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4)
	{
		var buffer = Caller.AllocateBuffer(4);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		var buffer = Caller.AllocateBuffer(5);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		var buffer = Caller.AllocateBuffer(5);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		var buffer = Caller.AllocateBuffer(6);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		var buffer = Caller.AllocateBuffer(6);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		var buffer = Caller.AllocateBuffer(7);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		var buffer = Caller.AllocateBuffer(7);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		var buffer = Caller.AllocateBuffer(8);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		var buffer = Caller.AllocateBuffer(8);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		var buffer = Caller.AllocateBuffer(9);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		var buffer = Caller.AllocateBuffer(9);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		var buffer = Caller.AllocateBuffer(10);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		var buffer = Caller.AllocateBuffer(10);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		var buffer = Caller.AllocateBuffer(11);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		var buffer = Caller.AllocateBuffer(11);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		var buffer = Caller.AllocateBuffer(12);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		var buffer = Caller.AllocateBuffer(12);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticHook(uint hookId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		var buffer = Caller.AllocateBuffer(13);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;
		buffer[12] = arg13;

		var result = CallStaticHook(hookId, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		var buffer = Caller.AllocateBuffer(13);
		buffer[0] = arg1;
		buffer[1] = arg2;
		buffer[2] = arg3;
		buffer[3] = arg4;
		buffer[4] = arg5;
		buffer[5] = arg6;
		buffer[6] = arg7;
		buffer[7] = arg8;
		buffer[8] = arg9;
		buffer[9] = arg10;
		buffer[10] = arg11;
		buffer[11] = arg12;
		buffer[12] = arg13;

		var result = CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, flag: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args: buffer);

		Caller.ClearBuffer(buffer);
		return result;
	}

	public static object CallStaticHook(uint hookId, object[] args, bool keepArgs = false)
	{
		return CallStaticHook(hookId, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, args, keepArgs: keepArgs);
	}
	public static object CallStaticDeprecatedHook(uint oldHookId, uint newHookId, DateTime expireDate, object[] args)
	{
		return CallStaticDeprecatedHook(oldHookId, newHookId, expireDate, args);
	}

	#endregion

	#region Generator

	public static void GenerateInternalCallHook(CompilationUnitSyntax input, out CompilationUnitSyntax output, out MethodDeclarationSyntax generatedMethod, out bool isPartial, List<ClassDeclarationSyntax> _classList = null)
	{
		var methodContents = "\n\tvar result = (object)null;\n\ttry\n\t{\n\t\tswitch(hook)\n\t\t{\n";

		var @namespace = (BaseNamespaceDeclarationSyntax)null;
		var namespaceIndex = 0;
		var classIndex = 0;
		var isTemp = false;

		if (_classList == null)
		{
			_classList = Pool.GetList<ClassDeclarationSyntax>();
			isTemp = true;
			FindPluginInfo(input, out @namespace, out _, out _, _classList);
		}
		else
		{
			FindPluginInfo(input, out @namespace, out _, out _, null);

			namespaceIndex = classIndex = 0;
		}

		var @class = _classList[0];

		if (@namespace == null)
		{
			@namespace = @class.Parent as BaseNamespaceDeclarationSyntax;
		}

		isPartial = @class.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword));

		var methodDeclarations = new List<MethodDeclarationSyntax>();
		methodDeclarations.AddRange(_classList.SelectMany(x => x.ChildNodes()).OfType<MethodDeclarationSyntax>());

		if (isTemp)
		{
			Pool.FreeList(ref _classList);
		}

		var hookableMethods = new Dictionary<uint, List<MethodDeclarationSyntax>>();
		var privateMethods0 = methodDeclarations.Where(md => (md.Modifiers.Count == 0 || md.Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.PublicKeyword) && !modifier.IsKind(SyntaxKind.StaticKeyword)) || md.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "HookMethod"))) && md.TypeParameterList == null);
		var privateMethods = privateMethods0.OrderBy(x => x.Identifier.ValueText);
		privateMethods0 = null;

		foreach (var method in privateMethods)
		{
			var methodName = method.Identifier.ValueText;
			var id = HookStringPool.GetOrAdd(methodName);

			if (!hookableMethods.TryGetValue(id, out var list))
			{
				hookableMethods[id] = list = new();
			}

			list.Add(method);
		}

		foreach (var group in hookableMethods)
		{
			methodContents += $"\t\t\t// {group.Value[0].Identifier.ValueText} aka {group.Key}\n\t\t\tcase {group.Key}:\n\t\t\t{{";

			var overrideCount = 1;

			for (int i = 0; i < group.Value.Count; i++)
			{
				var parameterIndex = -1;
				var method = group.Value[i];
				var conditional = method.AttributeLists.Select(x => x.Attributes.FirstOrDefault(x => ((IdentifierNameSyntax)x.Name).Identifier.Text == "Conditional"))?.FirstOrDefault()?.ArgumentList?.Arguments[0].ToString().Replace("\"", string.Empty);
				var methodName = method.Identifier.ValueText;
				var parameters0 = method.ParameterList.Parameters.Select(x =>
				{
					var type = x.Type.ToString().Replace("?", string.Empty);
					parameterIndex++;

					if (x.Modifiers.Any(x => x.IsKind(SyntaxKind.OutKeyword)))
					{
						return $"out var arg{parameterIndex}_{i}";
					}
					else if (x.Default != null || x.Type is NullableTypeSyntax)
					{
						return $"args[{parameterIndex}] is {type} arg{parameterIndex}_{i} ? arg{parameterIndex}_{i} : ({type})default";
					}
					else if (x.Modifiers.Any(x => x.IsKind(SyntaxKind.RefKeyword)))
					{
						return $"ref arg{parameterIndex}_{i}";
					}

					return $"arg{parameterIndex}_{i}";
				});
				var parameters = parameters0.ToArray();

				var requiredParameters = method.ParameterList.Parameters.Where(x => x.Default == null && x.Type is not NullableTypeSyntax);
				var requiredParameterCount = requiredParameters.Count(x => !x.Modifiers.Any(y => y.IsKind(SyntaxKind.OutKeyword)));

				var refSets = string.Empty;
				parameterIndex = 0;
				foreach (var @ref in method.ParameterList.Parameters)
				{
					if (@ref.Modifiers.Any(x => x.IsKind(SyntaxKind.RefKeyword) || x.IsKind(SyntaxKind.OutKeyword)))
					{
						refSets += $"args[{parameterIndex}] = arg{parameterIndex}_{i}; ";
					}

					parameterIndex++;
				}

				parameterIndex = -1;
				var parameterText = string.Empty;
				var varText = string.Empty;
				for (int o = 0; o < method.ParameterList.Parameters.Count; o++)
				{
					var parameter = method.ParameterList.Parameters[o];
					parameterIndex++;

					if (parameter.Default == null && !parameter.Modifiers.Any(y => y.IsKind(SyntaxKind.OutKeyword)) && parameter.Type is not NullableTypeSyntax && !(parameter.Type is ITypeSymbol symbol && symbol.IsValueType))
					{
						var type = parameter.Type.ToString().Replace("global::", string.Empty);
						varText += $"var narg{parameterIndex}_{i} = args[{parameterIndex}] is {type} or null;\nvar arg{parameterIndex}_{i} = narg{parameterIndex}_{i} ? ({type})(args[{parameterIndex}] ?? ({type})default) : ({type})default;\n";
						parameterText += !IsUnmanagedType(type) ? $"narg{parameterIndex}_{i} && " : $"(narg{parameterIndex}_{i} || args[{parameterIndex}] == null) && ";
					}
				}

				if (!string.IsNullOrEmpty(parameterText))
				{
					parameterText = parameterText[..^3];
				}

				var validLengthCheck = group.Value.Min(y => y.ParameterList.Parameters.Count) != group.Value.Max(y => y.ParameterList.Parameters.Count);
				methodContents += $"{(string.IsNullOrEmpty(conditional) ? string.Empty : $"\n#if {conditional}")}\t\t\t\n\t\t\t\t" +
					$"{(requiredParameterCount > 0 ? $"{(validLengthCheck ? $"if(args.Length >= {method.ParameterList.Parameters.Count})" : string.Empty)}" : "")} " +
					$"{(requiredParameterCount > 0 && methodName != "OnServerInitialized" && validLengthCheck ? "{" : "")} {varText}" +
					$"{(string.IsNullOrEmpty(parameterText) ? string.Empty : $"if({parameterText}) {{")} {(method.ReturnType.ToString() != "void" ? $"var result{overrideCount} = " : string.Empty)}" +
					$"{methodName}({string.Join(", ", parameters)}); {refSets} {(method.ReturnType.ToString() != "void" ? $"if(result == null) {{ result = result{overrideCount}; }}" : string.Empty)} " +
					$"{(requiredParameterCount > 0 && methodName != "OnServerInitialized" && validLengthCheck ? "}" : "")}" +
					$"{(string.IsNullOrEmpty(parameterText) ? string.Empty : $"}}")}{(string.IsNullOrEmpty(conditional) ? string.Empty : $"\n#endif")}\n";

				Array.Clear(parameters, 0, parameters.Length);
				parameters = null;
				parameters0 = null;
				requiredParameters = null;

				overrideCount++;
			}

			methodContents += "\t\t\t\tbreak;\n\t\t\t}\n";
		}

		methodContents += "}\n}\ncatch (System.Exception ex)\n{\nCarbon.Logger.Error($\"Failed to call internal hook '{Carbon.Pooling.HookStringPool.GetOrAdd(hook)}' on plugin '{base.Name} v{base.Version}' [{hook}]\", ex);\n}\nreturn result;";

		generatedMethod = SyntaxFactory.MethodDeclaration(
			SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword).WithTrailingTrivia(SyntaxFactory.Space)),
			"InternalCallHook").AddParameterListParameters(
				SyntaxFactory.Parameter(SyntaxFactory.Identifier("hook")).WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword)).WithTrailingTrivia(SyntaxFactory.Space)),
				SyntaxFactory.Parameter(SyntaxFactory.Identifier("args")).WithType(SyntaxFactory.ArrayType(
				SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
				SyntaxFactory.SingletonList(
						SyntaxFactory.ArrayRankSpecifier(
							SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
								SyntaxFactory.OmittedArraySizeExpression()
							)
						)
					)
				).WithTrailingTrivia(SyntaxFactory.Space)))
				.WithTrailingTrivia(SyntaxFactory.LineFeed)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxFactory.Space), SyntaxFactory.Token(SyntaxKind.OverrideKeyword).WithTrailingTrivia(SyntaxFactory.Space))
			.AddBodyStatements(SyntaxFactory.ParseStatement(methodContents)).WithTrailingTrivia(SyntaxFactory.LineFeed);

		output = input.WithMembers(input.Members.RemoveAt(namespaceIndex).Insert(namespaceIndex, @namespace.WithMembers(@namespace.Members.RemoveAt(classIndex).Insert(classIndex, @class.WithMembers(@class.Members.Insert(@class.Members.Count, generatedMethod))))));

		#region Cleanup

		methodDeclarations.Clear();
		methodDeclarations = null;
		foreach (var hookableMethod in hookableMethods)
		{
			hookableMethod.Value.Clear();
		}
		hookableMethods.Clear();
		hookableMethods = null;

		#endregion
	}

	public static void GeneratePartial(CompilationUnitSyntax input, out CompilationUnitSyntax output, CSharpParseOptions options, string fileName, List<ClassDeclarationSyntax> classes = null)
	{
		GenerateInternalCallHook(input, out _, out var method, out var isPartial, classes);

		var @namespace = (BaseNamespaceDeclarationSyntax)null;
		var @class = (ClassDeclarationSyntax)null;

		if (classes == null)
		{
			classes = Facepunch.Pool.GetList<ClassDeclarationSyntax>();
			FindPluginInfo(input, out @namespace, out _, out _, classes);

			@class = classes[0];
			Facepunch.Pool.FreeList(ref classes);
		}
		else
		{
			@namespace = classes[0].Parent as BaseNamespaceDeclarationSyntax;
			@class = classes[0];
		}

		var usings = input.Usings;
		var subUsings = @namespace.Usings;

		var source = @$"{usings.Select(x => x.ToString()).ToString("\n")}

namespace {@namespace.Name};
{(subUsings.Any() ? $"\n{subUsings.Select(x => x.ToString()).ToString("\n")}" : string.Empty)}
partial class {@class.Identifier.ValueText}
{{
	{method}
}}";

		string path;

#if DEBUG
		if (isPartial && Debugger.IsAttached)
		{
			path = Path.Combine(Defines.GetScriptDebugFolder(), $"{Path.GetFileNameWithoutExtension(fileName)}.Internal.cs");
			output = CSharpSyntaxTree.ParseText(source, options, path, Encoding.UTF8).GetCompilationUnitRoot().NormalizeWhitespace();
			OsEx.File.Create(path, output.ToFullString());
		}
		else
		{
			path = $"{fileName}/Internal";
			output = CSharpSyntaxTree.ParseText(source, options, path, Encoding.UTF8).GetCompilationUnitRoot();
		}
#else
			path = $"{fileName}/Internal";
			output = CSharpSyntaxTree.ParseText(source, options, path, Encoding.UTF8).GetCompilationUnitRoot();
#endif
	}

	public static bool FindPluginInfo(CompilationUnitSyntax input, out BaseNamespaceDeclarationSyntax @namespace, out int namespaceIndex, out int classIndex, List<ClassDeclarationSyntax> classes)
	{
		var @class = (ClassDeclarationSyntax)null;
		@namespace = null;
		namespaceIndex = 0;
		classIndex = 0;

		foreach (var ns in input.Members.OfType<BaseNamespaceDeclarationSyntax>())
		{
			var nsClasses = ns.Members.OfType<ClassDeclarationSyntax>();

			for(int i = 0; i < nsClasses.Count(); i++)
			{
				var cls = nsClasses.ElementAt(i);

				if (cls.AttributeLists.Count > 0)
				{
					foreach(var attribute in cls.AttributeLists)
					{
						if (attribute.Attributes[0].Name is IdentifierNameSyntax nameSyntax && nameSyntax.Identifier.Text == "Info")
						{
							@namespaceIndex = input.Members.IndexOf(ns);
							@namespace = ns;
							classIndex = i;
							@class = cls;
							classes?.Insert(0, @class);
						}
					}
				}
				else if(cls.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
				{
					classes?.Add(cls);
				}
			}
		}

		return @class != null;
	}

	public static bool IsUnmanagedType(string type)
	{
		return type switch
		{
			"string" or "int" or "uint" or "long" or "ulong" or "bool" => true,
			_ => false,
		};
	}

	#endregion
}
