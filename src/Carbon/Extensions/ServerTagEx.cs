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

		if (!tags.Contains(tag))
		{
			var indexOf = tags.IndexOf('^');

			if (indexOf > 0)
			{
				Steamworks.SteamServer.GameTags = tags.Insert(indexOf, tag);
			}
			else
			{
				Steamworks.SteamServer.GameTags = $"{tags}{(tags.EndsWith(",") ? string.Empty : ",")}{tag}";
			}

			return true;
		}

		return false;
	}

	public static bool UnsetRequiredTag(string tag)
	{
		var tags = Steamworks.SteamServer.GameTags;

		if (tags.Contains(tag))
		{
			Steamworks.SteamServer.GameTags = tags.Replace(tag, "");
			return true;
		}

		return false;
	}
}
