﻿using Logger = Carbon.Logger;

namespace Oxide.Core;

public class Interface
{
	public static OxideMod Oxide { get; set; } = new();
	public static OxideMod uMod => Oxide;

	public static void Initialize()
	{
		Oxide.Load();
		Logger.Log($"  Instance Directory: {Oxide.InstanceDirectory}");
		Logger.Log($"  Root Directory: {Oxide.RootDirectory}");
		Logger.Log($"  Config Directory: {Oxide.ConfigDirectory}");
		Logger.Log($"  Data Directory: {Oxide.DataDirectory}");
		Logger.Log($"  Lang Directory: {Oxide.LangDirectory}");
		Logger.Log($"  Log Directory: {Oxide.LogDirectory}");
		Logger.Log($"  Plugin Directory: {Oxide.PluginDirectory}");
	}

	public static OxideMod GetMod() => Oxide;

	public static T Call<T>(string hookName)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName)));
	}
	public static T Call<T>(string hookName, object arg1)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1));
	}
	public static T Call<T>(string hookName, object arg1, object arg2)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11));
	}
	public static T Call<T>(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return (T)TypeEx.ConvertType<T>(HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12));
	}

	public static object Call(string hook)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook));
	}
	public static object Call(string hook, object arg1)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1);
	}
	public static object Call(string hook, object arg1, object arg2)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}
	public static object Call(string hook, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
	}

	public static object CallHook(string hookName)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName));
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate);
	}
	public static object CallHook(string hookName, object arg1)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1);
	}
	public static object CallHook(string hookName, object arg1, object arg2)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5, arg6);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}
	public static object CallHook(string hookName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hookName), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}
	public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
	{
		return HookCaller.CallStaticDeprecatedHook(HookStringPool.GetOrAdd(oldHook), HookStringPool.GetOrAdd(newHook), expireDate, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}

	public static object Call(string hook, object[] args)
	{
		return Call<object>(hook, args);
	}
	public static object CallHook(string hook, object[] args)
	{
		return Call<object>(hook, args);
	}

	public static object Call<T>(string hook, object[] args)
	{
		return HookCaller.CallStaticHook(HookStringPool.GetOrAdd(hook), args: args);
	}
}
