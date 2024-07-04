/*
 *
 * Copyright (c) 2024 Carbon Community
 * All rights reserved.
 *
 */

using Newtonsoft.Json;

namespace Carbon.Core;

public static partial class ModLoader
{
	public class PackageBank : List<Package>
	{
		public Package FindPackage(string name)
		{
			return this.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCulture));
		}

		public RustPlugin FindPlugin(string name)
		{
			return this.Select(package => package.FindPlugin(name)).FirstOrDefault(plugin => plugin != null);
		}
	}
}
