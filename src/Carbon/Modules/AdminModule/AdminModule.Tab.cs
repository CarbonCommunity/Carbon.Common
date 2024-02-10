using Oxide.Game.Rust.Cui;

namespace Carbon.Modules;

public partial class AdminModule
{
	public class Tab : IDisposable
	{
		public string Id;
		public string Name;
		public string Access = null;
		public RustPlugin Plugin;
		public Action<Tab, CUI, CuiElementContainer, string, PlayerSession> Over, Under, Override;
		public Dictionary<int, List<Option>> Columns = new();
		public Action<PlayerSession, Tab> OnChange;
		public Dictionary<string, Radio> Radios = new();
		public TabDialog Dialog;
		public bool Fullscreen;

		public Tab(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null, string access = null)
		{
			Id = id;
			Name = name;
			Plugin = plugin;
			OnChange = onChange;
			Access = access;
		}

		public void ClearColumn(int column, bool erase = false)
		{
			if (Columns.TryGetValue(column, out var rows))
			{
				rows.Clear();

				if (erase)
				{
					Columns[column] = null;
					Columns.Remove(column);
				}
			}
		}
		public void ClearAfter(int index, bool erase = false)
		{
			var count = Columns.Count;

			for (int i = 0; i < count; i++)
			{
				if (i >= index) ClearColumn(i, erase);
			}
		}
		public Tab AddColumn(int column, bool clear = false)
		{
			if (!Columns.TryGetValue(column, out var options))
			{
				Columns[column] = options = new List<Option>();
			}

			if (clear)
			{
				options.Clear();
			}

			return this;
		}
		public Tab AddRow(int column, Option row, bool hidden = false)
		{
			row.CurrentlyHidden = row.Hidden = hidden;

			if (Columns.TryGetValue(column, out var options))
			{
				options.Add(row);
			}
			else
			{

				Columns[column] = options = new List<Option>();
				options.Add(row);
			}

			return this;
		}
		public Tab AddName(int column, string name, TextAnchor align = TextAnchor.MiddleLeft, bool hidden = false)
		{
			return AddRow(column, new OptionName(name, align, null), hidden);
		}
		public Tab AddButton(int column, string name, Action<PlayerSession> callback, Func<PlayerSession, OptionButton.Types> type = null, TextAnchor align = TextAnchor.MiddleCenter, bool hidden = false)
		{
			return AddRow(column, new OptionButton(name, align, callback, type, null), hidden);
		}
		public Tab AddToggle(int column, string name, Action<PlayerSession> callback, Func<PlayerSession, bool> isOn = null, string tooltip = null, bool hidden = false)
		{
			return AddRow(column, new OptionToggle(name, callback, ap => { try { return (isOn?.Invoke(ap)).GetValueOrDefault(false); } catch (Exception ex) { Logger.Error($"AddToggle[{column}][{name}] failed", ex); } return false; }, tooltip), hidden);
		}
		public Tab AddText(int column, string name, int size, string color, TextAnchor align = TextAnchor.MiddleCenter, CUI.Handler.FontTypes font = CUI.Handler.FontTypes.RobotoCondensedRegular, bool isInput = false, bool hidden = false)
		{
			return AddRow(column, new OptionText(name, size, color, align, font, isInput, null), hidden);
		}
		public Tab AddInput(int column, string name, Func<PlayerSession, string> placeholder, int characterLimit, bool readOnly, Action<PlayerSession, IEnumerable<string>> callback = null, string tooltip = null, bool hidden = false)
		{
			return AddRow(column, new OptionInput(name, placeholder, characterLimit, readOnly, callback, tooltip), hidden);
		}
		public Tab AddInput(int column, string name, Func<PlayerSession, string> placeholder, Action<PlayerSession, IEnumerable<string>> callback = null, string tooltip = null, bool hidden = false)
		{
			return AddInput(column, name, placeholder, 0, callback == null, callback, tooltip, hidden);
		}
		public Tab AddEnum(int column, string name, Action<PlayerSession, bool> callback, Func<PlayerSession, string> text, string tooltip = null, bool hidden = false)
		{
			AddRow(column, new OptionEnum(name, callback, text, tooltip), hidden);
			return this;
		}
		public Tab AddRadio(int column, string name, string id, bool wantsOn, Action<bool, PlayerSession> callback = null, string tooltip = null, bool hidden = false)
		{
			if (!Radios.TryGetValue(id, out var radio))
			{
				Radios[id] = radio = new();
			}

			radio.TemporaryIndex++;
			if (wantsOn) radio.Selected = radio.TemporaryIndex;

			var index = radio.TemporaryIndex;
			var option = new OptionRadio(name, id, index, wantsOn, callback, radio, tooltip);
			radio.Options.Add(option);

			return AddRow(column, option, hidden);
		}
		public Tab AddDropdown(int column, string name, Func<PlayerSession, int> index, Action<PlayerSession, int> callback, string[] options, string[] optionsIcons = null, float optionsIconScale = 0f, string tooltip = null, bool hidden = false)
		{
			return AddRow(column, new OptionDropdown(name, index, callback, options, optionsIcons, optionsIconScale, tooltip), hidden);
		}
		public Tab AddRange(int column, string name, float min, float max, Func<PlayerSession, float> value, Action<PlayerSession, float> callback, Func<PlayerSession, string> text = null, string tooltip = null, bool hidden = false)
		{
			return AddRow(column, new OptionRange(name, min, max, value, callback, text, tooltip), hidden);
		}
		public Tab AddButtonArray(int column, float spacing, params OptionButton[] buttons)
		{
			return AddRow(column, new OptionButtonArray(string.Empty, spacing, null, false, buttons));
		}
		public Tab AddButtonArray(int column, params OptionButton[] buttons)
		{
			return AddRow(column, new OptionButtonArray(string.Empty, 0.01f, null, false, buttons));
		}
		public Tab AddInputButton(int column, string name, float buttonPriority, OptionInput input, OptionButton button, string tooltip = null, bool hidden = false)
		{
			return AddRow(column, new OptionInputButton(name, buttonPriority, input, button, tooltip), hidden);
		}
		public Tab AddColor(int column, string name, Func<string> color, Action<PlayerSession, string, string, float> callback, string tooltip = null, bool hidden = false)
		{
			return AddRow(column, new OptionColor(name, color, callback, tooltip), hidden);
		}

		public void CreateDialog(string title, Action<PlayerSession> onConfirm, Action<PlayerSession> onDecline)
		{
			Dialog = new TabDialog(title, onConfirm, onDecline);
		}
		public void ResetHiddens()
		{
			foreach (var column in Columns)
			{
				foreach (var row in column.Value)
				{
					row.CurrentlyHidden = row.Hidden;
				}
			}
		}
		public void Dispose()
		{
			foreach (var column in Columns)
			{
				column.Value.Clear();
			}

			Columns.Clear();
			Columns = null;
		}

		public class Radio : IDisposable
		{
			public int Selected;

			public int TemporaryIndex = -1;

			public List<OptionRadio> Options = new();

			public void Change(int index, PlayerSession ap)
			{
				Options[Selected]?.Callback?.Invoke(false, ap);

				Selected = index;
			}

			public void Dispose()
			{
				Options.Clear();
				Options = null;
			}
		}
		public class TabDialog
		{
			public string Title;
			public Action<PlayerSession> OnConfirm, OnDecline;

			public TabDialog(string title, Action<PlayerSession> onConfirm, Action<PlayerSession> onDecline)
			{
				Title = title;
				OnConfirm = onConfirm;
				OnDecline = onDecline;
			}
		}

		public class Option
		{
			public string Name;
			public string Tooltip;
			public bool Hidden;
			public bool CurrentlyHidden;

			public Option(string name, string tooltip = null, bool hidden = false)
			{
				Name = name;
				Tooltip = tooltip;
				CurrentlyHidden = Hidden = hidden;
			}
		}
		public class OptionName : Option
		{
			public TextAnchor Align;

			public OptionName(string name, TextAnchor align, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden) { Align = align; }
		}
		public class OptionText : Option
		{
			public int Size;
			public string Color;
			public TextAnchor Align;
			public CUI.Handler.FontTypes Font;
			public bool IsInput;

			public OptionText(string name, int size, string color, TextAnchor align, CUI.Handler.FontTypes font, bool isInput, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden) { Align = align; Size = size; Color = color; Font = font; IsInput = isInput; }
		}
		public class OptionInput : Option
		{
			public Func<PlayerSession, string> Placeholder;
			public int CharacterLimit;
			public bool ReadOnly;
			public Action<PlayerSession, IEnumerable<string>> Callback;

			public OptionInput(string name, Func<PlayerSession, string> placeholder, int characterLimit, bool readOnly, Action<PlayerSession, IEnumerable<string>> args, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Placeholder = ap => { try { return placeholder?.Invoke(ap); } catch (Exception ex) { Logger.Error($"Failed OptionInput.Placeholder callback ({name}): {ex.Message}"); return string.Empty; } };
				Callback = (ap, args2) => { try { args?.Invoke(ap, args2); } catch (Exception ex) { Logger.Error($"Failed OptionInput.Callback callback ({name}): {ex.Message}"); } };
				CharacterLimit = characterLimit;
				ReadOnly = readOnly;
			}
		}
		public class OptionButton : Option
		{
			public Func<PlayerSession, Types> Type;
			public Action<PlayerSession> Callback;
			public TextAnchor Align = TextAnchor.MiddleCenter;

			public enum Types
			{
				None,
				Selected,
				Warned,
				Important
			}

			public OptionButton(string name, TextAnchor align, Action<PlayerSession> callback, Func<PlayerSession, Types> type = null, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Align = align;
				Callback = (ap) => { try { callback?.Invoke(ap); } catch (Exception ex) { Logger.Error($"Failed OptionButton.Callback callback ({name}): {ex.Message}"); } };
				Type = (ap) => { try { return (type?.Invoke(ap)).GetValueOrDefault(Types.None); } catch (Exception ex) { Logger.Error($"Failed OptionButton.Type callback ({name}): {ex.Message}"); return Types.None; } };
			}
			public OptionButton(string name, Action<PlayerSession> callback, Func<PlayerSession, Types> type = null, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Callback = callback;
				Type = type;
			}
		}
		public class OptionToggle : Option
		{
			public Func<PlayerSession, bool> IsOn;
			public Action<PlayerSession> Callback;

			public OptionToggle(string name, Action<PlayerSession> callback, Func<PlayerSession, bool> isOn = null, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Callback = (ap) => { try { callback?.Invoke(ap); } catch (Exception ex) { Logger.Error($"Failed OptionToggle.Callback callback ({name}): {ex.Message}"); } };
				IsOn = (ap) => { try { return (isOn?.Invoke(ap)).GetValueOrDefault(false); } catch (Exception ex) { Logger.Error($"Failed OptionToggle.IsOn callback ({name}): {ex.Message}"); return false; } };
			}
		}
		public class OptionEnum : Option
		{
			public Func<PlayerSession, string> Text;
			public Action<PlayerSession, bool> Callback;

			public OptionEnum(string name, Action<PlayerSession, bool> callback, Func<PlayerSession, string> text, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Callback = (ap, value) => { try { callback?.Invoke(ap, value); } catch (Exception ex) { Logger.Error($"Failed OptionEnum.Callback callback ({name}): {ex.Message}"); } };
				Text = (ap) => { try { return text?.Invoke(ap); } catch (Exception ex) { Logger.Error($"Failed OptionToggle.Callback callback ({name}): {ex.Message}"); return string.Empty; } };
			}
		}
		public class OptionRange : Option
		{
			public float Min = 0;
			public float Max = 1;
			public Func<PlayerSession, float> Value;
			public Action<PlayerSession, float> Callback;
			public Func<PlayerSession, string> Text;

			public OptionRange(string name, float min, float max, Func<PlayerSession, float> value, Action<PlayerSession, float> callback, Func<PlayerSession, string> text, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Min = min;
				Max = max;
				Callback = (ap, value) => { try { callback?.Invoke(ap, value); } catch (Exception ex) { Logger.Error($"Failed OptionRange.Callback callback ({name}): {ex.Message}"); } };
				Value = (ap) => { try { return (value?.Invoke(ap)).GetValueOrDefault(0); } catch (Exception ex) { Logger.Error($"Failed OptionRange.Callback callback ({name}): {ex.Message}"); return 0f; } };
				Text = (ap) => { try { return text?.Invoke(ap); } catch (Exception ex) { Logger.Error($"Failed OptionRange.Callback callback ({name}): {ex.Message}"); return string.Empty; } };
			}
		}
		public class OptionRadio : Option
		{
			public string Id;
			public int Index;
			public bool WantsOn;
			public Action<bool, PlayerSession> Callback;

			public Radio Radio;

			public OptionRadio(string name, string id, int index, bool on, Action<bool, PlayerSession> callback, Radio radio, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Id = id;
				Callback = (value, ap) => { try { callback?.Invoke(value, ap); } catch (Exception ex) { Logger.Error($"Failed OptionRadio.Callback callback ({name}): {ex.Message}"); } };
				WantsOn = on;
				Index = index;
				Radio = radio;
			}
		}
		public class OptionDropdown : Option
		{
			public Func<PlayerSession, int> Index;
			public Action<PlayerSession, int> Callback;
			public string[] Options;
			public string[] OptionsIcons;
			public float OptionsIconScale;

			public OptionDropdown(string name, Func<PlayerSession, int> index, Action<PlayerSession, int> callback, string[] options, string[] optionsIcons, float optionsIconScale, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Index = (ap) => { try { return (index?.Invoke(ap)).GetValueOrDefault(0); } catch (Exception ex) { Logger.Error($"Failed OptionRange.Callback callback ({name}): {ex.Message}"); return 0; } };
				Callback = (ap, value) => { try { callback?.Invoke(ap, value); } catch (Exception ex) { Logger.Error($"Failed OptionRange.Callback callback ({name}): {ex.Message}"); } };
				Options = options;
				OptionsIcons = optionsIcons;
				OptionsIconScale = optionsIconScale;
			}
		}
		public class OptionInputButton : Option
		{
			public OptionInput Input;
			public OptionButton Button;
			public float ButtonPriority = 0.25f;

			public OptionInputButton(string name, float buttonPriority, OptionInput input, OptionButton button, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				ButtonPriority = buttonPriority;
				Input = input;
				Button = button;
			}
		}
		public class OptionButtonArray : Option
		{
			public OptionButton[] Buttons;
			public float Spacing = 0.01f;

			public OptionButtonArray(string name, float spacing, string tooltip = null, bool hidden = false, params OptionButton[] buttons) : base(name, tooltip, hidden)
			{
				Buttons = buttons;
				Spacing = spacing;
			}
		}
		public class OptionColor : Option
		{
			public Func<string> Color;
			public Action<PlayerSession, string, string, float> Callback;

			public OptionColor(string name, Func<string> color, Action<PlayerSession, string, string, float> callback, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Color = color;
				Callback = callback;
			}
		}
	}

	public class DynamicTab : Tab
	{
		public DynamicTab(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null) : base(id, name, plugin, onChange) { }
	}
}
