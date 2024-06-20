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
	[JsonObject(MemberSerialization.OptIn)]
	public struct CompilationResult
	{
		[JsonProperty] public string File;
		[JsonProperty] public List<Trace> Errors;
		[JsonProperty] public List<Trace> Warnings;
		public Type RollbackType;

		public static CompilationResult Create(string file)
		{
			CompilationResult result = default;
			result.File = file;
			result.Errors = new();
			result.Warnings = new();
			return result;
		}

		public void AppendErrors(IEnumerable<Trace> traces)
		{
			Errors.AddRange(traces);
		}
		public void AppendWarnings(IEnumerable<Trace> traces)
		{
			Warnings.AddRange(traces);
		}

		public void SetRollbackType(Type type)
		{
			RollbackType = type;
		}
		public void LoadRollbackType()
		{
			if (RollbackType == null)
			{
				return;
			}

			var existentPlugin = FindPlugin(GetRollbackTypeName());

			if (existentPlugin != null)
			{
				return;
			}

			InitializePlugin(RollbackType, out var plugin, Community.Runtime.Plugins, plugin =>
			{
				Logger.Warn($"Rollback for plugin '{plugin.ToPrettyString()}' due to compilation failure");
			}, precompiled: true);
			plugin.InternalCallHookOverriden = true;
			plugin.IsPrecompiled = false;
		}
		public string GetRollbackTypeName()
		{
			return RollbackType == null ? string.Empty : RollbackType.GetCustomAttribute<InfoAttribute>()?.Title?.Replace(" ", string.Empty);
		}

		public bool IsValid()
		{
			return Errors is { Count: > 0 };
		}
		public void Clear()
		{
			Errors?.Clear();
			Warnings?.Clear();
		}
	}
}
