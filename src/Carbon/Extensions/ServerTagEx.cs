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
			Steamworks.SteamServer.GameTags = ServerTagCompressor.CompressTags($"{tags}{tag}");
			return true;
		}

		return false;
	}

	public static bool UnsetRequiredTag(string tag)
	{
		var tags = Steamworks.SteamServer.GameTags;

		if (tags.Contains(tag))
		{
			Steamworks.SteamServer.GameTags = tags.Replace(tag, string.Empty);
			return true;
		}

		return false;
	}
}
