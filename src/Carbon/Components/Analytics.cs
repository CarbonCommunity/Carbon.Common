namespace Carbon.Components;

public partial struct Analytics
{
	public static readonly Dictionary<string, object> Metrics = new();

	public static bool Enabled => Community.Runtime.Analytics.Enabled;

	public static Analytics Singleton = default;

	public Analytics Include(string key, object value)
	{
		Metrics[key] = value;
		return this;
	}

	public Analytics Submit(string eventName)
	{
		Community.Runtime.Analytics.LogEvents(eventName);
		Dispose();
		return this;
	}

	public static void Dispose()
	{
		Metrics.Clear();
	}
}
