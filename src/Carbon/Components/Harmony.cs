using Facepunch;
using HarmonyLib;

namespace Carbon.Components;

public class Harmony
{
	public static Dictionary<Assembly, List<object>> ModHooks = new();
	public static List<PatchInfoEntry> CurrentPatches = new();

	public static void PatchAll(Assembly assembly)
	{
		var assemblyName = assembly.GetName().Name;
		var harmony = new HarmonyLib.Harmony($"com.compat-harmony.{assemblyName}");

		foreach (var type in assembly.GetExportedTypes().Where(x => x.GetCustomAttribute<HarmonyPatch>() != null))
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
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"[{harmony.Id}] Failed to patch '{type.Name}'", ex);
			}
		}

		CurrentPatches.Add(new PatchInfoEntry(assemblyName, assemblyName, null, null, null, harmony));
	}
	public static void UnpatchAll(string assembly)
	{
		var patches = Pool.GetList<PatchInfoEntry>();
		patches.AddRange(CurrentPatches.Where(x => x.AssemblyName == assembly));

		foreach (var a in patches )
		{
			a.Unpatch();
		}

		CurrentPatches.RemoveAll(x => x.AssemblyName == assembly);
		Pool.FreeList(ref patches);
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

		public void Unpatch()
		{
			if (Harmony == null)
			{
				return;
			}

			try
			{
				if (Harmony != null)
				{
					foreach (var method in Harmony.GetPatchedMethods())
					{
						Logger.Warn($"[{Harmony.Id}] Unpatched '{method.Name}' method. ({method.DeclaringType.Name})");
					}
				}

				Harmony.UnpatchAll(Harmony.Id);
				Harmony = null;
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to unpatch '{MethodName}' ({TypeName})", ex);
			}
		}
	}
}
