namespace Carbon.Profiler;

[Serializable]
public class MonoProfilerConfig
{
	public bool Enabled = false;
	public bool Allocations = false;
	public List<string> AssembliesToProfile = new();

	public bool AddAssembly(string assembly)
	{
		if (AssembliesToProfile.Contains(assembly))
		{
			return false;
		}

		AssembliesToProfile.Add(assembly);
		return true;
	}
	public bool RemoveAssembly(string assembly)
	{
		if (!AssembliesToProfile.Contains(assembly))
		{
			return false;
		}

		AssembliesToProfile.Remove(assembly);
		return true;
	}
}
