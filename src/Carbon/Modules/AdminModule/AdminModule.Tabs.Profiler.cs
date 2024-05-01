using Facepunch;

#if !MINIMAL

/*
*
 * Copyright (c) 2022-2024 Carbon Community
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

		public enum SubtabTypes
		{
			Calls,
			Memory
		}

		internal static string[] sortAssemblyOptions =
		[
			"Name",
			"Time",
			"Calls",
			"Memory"
		];
		internal static string[] sortCallsOptions =
		[
			"Method",
			"Calls",
			"Time (Total)",
			"Time (Own)",
			"Memory (Total)",
			"Memory (Own)"
		];
		internal static string[] sortMemoryOptions =
		[
			"Type",
			"Allocations",
			"Memory"
		];

		public ProfilerTab(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null) : base(id, name, plugin, onChange)
		{
			ColorUtility.TryParseHtmlString("#d13b38", out intenseColor);
			ColorUtility.TryParseHtmlString("#3882d1", out calmColor);
			ColorUtility.TryParseHtmlString("#60a848", out niceColor);
		}

		public static ProfilerTab GetOrCache(PlayerSession session) => _instance ??= Make(session);

		public static ProfilerTab Make(PlayerSession session)
		{
			var profiler = new ProfilerTab("profiler", "Profiler", Community.Runtime.CorePlugin);
			profiler.OnChange = (ap, _) =>
			{
				profiler.Draw(ap);
			};
			profiler.Over = (_, cui, container, parent, _) =>
			{
				if (MonoProfiler.Enabled) return;

				var blur = cui.CreatePanel(container, parent, "0 0 0 0.5", blur: true);
				cui.CreateText(container, blur, "1 1 1 0.5", "<b>Mono profiler is disabled</b>\nEnable it in the config, then reboot the server.", 10);
			};
			profiler.Draw(session);

			return profiler;
		}

		public static IEnumerable<MonoProfiler.AssemblyRecord> GetSortedAssemblies(int sort, string search)
		{
			return (sort switch
			{
				0 => MonoProfiler.AssemblyRecords.OrderBy(x => x.assembly_name.displayName),
				1 => MonoProfiler.AssemblyRecords.OrderByDescending(x => x.total_time),
				2 => MonoProfiler.AssemblyRecords.OrderByDescending(x => x.calls),
				3 => MonoProfiler.AssemblyRecords.OrderByDescending(x => x.alloc),
				_ => default
			})!.Where(x => string.IsNullOrEmpty(search) || x.assembly_name.displayName.Contains(search, CompareOptions.OrdinalIgnoreCase));
		}
		public static IEnumerable<MonoProfiler.CallRecord> GetSortedCalls(ModuleHandle selection, int sort, string search)
		{
			var advancedRecords = MonoProfiler.CallRecords.Where(x => selection.GetHashCode() == 0 || x.assembly_handle == selection);

			if (!advancedRecords.Any()) return advancedRecords;

			return (sort switch
			{
				0 => advancedRecords.OrderBy(x => x.method_name),
				1 => advancedRecords.OrderByDescending(x => x.calls),
				2 => advancedRecords.OrderByDescending(x => x.total_time),
				3 => advancedRecords.OrderByDescending(x => x.own_time),
				4 => advancedRecords.OrderByDescending(x => x.total_alloc),
				5 => advancedRecords.OrderByDescending(x => x.own_alloc),
				_ => advancedRecords
			})!.Where(x => string.IsNullOrEmpty(search) || x.method_name.Contains(search, CompareOptions.OrdinalIgnoreCase));;
		}
		public static IEnumerable<MonoProfiler.MemoryRecord> GetSortedMemory(int sort, string search)
		{
			var records = MonoProfiler.MemoryRecords.AsEnumerable();

			if (!records.Any()) return records;

			return (sort switch
			{
				0 => records.OrderBy(x => x.class_name),
				1 => records.OrderByDescending(x => x.allocations),
				2 => records.OrderByDescending(x => x.total_alloc_size),
				_ => records
			})!.Where(x => string.IsNullOrEmpty(search) || x.class_name.Contains(search, CompareOptions.OrdinalIgnoreCase));;
		}

		internal void Draw(PlayerSession ap)
		{
			var selection = ap.GetStorage<ModuleHandle>(null, "profilerval");

			DrawAssemblies(ap, selection);
			DrawSubtabs(ap, selection);
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

			filtered.AddRange(GetSortedAssemblies(sortIndex, searchInput));

			if (filtered.Count > 0)
			{
				maxVal = sortIndex switch
				{
					0 or 1 => filtered.Max(x => (float)x.total_time),
					2 => filtered.Max(x => (float)x.calls),
					3 => filtered.Max(x => (float)x.alloc),
					_ => maxVal
				};
			}

			AddWidget(0, 0, (ap, cui, container, panel) =>
			{
				var tabSpacing = 1;
				const float offset = -46f;

				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", "1 1 1 0.2",
					"<size=6>EXPORT\n</size>PROTO", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: offset * tabSpacing, OxMax: offset * tabSpacing, command: "adminmodule.profilerexport 3");
				tabSpacing++;

				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", $"1 1 1 {(MonoProfiler.IsCleared ? 0.2 : 0.5)}",
					"<size=6>EXPORT\n</size>CSV", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: offset * tabSpacing, OxMax: offset * tabSpacing, command: "adminmodule.profilerexport 2");
				tabSpacing++;

				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", $"1 1 1 {(MonoProfiler.IsCleared ? 0.2 : 0.5)}",
					"<size=6>EXPORT\n</size>JSON", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: offset * tabSpacing, OxMax: offset * tabSpacing, command: "adminmodule.profilerexport 1");
				tabSpacing++;

				cui.CreateProtectedButton(container, panel, "0.2 0.2 0.2 0.7", $"1 1 1 {(MonoProfiler.IsCleared ? 0.2 : 0.5)}",
					"<size=6>EXPORT\n</size>TABLE", 8,
					xMin: 0.83f, xMax: 0.925f, OxMin: offset * tabSpacing, OxMax: offset * tabSpacing, command: "adminmodule.profilerexport 0");
				tabSpacing++;

				cui.CreateProtectedButton(container, panel, !MonoProfiler.IsCleared || MonoProfiler.Recording ? "0.9 0.1 0.1 1" : "0.2 0.2 0.2 0.7", "1 1 1 0.5",
					MonoProfiler.Recording ? "ABORT" : "CLEAR", 8,
					xMin: 0.83f, xMax: 0.925f, command: "adminmodule.profilerclear");

				cui.CreateProtectedButton(container, panel,
					MonoProfiler.Recording ? "0.9 0.1 0.1 1" : "0.2 0.2 0.2 0.7", "1 1 1 0.5", "REC<size=6>\n[SHIFT]</size>", 8,
					xMin: 0.93f, xMax: 0.99f, command: "adminmodule.profilertoggle");
			});

			Stripe(this, 0, (float)filtered.Sum(x => x.total_time_percentage), 100, niceColor, niceColor,
				"All",
				$"{filtered.Sum(x => (float)x.total_time):n0}ms | {filtered.Sum(x => (float)x.total_time_percentage):0.0}%",
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

				var value = sortIndex switch
				{
					0 or 1 => record.total_time,
					2 => record.calls,
					3 => record.alloc,
					_ => 0f
				};

				Stripe(this, 0, value, maxVal, intenseColor, calmColor,
					record.assembly_name.displayName,
					$"{record.GetTotalTime()} ({record.total_time_percentage:0.0}%) | {ByteEx.Format(record.alloc).ToUpper()}",
					$"{record.assembly_name.profileType}\n<b>{record.calls:n0}</b> calls", $"adminmodule.profilerselect {i}", record.assembly_handle == selection);
			}

			if (filtered.Count == 0)
			{
				AddText(0, "No assemblies available", 8, "1 1 1 0.5");
			}

			Pool.FreeList(ref filtered);
		}
		public void DrawSubtabs(PlayerSession session, ModuleHandle selection)
		{
			AddColumn(1, true);

			var subtab = session.GetStorage<SubtabTypes>(this, "subtab", default);

			AddButtonArray(1, new OptionButton("Calls", ap =>
				{
					session.SetStorage(this, "subtab", SubtabTypes.Calls);
					DrawSubtabs(session, selection);
				}, ap => subtab == SubtabTypes.Calls ? OptionButton.Types.Selected : OptionButton.Types.None),
				new OptionButton("Memory", ap =>
				{
					session.SetStorage(this, "subtab", SubtabTypes.Memory);
					DrawSubtabs(session, selection);
				}, ap => subtab == SubtabTypes.Memory ? OptionButton.Types.Selected : OptionButton.Types.None));

			switch (subtab)
			{
				case SubtabTypes.Memory:
				{
					var searchInput = session.GetStorage(this, "msearch", string.Empty);
					var sort = session.GetStorage(this, "msort", 1);
					var advancedRecords = GetSortedMemory(sort, searchInput);
					var maxVal = 0f;

					if (advancedRecords.Any())
					{
						maxVal = sort switch
						{
							0 or 1 => advancedRecords.Max(x => (float)x.allocations),
							2 => advancedRecords.Max(x => (float)x.total_alloc_size),
							_ => maxVal
						};
					}

					AddDropdown(1, $"<b>MEMORY ({advancedRecords.Count():n0})</b>", ap => sort, (ap, i) =>
					{
						ap.SetStorage(this, "msort", i);
						DrawSubtabs(session, selection);
					}, sortMemoryOptions);

					AddInputButton(1, "Search", 0.075f, new OptionInput(null, ap => searchInput, 0, false, (ap, args) =>
					{
						ap.SetStorage(this, "msearch", args.ToString(" "));
						DrawSubtabs(ap, selection);
					}), new OptionButton("X", ap =>
					{
						ap.SetStorage(this, "msearch", string.Empty);
						DrawSubtabs(ap, selection);

					}, _ => string.IsNullOrEmpty(searchInput) ? OptionButton.Types.None : OptionButton.Types.Important));

					var index = 0;
					foreach (var record in advancedRecords)
					{
						var value = sort switch
						{
							0 or 1 => record.allocations,
							2 => record.total_alloc_size,
							_ => 0f
						};

						Stripe(this, 1, value, maxVal, intenseColor, calmColor,
							record.class_name,
							$"{record.allocations:n0} allocated | {ByteEx.Format(record.total_alloc_size).ToUpper()} total",
							$"<b>{record.instance_size} B</b>",
							string.Empty);

						index++;
					}

					if (!advancedRecords.Any())
					{
						AddText(1, "No memory records available", 8, "1 1 1 0.5");
					}

					break;
				}

				default:
				case SubtabTypes.Calls:
				{
					var searchInput = session.GetStorage(this, "asearch", string.Empty);
					var sort = session.GetStorage(this, "asort", 1);
					var advancedRecords = GetSortedCalls(selection, sort, searchInput);
					var maxVal = 0f;

					if (advancedRecords.Any())
					{
						maxVal = sort switch
						{
							0 or 1 => advancedRecords.Max(x => (float)x.calls),
							2 => advancedRecords.Max(x => (float)x.total_time),
							3 => advancedRecords.Max(x => (float)x.own_time),
							4 => advancedRecords.Max(x => (float)x.total_alloc),
							5 => advancedRecords.Max(x => (float)x.own_alloc),
							_ => maxVal
						};
					}

					AddDropdown(1, $"<b>CALLS ({advancedRecords.Count():n0})</b>", ap => sort, (ap, i) =>
					{
						ap.SetStorage(this, "asort", i);
						DrawSubtabs(session, selection);
					}, sortCallsOptions);

					AddInputButton(1, "Search", 0.075f, new OptionInput(null, ap => searchInput, 0, false, (ap, args) =>
					{
						ap.SetStorage(this, "asearch", args.ToString(" "));
						DrawSubtabs(ap, selection);
					}), new OptionButton("X", ap =>
					{
						ap.SetStorage(this, "asearch", string.Empty);
						DrawSubtabs(ap, selection);

					}, _ => string.IsNullOrEmpty(searchInput) ? OptionButton.Types.None : OptionButton.Types.Important));

					var index = 0;
					foreach (var record in advancedRecords)
					{
						var value = sort switch
						{
							0 or 1 => record.calls,
							2 => record.total_time,
							3 => record.own_time,
							4 => record.total_alloc,
							5 => record.own_alloc,
							_ => 0f
						};

						Stripe(this, 1, value, maxVal, intenseColor, calmColor,
							record.method_name.Truncate(105, "..."),
							$"{record.GetTotalTime()} total ({record.total_time_percentage:0.0}%) | {record.GetOwnTime()} own ({record.own_time_percentage:0.0}%)",
							$"<b>{record.calls:n0}</b> {(((int)record.calls).Plural("call", "calls"))}\n{ByteEx.Format(record.total_alloc).ToUpper()} total | {ByteEx.Format(record.own_alloc).ToUpper()} own",
							Community.Runtime.MonoProfilerConfig.SourceViewer
								? $"adminmodule.profilerselectcall {index}"
								: string.Empty);

						index++;
					}

					if (!advancedRecords.Any())
					{
						AddText(1, "No call records available", 8, "1 1 1 0.5");
					}

					break;
				}
			}
		}
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.profilerselect")]
	private void ProfilerSelect(ConsoleSystem.Arg arg)
	{
		var ap = GetPlayerSession(arg.Player());

		var selection = ProfilerTab.GetSortedAssemblies(ap.GetStorage(ap.SelectedTab, "bsort", 1), ap.GetStorage(ap.SelectedTab, "bsearch", string.Empty))
			.FindAt(arg.GetInt(0));
		ap.SetStorage(null, "profilerval", selection.assembly_handle);
		ap.SelectedTab.OnChange(ap, ap.SelectedTab);

		Draw(ap.Player);
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.profilerselectcall")]
	private void ProfilerSelectCall(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();

		if (!HasAccess(player, "profiler.sourceviewer"))
		{
			return;
		}

		var index = arg.GetInt(0);
		var ap = GetPlayerSession(player);

		var selection = ap.GetStorage<ModuleHandle>(null, "profilerval");
		var call = ProfilerTab.GetSortedCalls(selection, ap.GetStorage(ap.SelectedTab, "asort", 1), ap.GetStorage(ap.SelectedTab, "asearch", string.Empty))
			.FindAt(index);

		var currentTab = ap.SelectedTab;
		var tab = SourceViewerTab.MakeMethod(call);

		tab.Close = ap =>
		{
			SetTab(player, currentTab, true);
		};

		SetTab(player, tab, true);
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.profilertoggle")]
	private void ProfilerToggle(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();

		if (!HasAccess(player, "profiler.startstop"))
		{
			return;
		}

		var ap = GetPlayerSession(player);

		if (!MonoProfiler.Enabled)
		{
			return;
		}

		if (!MonoProfiler.Recording && ap.Player.serverInput.IsDown(BUTTON.SPRINT))
		{
			var dictionary = PoolEx.GetDictionary<string, ModalModule.Modal.Field>();

			dictionary["duration"] = ModalModule.Modal.Field.Make("Duration", ModalModule.Modal.Field.FieldTypes.Float, true, 3f, customIsInvalid: field => field.Value.ToString().ToFloat() <= 0 ? "Duration must be above zero." : string.Empty);
			dictionary["calls"] = ModalModule.Modal.Field.Make("Calls", ModalModule.Modal.Field.FieldTypes.Boolean, false, true);
			dictionary["advancedmemory"] = ModalModule.Modal.Field.Make("Advanced Memory", ModalModule.Modal.Field.FieldTypes.Boolean, false, true);
			dictionary["callmemory"] = ModalModule.Modal.Field.Make("Call Memory", ModalModule.Modal.Field.FieldTypes.Boolean, false, true);
			dictionary["timings"] = ModalModule.Modal.Field.Make("Timings (Performance Intensive)", ModalModule.Modal.Field.FieldTypes.Boolean, false, true);

			Modal.Open(player, "Profile Recording", dictionary, (_, _) =>
			{
				MonoProfiler.ProfilerArgs profilerArgs = default;

				if (dictionary["advancedmemory"].Get<bool>()) profilerArgs |= MonoProfiler.ProfilerArgs.AdvancedMemory;
				if (dictionary["callmemory"].Get<bool>()) profilerArgs |= MonoProfiler.ProfilerArgs.CallMemory;
				if (dictionary["calls"].Get<bool>()) profilerArgs |= MonoProfiler.ProfilerArgs.Calls;
				if (dictionary["timings"].Get<bool>()) profilerArgs |= MonoProfiler.ProfilerArgs.Timings;

				MonoProfiler.Clear();
				MonoProfiler.ToggleProfilingTimed(dictionary["duration"].Get<float>(), profilerArgs, args =>
				{
					if (ap.IsInMenu && ap.SelectedTab != null && ap.SelectedTab.Id == "profiler")
					{
						ap.SelectedTab.OnChange(ap, ap.SelectedTab);
						Draw(ap.Player);
					}
				});

				PoolEx.FreeDictionary(ref dictionary);

				ap.SelectedTab.OnChange(ap, ap.SelectedTab);
				Draw(player);
			}, onCancel: () =>
			{
				PoolEx.FreeDictionary(ref dictionary);

				ap.SelectedTab.OnChange(ap, ap.SelectedTab);
				Draw(player);
			});
		}
		else
		{
			MonoProfiler.Clear();
			MonoProfiler.ToggleProfilingTimed(0f);

			ap.SelectedTab.OnChange(ap, ap.SelectedTab);

			Draw(player);
		}
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.profilerexport")]
	private void ProfilerExport(ConsoleSystem.Arg arg)
	{
		if (MonoProfiler.IsCleared)
		{
			return;
		}

		var ap = GetPlayerSession(arg.Player());
		var index = arg.GetInt(0);

		switch (index)
		{
			case 0:
				WriteFileString("txt",
					$"{MonoProfiler.AssemblyRecords.ToTable()}\n\n{MonoProfiler.CallRecords.ToTable()}", ap.Player);
				break;

			case 1:
				WriteFileString("json",
					$"{MonoProfiler.AssemblyRecords.ToJson(true)}\n\n{MonoProfiler.CallRecords.ToJson(true)}", ap.Player);
				break;

			case 2:
				WriteFileString("csv",
					$"{MonoProfiler.AssemblyRecords.ToCSV()}\n\n{MonoProfiler.CallRecords.ToCSV()}", ap.Player);
				break;
		}

		static void WriteFileString(string extension, string data, BasePlayer player)
		{
			var date = DateTime.Now;
			var file = Path.Combine(Defines.GetProfilesFolder(), $"profile-{date.Year}_{date.Month}_{date.Day}_{date.Hour}{date.Minute}{date.Second}.{extension}");
			OsEx.File.Create(file, data);

			Notifications.Add(player, $"Stored output at '{file}'", 5);
		}
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.profilerclear")]
	private void ProfilerClear(ConsoleSystem.Arg arg)
	{
		var ap = GetPlayerSession(arg.Player());

		if (MonoProfiler.Recording)
		{
			MonoProfiler.ToggleProfiling(MonoProfiler.ProfilerArgs.Abort);
		}
		else
		{
			MonoProfiler.Clear();
			ap.SetStorage(null, "profilerval", (ModuleHandle)default);
		}

		ap.SelectedTab.OnChange(ap, ap.SelectedTab);

		Draw(ap.Player);
	}
}

#endif
