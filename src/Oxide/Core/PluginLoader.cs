/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Oxide.Core.Plugins
{
	public class PluginLoader
	{
		public virtual Type[] CorePlugins { get; } = [];

		public virtual IEnumerable<string> ScanDirectory(string directory)
		{
			foreach (var plugin in CorePlugin.OrderedFiles)
			{
				if (!plugin.Value.Contains(directory, CompareOptions.OrdinalIgnoreCase))
				{
					continue;
				}

				yield return plugin.Value;
			}
		}
	}
}
