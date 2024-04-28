/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Extensions;

public static class ServerTagEx
{
	public static bool SetRequiredTag(string tag)
	{
		var tags = Steamworks.SteamServer.GameTags;

		if (tags.Contains(tag)) return false;
		var indexOf = tags.IndexOf('^');

		Steamworks.SteamServer.GameTags = indexOf > 0 ? tags.Insert(indexOf, tag) : $"{tags}{(tags.EndsWith(",") ? string.Empty : ",")}{tag}";

		return true;
	}

	public static bool UnsetRequiredTag(string tag)
	{
		var tags = Steamworks.SteamServer.GameTags;

		if (!tags.Contains(tag)) return false;
		Steamworks.SteamServer.GameTags = tags.Replace(tag, string.Empty);
		return true;
	}
}
