/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Hooks;

public sealed class Updater
{
	private static readonly string Repository
		= @"CarbonCommunity/Carbon.Redist";

	private static string GithubReleaseUrl(string file, string protocol = null)
	{
		string branch = "main";
		string suffix = (Community.Runtime.Analytics.Platform == "linux") ? "Unix" : default;
		string target = (Community.Runtime.Analytics.Branch == "Release") ? "Release" : "Debug";

		return $"https://raw.githubusercontent.com/{Repository}/{branch}/Modules/"
			+ $"{target}{suffix}/{(protocol is null ? $"{file}" : $"{protocol}/{file}")}";
	}

	public static async void DoUpdate(Action<bool> callback = null)
	{
		// FIXME: the update process is triggering carbon init process twice
		// when more than one file is listed here to be downloaded [and] one of
		// them fails with 404.
		IReadOnlyList<string> files = new List<string>(){
			@"carbon/managed/hooks/Carbon.Hooks.Community.dll",
			@"carbon/managed/hooks/Carbon.Hooks.Oxide.dll"
		};

		int failed = 0;
		foreach (string file in files)
		{
			Logger.Warn($"Updating component '{Path.GetFileName(file)}@{Community.Runtime.Analytics.Protocol}' using the "
				+ $"'{Community.Runtime.Analytics.Branch} [{Community.Runtime.Analytics.Platform}]' branch");
			byte[] buffer = await Community.Runtime.Downloader.Download(GithubReleaseUrl(file, Community.Runtime.Analytics.Protocol));

			if (buffer is { Length: < 1 })
			{
				Logger.Warn($"[Retry updating component '{Path.GetFileName(file)}' using the "
					+ $"'{Community.Runtime.Analytics.Branch} [{Community.Runtime.Analytics.Platform}]' branch");
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
