﻿using Newtonsoft.Json;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Oxide.Core.Libraries;

public class Lang : Library
{
	public Dictionary<string, Dictionary<string, string>> Phrases { get; set; } = new();

	public Lang(BaseHookable plugin)
	{
		foreach (var directory in Directory.EnumerateDirectories(Defines.GetLangFolder()))
		{
			var lang = Path.GetFileName(directory);
			var messages = GetMessageFile(plugin.Name, lang);

			if (messages != null)
			{
				Phrases[lang] = messages;
			}
		}
	}

	public string GetLanguage(string userId)
	{
		if (!string.IsNullOrEmpty(userId) && Interface.Oxide.Permission.UserExists(userId, out var data))
		{
			return data.Language;
		}

		return Community.Runtime.Config.Language;
	}
	public string[] GetLanguages(Plugin plugin = null)
	{
		var list = Facepunch.Pool.GetList<string>();

		foreach (string text in Directory.GetDirectories(Interface.Oxide.LangDirectory))
		{
			if (Directory.GetFiles(text).Length != 0 && (plugin == null || (plugin != null && OsEx.File.Exists(Path.Combine(text, plugin.Name + ".json")))))
			{
				list.Add(text[(Interface.Oxide.LangDirectory.Length + 1)..]);
			}
		}

		var result = list.ToArray();
		Facepunch.Pool.FreeList(ref list);
		return result;
	}
	public void SetLanguage(string lang, string userId)
	{
		if (string.IsNullOrEmpty(lang) || string.IsNullOrEmpty(userId)) return;

		if (Interface.Oxide.Permission.UserExists(userId, out var data) && data.Language != lang)
		{
			data.Language = lang;
			Interface.Oxide.Permission.SaveData();
		}
	}
	public void SetServerLanguage(string lang)
	{
		if (string.IsNullOrEmpty(lang) || lang == Community.Runtime.Config.Language) return;

		Community.Runtime.Config.Language = lang;
		Community.Runtime.SaveConfig();
	}
	public string GetServerLanguage()
	{
		return Community.Runtime.Config.Language;
	}
	private Dictionary<string, string> GetMessageFile(string plugin, string lang = "en")
	{
		if (string.IsNullOrEmpty(plugin)) return null;

		var invalidFileNameChars = Path.GetInvalidFileNameChars();

		foreach (char oldChar in invalidFileNameChars)
		{
			lang = lang.Replace(oldChar, '_');
		}

		var path = Path.Combine(Defines.GetLangFolder(), lang, $"{plugin}.json");

		if (!OsEx.File.Exists(path))
		{
			return null;
		}

		return JsonConvert.DeserializeObject<Dictionary<string, string>>(OsEx.File.ReadText(path));
	}
	private void SaveMessageFile(string plugin, string lang = "en")
	{
		if (Phrases.TryGetValue(lang, out var messages))
		{
			var folder = Path.Combine(Defines.GetLangFolder(), lang);
			OsEx.Folder.Create(folder);

			OsEx.File.Create(Path.Combine(folder, $"{plugin}.json"), JsonConvert.SerializeObject(messages, Formatting.Indented)); ;
		}
	}

	public void RegisterMessages(Dictionary<string, string> newPhrases, BaseHookable plugin, string lang = "en")
	{
		if (!Phrases.TryGetValue(lang, out var phrases))
		{
			Phrases.Add(lang, phrases = newPhrases);
		}

		var save = false;

		foreach (var phrase in newPhrases)
		{
			if (!phrases.TryGetValue(phrase.Key, out var value))
			{
				phrases.Add(phrase.Key, phrase.Value);
				save = true;
			}
		}

		if (newPhrases == phrases || save) SaveMessageFile(plugin.Name, lang);
	}

	public string GetMessage(string key, BaseHookable hookable, string player = null, string lang = null)
	{
		if (string.IsNullOrEmpty(lang)) lang = GetLanguage(player);

		if (Phrases.TryGetValue(lang, out var messages) && messages.TryGetValue(key, out var phrase))
		{
			return phrase;
		}
		else
		{
			try
			{
				if (hookable is RustPlugin rustPlugin) rustPlugin.ILoadDefaultMessages();
				messages = GetMessageFile(hookable.Name, lang);

				if (messages.TryGetValue(key, out phrase))
				{
					return phrase;
				}
			}
			catch { }
		}

		return lang == "en" ? key : GetMessage(key, hookable, player, "en");
	}
	public Dictionary<string, string> GetMessages(string lang, BaseHookable plugin)
	{
		return GetMessageFile(plugin.Name, lang);
	}
}
