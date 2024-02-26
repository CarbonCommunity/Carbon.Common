namespace Carbon.Components;

public struct Analytic
{
	public static readonly Dictionary<string, object> Metrics = new();

	public static bool Enabled => Community.Runtime.Analytics.Enabled;

	public static void Include(string key, object value)
	{
		Metrics[key] = value;
	}

	public static void Send(string eventName)
	{
		Community.Runtime.Analytics.LogEvents(eventName);
		Dispose();
	}

	public static void Dispose()
	{
		Metrics.Clear();
	}
}
