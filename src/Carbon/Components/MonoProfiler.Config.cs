namespace Carbon.Components;

public partial class MonoProfiler
{
	public class Config
	{
		public bool Enabled = false;
		public bool Allocations = false;
		public List<string> AssembliesToProfile = new List<string>();
	}
}
