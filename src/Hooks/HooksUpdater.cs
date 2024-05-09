/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Hooks;

public sealed class Updater
{
	private static string GithubReleaseUrl(string file, string protocol = null)
	{
		var suffix = (Community.Runtime.Analytics.Platform == "linux") ? "unix" : default;
		var target = (Community.Runtime.Analytics.Branch == "Release") ? "release" : "debug";

		return $"https://carbonmod.gg/redist/server/"
			+ $"{target}{suffix}/{(protocol is null ? $"{file}" : $"{protocol}/{file}")}";
	}

	public static async void DoUpdate(Action<bool> callback = null)
	{
		// FIXME: the update process is triggering carbon init process twice
		// when more than one file is listed here to be downloaded [and] one of
		// them fails with 404.
		IReadOnlyList<string> files = [
			@"carbon/managed/hooks/Carbon.Hooks.Community.dll",
			@"carbon/managed/hooks/Carbon.Hooks.Oxide.dll"
		];

		int failed = 0;
		foreach (string file in files)
		{
			Logger.Warn($"Updating component '{Path.GetFileName(file)}@{Community.Runtime.Analytics.Protocol}' on {Community.Runtime.Analytics.Platform} [{Community.Runtime.Analytics.Branch}]");
			byte[] buffer = await Community.Runtime.Downloader.Download(GithubReleaseUrl(file, Community.Runtime.Analytics.Protocol));

			if (buffer is { Length: < 1 })
			{
				Logger.Warn($"Retrying component update '{Path.GetFileName(file)}' on {Community.Runtime.Analytics.Platform} [{Community.Runtime.Analytics.Branch}]...");
				buffer = await Community.Runtime.Downloader.Download(GithubReleaseUrl(file));
			}

			if (buffer is { Length: < 1 })
			{
				Logger.Warn($"Unable to update component '{Path.GetFileName(file)}', please try again later");
				failed++; continue;
			}

			try
			{
				string destination = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file));
				File.WriteAllBytes(destination, buffer);
			}
			catch (System.Exception e)
			{
				Logger.Error($"Error while updating component '{Path.GetFileName(file)}'", e);
				failed++;
			}
		}
		callback?.Invoke(failed == 0);
	}
}
