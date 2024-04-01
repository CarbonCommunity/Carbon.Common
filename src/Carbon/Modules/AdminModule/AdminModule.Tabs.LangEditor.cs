#if !MINIMAL

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StringEx = Carbon.Extensions.StringEx;
using static Carbon.Components.CUI;

namespace Carbon.Modules;

public partial class AdminModule
{
	public class LangEditor : Tab
	{
		internal BaseHookable Plugin;
		internal Action<PlayerSession> OnCancel;
		internal const string Spacing = " ";

		public LangEditor(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null) : base(id, name, plugin, onChange)
		{
		}

		public static LangEditor Make(Plugin plugin, Action<PlayerSession> onCancel)
		{
			var tab = new LangEditor("langeditor", "Lang Editor", Community.Runtime.CorePlugin)
			{
				Plugin = plugin,
				OnCancel = onCancel,
			};

			tab._draw();
			return tab;
		}

		internal void _draw()
		{
			AddColumn(0, true);
			AddColumn(1, true);

			AddButton(0, "Cancel", ap => { OnCancel?.Invoke(ap); }, ap => OptionButton.Types.Important);

			foreach (var folder in Directory.GetDirectories(Defines.GetLangFolder()))
			{
				var files = Directory.GetFiles(folder);

				if (files.Length == 0)
				{
					continue;
				}

				var pluginFiles = files.Where(x => x.Contains(Plugin.Name, CompareOptions.OrdinalIgnoreCase));

				if (!pluginFiles.Any())
				{
					AddText(0, $"No localisation files found for '{Plugin.ToPrettyString()}'", 10, "1 1 1 0.5");
					continue;
				}

				var file = pluginFiles.FirstOrDefault();

				AddButton(0, Path.GetFileName(folder), ap =>
				{
					Singleton.SetTab(ap.Player, ConfigEditor.Make(OsEx.File.ReadText(file),
						(ap, jobject) =>
						{
							Community.Runtime.CorePlugin.NextTick(() => Singleton.SetTab(ap.Player, "plugins", false));
						},
						(ap, jobject) =>
						{
							OsEx.File.Create(file, jobject.ToString(Formatting.Indented));
							Community.Runtime.CorePlugin.NextTick(() => Singleton.SetTab(ap.Player, "plugins", false));
						},
						(ap, jobject) =>
						{
							OsEx.File.Create(file, jobject.ToString(Formatting.Indented));

							if (Plugin is RustPlugin rustPlugin)
							{
								rustPlugin.ProcessorProcess.MarkDirty();
							}

							Community.Runtime.CorePlugin.NextTick(() => Singleton.SetTab(ap.Player, "plugins", false));
						}));
				}, ap => OptionButton.Types.Warned);
			}
		}
	}
}

#endif
