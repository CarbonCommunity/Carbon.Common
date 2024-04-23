using Carbon.Profiler;
using Facepunch;

#if !MINIMAL

/*
*
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Modules;

public partial class AdminModule
{
	public class ProfilerTab : Tab
	{
		internal static ProfilerTab _instance;
		internal static Color intenseColor;
		internal static Color calmColor;
		internal static Color niceColor;

		internal static string[] sortAssemblyOptions = new[] {
			"Name",
			"Total Time",
			"Total Time (%)",
			"Calls",
			"Memory" };

		internal static string[] sortCallsOptions = new[]
		{
			"Method",
			"Total Time",
			"Total Time (%)",
			"Own Time",
			"Own Time (%)",
			"Calls",
			"Memory (Total)",
			"Memory (Own)"
		};

		public ProfilerTab(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null) :
			base(id, name, plugin, onChange)
		{
			ColorUtility.TryParseHtmlString("#d13b38", out intenseColor);
			ColorUtility.TryParseHtmlString("#3882d1", out calmColor);
			ColorUtility.TryParseHtmlString("#60a848", out niceColor);
		}

		public static ProfilerTab GetOrCache(PlayerSession session) => _instance ??= Make(session);

		public static ProfilerTab Make(PlayerSession session)
		{
			var profiler = new ProfilerTab("profiler", "Profiler", Community.Runtime.CorePlugin);
			profiler.OnChange = (ap, tab) => profiler.Draw(ap);
			profiler.Over = (tab, cui, container, parent, ap) =>
			{
				if (MonoProfiler.Enabled) return;

				var blur = cui.CreatePanel(container, parent, "0 0 0 0.5", blur: true);
				cui.CreateText(container, blur, "1 1 1 0.5", "<b>Mono profiler is disabled</b>\nEnable it in the config, then reboot the server.", 10);
			};
			profiler.Draw(session);

			return profiler;
		}

		public static IEnumerable<MonoProfiler.AssemblyRecord> GetSortedBasic(int sort)
		{
			return sort switch
			{
				0 => MonoProfiler.AssemblyRecords.OrderBy(x =>
				{
					if (!MonoProfiler.AssemblyMap.TryGetValue(x.assembly_handle, out var assemblyName))
					{
						return string.Empty;
					}

					return assemblyName;
				}),
				1 => MonoProfiler.AssemblyRecords.OrderByDescending(x => x.total_time),
				2 => MonoProfiler.AssemblyRecords.OrderByDescending(x => x.total_time_percentage),
				3 => MonoProfiler.AssemblyRecords.OrderByDescending(x => x.calls),
				4 => MonoProfiler.AssemblyRecords.OrderByDescending(x => x.alloc),
				_ => default
			};
		}

		internal void Draw(PlayerSession ap)
		{
			var selection = ap.GetStorage<ModuleHandle>(null, "profilerval");

			DrawAssemblies(ap, selection);
			DrawCalls(ap, selection);
		}

		static void Stripe(Tab tab,
			int column,
			float value,
			float maxValue,
			Color intenseColor,
			Color calmColor,
			string title,
			string subtitle,
			string side,
			string command,
			bool selected = false)
		{
			if (maxValue <= value)
			{
				maxValue = value;
			}

			tab.AddWidget(column, 0, (ap, cui, container, parent) =>
			{
				var percentage = value.Scale(0, maxValue, 0f, 1f);
				var color = Color32.Lerp(calmColor, intenseColor, percentage);

				var button = string.IsNullOrEmpty(command) ?
					cui.CreatePanel(container, parent, "0.15 0.15 0.15 0.7", Cache.CUI.BlankColor,
						xMin: 0.01f, xMax: 0.99f).Id :
					cui.CreateProtectedButton(container, parent, "0.15 0.15 0.15 0.7", Cache.CUI.BlankColor,
					string.Empty, 0, xMin: 0.01f, xMax: 0.99f, command: command).Id;

				var bar = cui.CreatePanel(container, button, $"#{ColorUtility.ToHtmlStringRGB(color)}",
					xMax: percentage);

				if (selected)
				{
					cui.CreatePanel(container, button, "#d13b38", xMax: 0.005f);
				}

				cui.CreateText(container, button, Cache.CUI.WhiteColor, title, 9, xMin: selected ? 0.02f : 0.01f,
					yMax: 0.9f, align: TextAnchor.UpperLeft, font: CUI.Handler.FontTypes.RobotoCondensedRegular);
				cui.CreateText(container, button, "1 1 1 0.6", subtitle, 8, xMin: selected ? 0.02f : 0.01f, yMin: 0.05f,
					align: TextAnchor.LowerLeft, font: CUI.Handler.FontTypes.RobotoCondensedRegular);
				cui.CreateText(container, button, "1 1 1 0.2", side, 8, xMax: 0.99f, yMin: 0.05f,
					align: TextAnchor.MiddleRight, font: CUI.Handler.FontTypes.RobotoCondensedRegular);

				cui.CreateImage(container, bar, "fade", Cache.CUI.WhiteColor);
			});
		}

		public void DrawAssemblies(PlayerSession session, ModuleHandle selection)
		{
			AddColumn(0, true);

			var searchInput = session.GetStorage(this, "bsearch", string.Empty);
			var sortIndex = session.GetStorage(this, "bsort", 1);
			var filtered = Pool.GetList<MonoProfiler.AssemblyRecord>();
			var maxVal = 0f;

			filtered.AddRange(GetSortedBasic(sortIndex));

			if (filtered.Count > 0)
			{
				maxVal = sortIndex switch
				{
					1 => filtered.Max(x => (float)x.total_time),
					0 or 2 => filtered.Max(x => (float)x.total_time_percentage),
					3 => filtered.Max(x => (float)x.calls),
					4 => filtered.Max(x => (float)x.alloc),
					_ => maxVal
				};
			}

			AddWidget(0, 0, (ap, cui, container, panel) =>
			{
				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", "1 1 1 0.5",
					"<size=6>EXPORT\n</size>TABLE", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: -46 * 4, OxMax: -46 * 4, command: "adminmodule.profilerexport 3");

				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", "1 1 1 0.5",
					"<size=6>EXPORT\n</size>JSON", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: -46 * 3, OxMax: -46 * 3, command: "adminmodule.profilerexport 0");

				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", "1 1 1 0.5",
					"<size=6>EXPORT\n</size>PROTO", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: -46 * 2, OxMax: -46 * 2, command: "adminmodule.profilerexport 1");

				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", "1 1 1 0.5",
					"<size=6>EXPORT\n</size>CSV", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: -46, OxMax: -46, command: "adminmodule.profilerexport 2");

				cui.CreateProtectedButton(container, panel, !MonoProfiler.IsCleared ? "0.9 0.1 0.1 1" : "0.2 0.2 0.2 0.7", "1 1 1 0.5",
					"CLEAR", 8,
					xMin: 0.83f, xMax: 0.925f, command: "adminmodule.profilerclear");

				cui.CreateProtectedButton(container, panel,
					MonoProfiler.Recording ? "0.9 0.1 0.1 1" : "0.2 0.2 0.2 0.7", "1 1 1 0.5", "REC", 8,
					xMin: 0.93f, xMax: 0.99f, command: "adminmodule.profilertoggle");
			});

			Stripe(this, 0, (float)filtered.Sum(x => x.total_time_percentage), 100, niceColor, niceColor,
				"All",
				$"{filtered.Sum(x => (float)x.total_time)}ms | {filtered.Sum(x => (float)x.total_time_percentage)}%",
				$"<size=7>{TimeEx.Format(MonoProfiler.DurationTime.TotalSeconds, false).ToLower()}\n{MonoProfiler.CallRecords.Count:n0} calls</size>",
				$"adminmodule.profilerselect -1",
				selection.GetHashCode() == 0);

			AddDropdown(0, $"<b>ASSEMBLIES ({MonoProfiler.AssemblyRecords.Count:n0})</b>", ap => sortIndex, (ap, i) =>
			{
				ap.SetStorage(this, "bsort", i);
				DrawAssemblies(session, selection);
			}, sortAssemblyOptions);

			AddInputButton(0, "Search", 0.075f, new OptionInput(null, ap => searchInput, 0, false, (ap, args) =>
			{
				ap.SetStorage(this, "bsearch", args.ToString(" "));
				DrawAssemblies(ap, selection);
			}), new OptionButton("X", ap =>
			{
				ap.SetStorage(this, "bsearch", string.Empty);
				DrawAssemblies(ap, selection);

			}, _ => string.IsNullOrEmpty(searchInput) ? OptionButton.Types.None : OptionButton.Types.Important));

			for (int i = 0; i < filtered.Count; i++)
			{
				var record = filtered[i];

				if (!MonoProfiler.AssemblyMap.TryGetValue(record.assembly_handle, out var assemblyName))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(searchInput) &&
				    !assemblyName.Contains(searchInput, CompareOptions.OrdinalIgnoreCase))
				{
					continue;
				}

				if (!MonoProfiler.AssemblyType.TryGetValue(record.assembly_handle, out var assemblyType))
				{
					assemblyType = MonoProfilerConfig.ProfileTypes.Assembly;
				}

				var value = sortIndex switch
				{
					1 => record.total_time,
					2 or 0 => (float)record.total_time_percentage,
					3 => record.calls,
					4 => record.alloc,
					_ => 0f
				};

				Stripe(this, 0, value, maxVal, intenseColor, calmColor,
					assemblyName,
					$"{record.total_time}ms ({record.total_time_percentage:0.0}%) | {ByteEx.Format(record.alloc).ToUpper()}",
					$"{assemblyType}\n<b>{record.calls:n0}</b> calls", $"adminmodule.profilerselect {i}", record.assembly_handle == selection);
			}

			if (filtered.Count == 0)
			{
				AddText(0, "No assemblies available", 8, "1 1 1 0.5");
			}

			Pool.FreeList(ref filtered);
		}
		public void DrawCalls(PlayerSession session, ModuleHandle selection)
		{
			AddColumn(1, true);

			var searchInput = session.GetStorage(this, "asearch", string.Empty);
			var sortIndex = session.GetStorage(this, "asort", 1);
			var advancedRecords =
				MonoProfiler.CallRecords.Where(x => selection.GetHashCode() == 0 || x.assembly_handle == selection);
			var maxVal = 0f;

			if (advancedRecords.Any())
			{
				switch (sortIndex)
				{
					case 0:
						advancedRecords = advancedRecords.OrderBy(x => x.method_name);
						maxVal = advancedRecords.Max(x => (float)x.total_time_percentage);
						break;

					case 1:
						advancedRecords = advancedRecords.OrderByDescending(x => x.total_time);
						maxVal = advancedRecords.Max(x => (float)x.total_time);
						break;

					case 2:
						advancedRecords = advancedRecords.OrderByDescending(x => x.total_time_percentage);
						maxVal = advancedRecords.Max(x => (float)x.total_time_percentage);
						break;

					case 3:
						advancedRecords = advancedRecords.OrderByDescending(x => x.own_time);
						maxVal = advancedRecords.Max(x => (float)x.own_time);
						break;

					case 4:
						advancedRecords = advancedRecords.OrderByDescending(x => x.own_time_percentage);
						maxVal = advancedRecords.Max(x => (float)x.own_time_percentage);
						break;

					case 5:
						advancedRecords = advancedRecords.OrderByDescending(x => x.calls);
						maxVal = advancedRecords.Max(x => (float)x.calls);
						break;

					case 6:
						advancedRecords = advancedRecords.OrderByDescending(x => x.total_alloc);
						maxVal = advancedRecords.Max(x => (float)x.total_alloc);
						break;

					case 7:
						advancedRecords = advancedRecords.OrderByDescending(x => x.own_alloc);
						maxVal = advancedRecords.Max(x => (float)x.own_alloc);
						break;
				}
			}

			AddDropdown(1, $"<b>CALLS ({advancedRecords.Count():n0})</b>", ap => sortIndex, (ap, i) =>
			{
				ap.SetStorage(this, "asort", i);
				DrawCalls(session, selection);
			}, sortCallsOptions);

			AddInputButton(1, "Search", 0.075f, new OptionInput(null, ap => searchInput, 0, false, (ap, args) =>
			{
				ap.SetStorage(this, "asearch", args.ToString(" "));
				DrawCalls(ap, selection);
			}), new OptionButton("X", ap =>
			{
				ap.SetStorage(this, "asearch", string.Empty);
				DrawCalls(ap, selection);

			}, _ => string.IsNullOrEmpty(searchInput) ? OptionButton.Types.None : OptionButton.Types.Important));

			var index = 0;
			foreach (var record in advancedRecords)
			{
				if (!string.IsNullOrEmpty(searchInput) &&
				    !record.method_name.Contains(searchInput, CompareOptions.OrdinalIgnoreCase))
				{
					continue;
				}

				var value = sortIndex switch
				{
					1 => record.total_time,
					2 or 0 => (float)record.total_time_percentage,
					3 => record.own_time,
					4 => (float)record.own_time_percentage,
					5 => record.calls,
					6 => record.total_alloc,
					7 => record.own_alloc,
					_ => 0f
				};

				Stripe(this, 1, value, maxVal, intenseColor, calmColor,
					record.method_name.Truncate(105, "..."),
					$"{record.total_time}ms total ({record.total_time_percentage}%) | {record.own_time}ms own ({record.own_time_percentage}%)",
					$"<b>{record.calls:n0}</b> {(((int)record.calls).Plural("call", "calls"))}\n{ByteEx.Format(record.total_alloc).ToUpper()} total | {ByteEx.Format(record.own_alloc).ToUpper()} own",
					string.Empty);

				index++;
			}

			if (!advancedRecords.Any())
			{
				AddText(1, "No calls available", 8, "1 1 1 0.5");
			}
		}
	}

	[ProtectedCommand("adminmodule.profilerselect")]
	private void ProfilerSelect(ConsoleSystem.Arg arg)
	{
		var ap = GetPlayerSession(arg.Player());

		var selection = ProfilerTab.GetSortedBasic(ap.GetStorage(ap.SelectedTab, "bsort", 1))
			.FindAt(arg.GetInt(0));
		ap.SetStorage(null, "profilerval", selection.assembly_handle);
		ap.SelectedTab.OnChange(ap, ap.SelectedTab);

		Draw(ap.Player);
	}

	[ProtectedCommand("adminmodule.profilertoggle")]
	private void ProfilerToggle(ConsoleSystem.Arg arg)
	{
		var ap = GetPlayerSession(arg.Player());

		if (!MonoProfiler.Enabled)
		{
			return;
		}

		MonoProfiler.Clear();
		MonoProfiler.ToggleProfilingTimed();

		ap.SelectedTab.OnChange(ap, ap.SelectedTab);

		Draw(ap.Player);
	}

	[ProtectedCommand("adminmodule.profilerexport")]
	private void ProfilerExport(ConsoleSystem.Arg arg)
	{
		var ap = GetPlayerSession(arg.Player());

		ap.SelectedTab.OnChange(ap, ap.SelectedTab);

		Draw(ap.Player);
	}

	[ProtectedCommand("adminmodule.profilerclear")]
	private void ProfilerClear(ConsoleSystem.Arg arg)
	{
		var ap = GetPlayerSession(arg.Player());

		MonoProfiler.Clear();
		ap.SetStorage(null, "profilerval", (ModuleHandle)default);
		ap.SelectedTab.OnChange(ap, ap.SelectedTab);

		Draw(ap.Player);
	}
}

#endif
