using UnityEngine.Serialization;

namespace Carbon.Profiler;

[Serializable]
public class MonoProfilerConfig
{
	public bool Enabled = false;
	public bool Allocations = false;
	public List<string> Assemblies = new();
	public List<string> Plugins = new();
	public List<string> Modules = new();
	public List<string> Extensions = new();
	public List<string> Harmony = new();

	public enum ProfileTypes
	{
		Assembly,
		Plugin,
		Module,
		Extension,
		Harmony
	}

	public bool AppendProfile(ProfileTypes profile,string value)
	{
		return profile switch
		{
			ProfileTypes.Assembly => Do(Assemblies, value),
			ProfileTypes.Plugin => Do(Plugins, value),
			ProfileTypes.Module => Do(Modules, value),
			ProfileTypes.Extension => Do(Extensions, value),
			ProfileTypes.Harmony => Do(Harmony, value),
			_ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
		};

		static bool Do(List<string> list, string value)
		{
			if (list.Contains(value))
			{
				return false;
			}

			list.Add(value);
			return true;
		}
	}
	public bool RemoveProfile(ProfileTypes profile,string value)
	{
		return profile switch
		{
			ProfileTypes.Assembly => Do(Assemblies, value),
			ProfileTypes.Plugin => Do(Plugins, value),
			ProfileTypes.Module => Do(Modules, value),
			ProfileTypes.Extension => Do(Extensions, value),
			ProfileTypes.Harmony => Do(Harmony, value),
			_ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
		};

		static bool Do(List<string> list, string value)
		{
			if (!list.Contains(value))
			{
				return false;
			}

			list.Remove(value);
			return true;
		}
	}
	public bool IsWhitelisted(ProfileTypes profile, string value)
	{
		return profile switch
		{
			ProfileTypes.Assembly => Assemblies.Contains("*") || Assemblies.Contains(value),
			ProfileTypes.Plugin => Plugins.Contains("*") || Plugins.Contains(value),
			ProfileTypes.Module => Modules.Contains("*") || Modules.Contains(value),
			ProfileTypes.Extension => Extensions.Contains("*") || Extensions.Contains(value),
			ProfileTypes.Harmony => Harmony.Contains("*") || Harmony.Contains(value),
			_ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
		};
	}
}
