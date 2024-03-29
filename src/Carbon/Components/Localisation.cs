﻿namespace Carbon.Components;

public partial struct Localisation
{
	internal static CorePlugin _core => Community.Runtime.CorePlugin as CorePlugin;

	public static Dictionary<string, string> Phrases = new()
	{
		["cooldown_player"] = "You're cooled down. Please wait {0}."
	};

	public static string Get(string key, string playerId)
	{
		return _core.lang.GetMessage(key, _core, playerId, Community.Runtime.Config.Language);
	}
	public static string Get(string key, string playerId, params object[] format)
	{
		return string.Format(Get(key, playerId), format);
	}
}
