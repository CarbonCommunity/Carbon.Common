/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Windows.Controls;
using API.Abstracts;
using ConVar;
using Oxide.Game.Rust.Cui;
using UnityEngine.UI;
using static Carbon.Components.CUI;
using static ConsoleSystem;

namespace Carbon.Modules;

public partial class FileModule : CarbonModule<EmptyModuleConfig, EmptyModuleData>
{
	public static FileModule Instance { get; internal set; }
	public AdminModule Admin { get; internal set; }
	public readonly Handler Handler = new();

	public override string Name => "File";
	public override VersionNumber Version => new(1, 0, 0);
	public override Type Type => typeof(FileModule);
	public override bool EnabledByDefault => true;
	public override bool ForceEnabled => true;

	public override bool InitEnd()
	{
		Instance = this;
		Admin = GetModule<AdminModule>();
		return base.InitEnd();
	}

	public File Open(BasePlayer player,
					  string title,
					  string directory,
					  string extension,
					  Action<BasePlayer, File> onConfirm = null,
					  Action<BasePlayer, File> onCancel = null)
	{
		var file = new File
		{
			Title = title,
			Directory = directory,
			Extension = extension,
			OnCancel = onCancel,
			OnConfirm = onConfirm,
			Player = player,
			Handler = new()
		};

		NextFrame(() =>
		{
			file.Fetch();
			file.Draw(player);
		});

		return file;
	}
	public void Close(BasePlayer player)
	{
		using var cui = new CUI(Handler);
		cui.Destroy(File.PanelId, player);
	}

	public class File
	{
		public string Title;
		public string Directory;
		public string Extension;
		public Action<BasePlayer, File> OnConfirm;
		public Action<BasePlayer, File> OnCancel;
		public string BackgroundColor = "0 0 0 0.99";

		public string SelectedFile;
		public string DeletingFile;

		internal Handler Handler;
		internal const string PanelId = "carbonfileui";
		internal BasePlayer Player;
		internal List<DirectoryFile> DirectoryFiles = new();

		public struct DirectoryFile
		{
			public string Path;
			public FileInfo Info;

			public static DirectoryFile Get(string path)
			{
				DirectoryFile file = default;
				file.Path = path;
				file.Info = new(path);
				return file;
			}
		}

		public void Fetch()
		{
			DirectoryFiles.Clear();

			if (!System.IO.Directory.Exists(Directory))
			{
				return;
			}

			DirectoryFiles.AddRange(System.IO.Directory.EnumerateFiles(Directory,
				$"*.{Extension}").Select(x => DirectoryFile.Get(x)));
		}

		public void Draw(BasePlayer player)
		{
			var ap = Instance.Admin.GetPlayerSession(player);

			using var cui = new CUI(Handler);
			var container = cui.CreateContainer(PanelId,
				color: BackgroundColor,
				xMin: 0, xMax: 1, yMin: 0, yMax: 1,
				needsCursor: true, destroyUi: PanelId);

			var background = cui.CreatePanel(container, PanelId, "0.05 0.05 0.05 0.9", xMin: 0.5f, xMax: 0.5f,
				yMin: 0.5f, yMax: 0.5f, OxMin: -300, OxMax: 300, OyMin: -250, OyMax: 250);
			cui.CreateText(container, background, Cache.CUI.WhiteColor, $"{Title.ToUpper()} ({DirectoryFiles.Count:n0})", 20,
				align: TextAnchor.UpperLeft, xMin: 0.03f, yMax: 0.97f, font: Handler.FontTypes.RobotoCondensedBold);

			const float nameSpace = 0f;
			const float dateSpace = 0.5f;
			const float sizeSpace = 0.75f;

			var bar = cui.CreatePanel(container, background, Cache.CUI.BlankColor, yMax: 0.9f);
			cui.CreateText(container, bar, "1 1 1 0.2", "NAME", 12, align: TextAnchor.UpperLeft,
				xMin: nameSpace + 0.03f);
			cui.CreateText(container, bar, "1 1 1 0.2", "DATE", 12, align: TextAnchor.UpperLeft, xMin: dateSpace);
			cui.CreateText(container, bar, "1 1 1 0.2", "SIZE", 12, align: TextAnchor.UpperCenter,
				xMin: sizeSpace, xMax: sizeSpace + 0.05f);

			var exit = cui.CreateProtectedButton(container, background, "0.5 0 0 0.4", Cache.CUI.BlankColor, string.Empty,
				0, xMin: 0.955f, xMax: 0.99f, yMin: 0.95f, yMax: 0.99f, command: "file.action cancel");
			cui.CreateImage(container, exit, "close", "1 0.5 0.5 0.3", xMin: 0.2f, xMax: 0.8f, yMin: 0.2f, yMax: 0.8f);

			var scroll = cui.CreateScrollView(container, background, true, false,
				ScrollRect.MovementType.Elastic, 0.1f,
				true, 0.1f, 30, "0 0", out var content, out _,
				out var verticalScrollbar,
				yMax: 0.85f, xMin: 0.01f, OyMin: 0.04f);

			cui.CreatePanel(container, scroll, Cache.CUI.BlankColor);

			verticalScrollbar.Size = 3f;

			var fileOffset = 0f;
			const float scale = 25f;
			const float spacing = 3f;
			var pageScale = DirectoryFiles.Count * (scale + spacing);

			content.AnchorMin = "0 0";
			content.AnchorMax = "0.985 0";
			content.OffsetMin = $"0 {-pageScale.Clamp(425, float.MaxValue)}";
			content.OffsetMax = $"0 0";

			for (int i = 0; i < DirectoryFiles.Count; i++)
			{
				var file = DirectoryFiles[i];
				var fileButton = cui.CreateProtectedButton(container, scroll, "0.3 0.3 0.3 0.5",
					"1 1 1 0.7", $"{Path.GetFileName(file.Path)}", 10, yMin: 1, yMax: 1, OyMin: fileOffset - scale,
					OyMax: fileOffset, align: TextAnchor.MiddleLeft, command: $"file.action select {i}");

				var modificationDate = file.Info.LastWriteTime;
				cui.CreateText(container, fileButton, "1 1 1 0.4",
					$"{modificationDate.Month:00} {modificationDate.Day:00} {modificationDate.Year:0000} - {modificationDate.Hour:00}:{modificationDate.Minute:00}",
					10, xMin: dateSpace - 0.1f, xMax: 0.65f, align: TextAnchor.MiddleCenter);
				cui.CreateText(container, fileButton, "1 1 1 0.4",
					$"{ByteEx.Format(file.Info.Length).ToUpper()}",
					10, xMin: sizeSpace - 0.04f, xMax: sizeSpace + 0.1f, align: TextAnchor.MiddleCenter);

				var deleteButton = cui.CreateProtectedButton(container, fileButton, "0.5 0 0 0.4", "1 0.5 0.5 0.3", "DELETE", 8,
					xMin: 0.9f, command: $"file.action delete {i}", id: $"filedelete{i}");
				deleteButton.Element2.Name = $"filedeletetext{i}";

				fileOffset -= scale + spacing;
			}

			ap.SetStorage(null, "file", this);
			cui.Send(container, player);
		}
	}

	#region Commands

	[ProtectedCommand("file.action")]
	private void FileAction(Arg arg)
	{
		var ap = Admin.GetPlayerSession(arg.Player());
		var file = ap.GetStorage<File>(null, "file");

		var mode = arg.GetString(0);

		switch (mode)
		{
			case "select":
				file.SelectedFile = file.DirectoryFiles[arg.GetInt(1)].Path;
				file.OnConfirm?.Invoke(ap.Player, file);
				Instance.Close(ap.Player);
				break;

			case "cancel":
				file.OnConfirm?.Invoke(ap.Player, file);
				Instance.Close(ap.Player);
				break;

			case "delete":
			{
				var index = arg.GetInt(1);
				var path = file.DirectoryFiles[index].Path;

				if (file.DeletingFile == path)
				{
					OsEx.File.Delete(path);
					file.DirectoryFiles.RemoveAll(x => x.Path == path);
					file.DeletingFile = null;
					file.Draw(ap.Player);
				}

				if (string.IsNullOrEmpty(file.DeletingFile))
				{
					using var cui = new CUI(Handler);
					using var update = cui.UpdatePool();

					update.Add(cui.UpdateProtectedButton($"filedelete{index}", "0 0.5 0 0.4", Cache.CUI.BlankColor,
						string.Empty, 0, command: $"file.action delete {index}"));
					update.Add(cui.UpdateText($"filedeletetext{index}", "0 1 0 0.4", "CONFIRM", 8));
					update.Send(ap.Player);

					file.DeletingFile = path;

					Community.Runtime.Core.timer.In(0.75f, () =>
					{
						using var cui = new CUI(Handler);
						using var update = cui.UpdatePool();

						update.Add(cui.UpdateProtectedButton($"filedelete{index}", "0.5 0 0 0.4", Cache.CUI.BlankColor,
							string.Empty, 0, command: $"file.action delete {index}"));
						update.Add(cui.UpdateText($"filedeletetext{index}", "1 0.5 0.5 0.3", "DELETE", 8));
						update.Send(ap.Player);

						file.DeletingFile = null;
					});
				}

				break;
			}
		}
	}

	#endregion
}
