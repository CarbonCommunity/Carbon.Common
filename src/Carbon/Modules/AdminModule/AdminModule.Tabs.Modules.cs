#if !MINIMAL

/*
*
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using Newtonsoft.Json;

namespace Carbon.Modules;

public partial class AdminModule : CarbonModule<AdminConfig, AdminData>
{
	public class ModulesTab
	{
		public static Tab Get()
		{
			var tab = (Tab)null;
			void Draw(PlayerSession ap)
			{
				tab.AddColumn(0, true);
				tab.AddColumn(1, true);

				var searchInput = ap.GetStorage<string>(tab, "search")?.ToLower();

				tab.AddInput(0, "Search", ap => searchInput, (ap, args) =>
				{
					ap.SetStorage(tab, "search", args.ToString(" "));
					Draw(ap);
				});

				tab.AddName(0, "Core Modules");
				Generate(x => x.ForceEnabled && (ap.HasStorage(tab, "search") && !string.IsNullOrEmpty(searchInput) ? x.Name.ToLower().Contains(searchInput) : true));

				tab.AddName(0, "Other Modules");
				Generate(x => !x.ForceEnabled && (ap.HasStorage(tab, "search") && !string.IsNullOrEmpty(searchInput) ? x.Name.ToLower().Contains(searchInput) : true));

				void Generate(Func<BaseModule, bool> condition)
				{
					foreach (var hookable in Community.Runtime.ModuleProcessor.Modules)
					{
						if (hookable is BaseModule module)
						{
							if (!condition(module)) continue;

							tab.AddButtonArray(0,
								new Tab.OptionButton(hookable.Name, ap =>
								{
									ap.SetStorage(tab, "selectedmodule", module);
									Draw(ap);
									DrawModuleSettings(tab, module, ap);
								}, type: ap => ap.GetStorage<BaseModule>(tab, "selectedmodule") == module ? Tab.OptionButton.Types.Selected : Tab.OptionButton.Types.None),
								new Tab.OptionButton($"{(module.ForceEnabled ? "Always Enabled" : module.IsEnabled() ? "Enabled" : "Disabled")}", ap =>
								{
									if (module.ForceEnabled) return;

									module.SetEnabled(!module.IsEnabled());
									module.Save();
									ap.SetStorage(tab, "selectedmodule", module);
									Draw(ap);
									DrawModuleSettings(tab, module, ap);
								}, type: ap => module.ForceEnabled ? Tab.OptionButton.Types.Warned : module.IsEnabled() ? Tab.OptionButton.Types.Selected : Tab.OptionButton.Types.None));
						}
					}
				}
			}

			tab = new Tab("modules", "Modules", Community.Runtime.Core, access: "modules.use", onChange: (ap, tab) =>
			{
				ap.ClearStorage(tab, "selectedmodule");
				Draw(ap);
			});

			return tab;
		}

		internal static string[] _configBlacklist = new[]
		{
			"Version"
		};

		internal static void DrawModuleSettings(Tab tab, BaseModule module, PlayerSession ap)
		{
			tab.ClearColumn(1);

			tab.AddInput(1, "Name", ap => module.Name, null);

			if (!module.ForceEnabled)
			{
				tab.AddToggle(1, "Enabled", ap2 => { module.SetEnabled(!module.IsEnabled()); module.Save(); DrawModuleSettings(tab, module, ap); }, ap2 => module.IsEnabled());
			}

			tab.AddButtonArray(1,
				new Tab.OptionButton("Save", ap => { module.Save(); }),
				new Tab.OptionButton("Load", ap => { module.Load(); }));

			if (Singleton.HasAccess(ap.Player, "modules.config_edit"))
			{
				tab.AddButton(1, "Edit Config", ap =>
				{
					var moduleConfigFile = Path.Combine(Defines.GetModulesFolder(), module.Name, "config.json");
					ap.SelectedTab = ConfigEditor.Make(OsEx.File.ReadText(moduleConfigFile),
						(ap, jobject) =>
						{
							Singleton.SetTab(ap.Player, "modules");
							Singleton.Draw(ap.Player);
						},
						(ap, jobject) =>
						{
							var wasEnabled = module.IsEnabled();
							OsEx.File.Create(moduleConfigFile, jobject.ToString(Formatting.Indented));
							module.SetEnabled(false);
							module.Reload();

							if(wasEnabled) module.SetEnabled(wasEnabled);

							Singleton.SetTab(ap.Player, "modules");
							Singleton.Draw(ap.Player);
						}, null, _configBlacklist);
				});
			}
		}
	}
}

#endif
