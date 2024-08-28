﻿using Facepunch;
using HarmonyLib;

namespace Carbon.Components;

public class Harmony
{
	public static Dictionary<Assembly, List<object>> ModHooks = new();
	public static List<PatchInfoEntry> CurrentPatches = new();

	public static int PatchAll(Assembly assembly)
	{
		var patchCount = 0;
		var assemblyName = assembly.GetName().Name;
		var harmony = new HarmonyLib.Harmony($"com.compat-harmony.{assemblyName}");

		foreach (var type in assembly.GetTypes().Where(x => x.GetCustomAttribute<HarmonyPatch>() != null))
		{
			try
			{
				var harmonyMethods = harmony.CreateClassProcessor(type).Patch();

				if (harmonyMethods == null || harmonyMethods.Count == 0)
				{
					continue;
				}

				foreach (MethodInfo method in harmonyMethods)
				{
					Logger.Warn($"[{harmony.Id}] Patched '{method.Name}' method. ({type.Name})");
					patchCount++;
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"[{harmony.Id}] Failed to patch '{type.Name}'", ex);
			}
		}

		CurrentPatches.Add(new PatchInfoEntry(assemblyName + ".dll", assemblyName, null, null, null, harmony));
		return patchCount;
	}
	public static int UnpatchAll(string assembly)
	{
		assembly += ".dll";

		var patches = Pool.Get<List<PatchInfoEntry>>();
		patches.AddRange(CurrentPatches.Where(x => x.ParentAssemblyName.Equals(assembly)));

		var count = patches.Sum(a => a.Unpatch());

		CurrentPatches.RemoveAll(x => x.ParentAssemblyName.Equals(assembly));
		Pool.FreeUnmanaged(ref patches);
		return count;
	}

	public class PatchInfoEntry
	{
		public string ParentAssemblyName;
		public string AssemblyName;
		public string TypeName;
		public string MethodName;
		public string Reason;
		public HarmonyLib.Harmony Harmony;
		public MethodBase runtime_method;

		public PatchInfoEntry(string parentAssemblyName, string assemblyName, string methodName, string typeName, string reason,
			HarmonyLib.Harmony harmony)
		{
			this.ParentAssemblyName = parentAssemblyName;
			this.AssemblyName = assemblyName;
			this.MethodName = methodName;
			this.TypeName = typeName;
			this.Reason = reason;
			this.Harmony = harmony;
		}

		public PatchInfoEntry(string parentAssemblyName,MethodBase method, HarmonyLib.Harmony harmony)
		{
			this.ParentAssemblyName = parentAssemblyName;
			this.Harmony = harmony;
			this.runtime_method = method;
		}

		public int Unpatch()
		{
			var count = 0;

			if (Harmony == null)
			{
				return count;
			}

			try
			{
				if (Harmony != null)
				{
					foreach (var method in Harmony.GetPatchedMethods())
					{
						Logger.Warn($"[{Harmony.Id}] Unpatched '{method.Name}' method. ({method.DeclaringType.Name})");
						count++;
					}
				}

				Harmony.UnpatchAll(Harmony.Id);
				Harmony = null;
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to unpatch '{MethodName}' ({TypeName})", ex);
			}

			return count;
		}
	}
}
