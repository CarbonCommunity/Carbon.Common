namespace Carbon.Modules;

public partial class AdminModule
{
#if !MINIMAL
	[Conditional("!MINIMAL")]
	[ProtectedCommand(PanelId + ".changetab")]
	private void ChangeTab(ConsoleSystem.Arg args)
	{
		var player = args.Player();
		var ap = GetPlayerSession(player);
		var previous = ap.SelectedTab;

		ap.Clear();

		if (int.TryParse(args.Args[0], out int index))
		{
			SetTab(player, index);
			ap.SelectedTab = Tabs[index];
		}
		else
		{
			var indexOf = Tabs.IndexOf(previous);
			indexOf = args.Args[0] == "up" ? indexOf + 1 : indexOf - 1;

			if (indexOf > Tabs.Count - 1) indexOf = 0;
			else if (indexOf < 0) indexOf = Tabs.Count - 1;

			SetTab(player, indexOf);
		}
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand(PanelId + ".callaction")]
	private void CallAction(ConsoleSystem.Arg args)
	{
		var player = args.Player();

		if (CallColumnRow(player, args.Args[0].ToInt(), args.Args[1].ToInt(), args.Args.Skip(2).Count() > 0 ? args.Args.Skip(2) : Array.Empty<string>()))
			Draw(player);
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand(PanelId + ".changecolumnpage")]
	private void ChangeColumnPage(ConsoleSystem.Arg args)
	{
		var player = args.Player();
		var instance = GetPlayerSession(player);
		var page = instance.GetOrCreatePage(args.Args[0].ToInt());
		var type = args.Args[1].ToInt();

		switch (type)
		{
			case 0:
				page.CurrentPage--;
				if (page.CurrentPage < 0) page.CurrentPage = page.TotalPages;
				break;

			case 1:
				page.CurrentPage++;
				if (page.CurrentPage > page.TotalPages) page.CurrentPage = 0;
				break;

			case 2:
				page.CurrentPage = 0;
				break;

			case 3:
				page.CurrentPage = page.TotalPages;
				break;

			case 4:
				page.CurrentPage = (args.Args[2].ToInt() - 1).Clamp(0, page.TotalPages);
				break;
		}

		Draw(player);
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand(PanelId + ".close")]
	private void CloseUI(ConsoleSystem.Arg args)
	{
		Close(args.Player());
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand(PanelId + ".dialogaction")]
	private void Dialog_Action(ConsoleSystem.Arg args)
	{
		var player = args.Player();
		var admin = GetPlayerSession(player);
		var tab = GetTab(player);
		var dialog = tab?.Dialog;
		if (tab != null) tab.Dialog = null;

		switch (args.Args[0])
		{
			case "confirm":
				try { dialog?.OnConfirm(admin); } catch { }
				break;

			case "decline":
				try { dialog?.OnDecline(admin); } catch { }
				break;
		}

		Draw(player);
	}
#endif
}
