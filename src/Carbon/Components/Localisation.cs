namespace Carbon.Components;

public partial struct Localisation
{
	internal static CorePlugin Core => Community.Runtime.Core;

	public static Dictionary<string, string> Phrases = new()
	{
		["cooldown_player"] = "You're cooled down. Please wait {0}."
	};

	public static string Get(string key, string playerId)
	{
		return Core.lang.GetMessage(key, Core, playerId, Community.Runtime.Config.Language);
	}
	public static string Get(string key, string playerId, params object[] format)
	{
		return string.Format(Get(key, playerId), format);
	}
}
