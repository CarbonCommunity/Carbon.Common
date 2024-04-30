﻿using System.Windows.Media;
using Facepunch;
using Oxide.Game.Rust.Cui;

namespace Carbon.Modules;

public partial class AdminModule
{
	public class Tab : IDisposable
	{
		public string Id;
		public string Name;
		public string Access;
		public RustPlugin Plugin;
		public Action<Tab, CUI, CuiElementContainer, string, PlayerSession> Over, Under, Override;
		public Dictionary<int, OptionPool> Columns = new();
		public Action<PlayerSession, Tab> OnChange;
		public Dictionary<string, Radio> Radios = new();
		public TabDialog Dialog;
		public bool IsFullscreen;

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
				rows.ClearToPool();

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
				Columns[column] = options = new();
			}

			if (clear)
			{
				options.ClearToPool();
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

				Columns[column] = options = new();
				options.Add(row);
			}

			return this;
		}
		public Tab AddName(int column, string name, TextAnchor align = TextAnchor.MiddleLeft, bool hidden = false)
		{
			var option = Pool.Get<OptionName>();
			option.Name = name;
			option.Align = align;

			return AddRow(column, new OptionName(name, align), hidden);
		}
		public Tab AddButton(int column, string name, Action<PlayerSession> callback, Func<PlayerSession, OptionButton.Types> type = null, TextAnchor align = TextAnchor.MiddleCenter, bool hidden = false)
		{
			var option = Pool.Get<OptionButton>();
			option.Name = name;
			option.Align = align;
			option.Callback = callback;
			option.Type = type;

			return AddRow(column, option, hidden);
		}
		public Tab AddToggle(int column, string name, Action<PlayerSession> callback, Func<PlayerSession, bool> isOn = null, string tooltip = null, bool hidden = false)
		{
			var option = Pool.Get<OptionToggle>();
			option.Name = name;
			option.Callback = callback;
			option.IsOn = ap =>
			{
				try
				{
					return (isOn?.Invoke(ap)).GetValueOrDefault(false);
				}
				catch (Exception ex)
				{
					Logger.Error($"AddToggle[{column}][{name}] failed", ex);
				}

				return false;
			};
			option.Tooltip = tooltip;

			return AddRow(column, option, hidden);
		}
		public Tab AddText(int column, string name, int size, string color, TextAnchor align = TextAnchor.MiddleCenter, CUI.Handler.FontTypes font = CUI.Handler.FontTypes.RobotoCondensedRegular, bool isInput = false, bool hidden = false)
		{
			var option = Pool.Get<OptionText>();
			option.Name = name;
			option.Size = size;
			option.Color = color;
			option.Align = align;
			option.Font = font;
			option.IsInput = isInput;

			return AddRow(column, option, hidden);
		}
		public Tab AddInput(int column, string name, Func<PlayerSession, string> placeholder, int characterLimit, bool readOnly, Action<PlayerSession, IEnumerable<string>> callback = null, string tooltip = null, bool hidden = false)
		{
			var option = Pool.Get<OptionInput>();
			option.Name = name;
			option.Placeholder = placeholder;
			option.CharacterLimit = characterLimit;
			option.ReadOnly = readOnly;
			option.Callback = callback;
			option.Tooltip = tooltip;

			return AddRow(column, option, hidden);
		}
		public Tab AddInput(int column, string name, Func<PlayerSession, string> placeholder, Action<PlayerSession, IEnumerable<string>> callback = null, string tooltip = null, bool hidden = false)
		{
			return AddInput(column, name, placeholder, 0, callback == null, callback, tooltip, hidden);
		}
		public Tab AddEnum(int column, string name, Action<PlayerSession, bool> callback, Func<PlayerSession, string> text, string tooltip = null, bool hidden = false)
		{
			var option = Pool.Get<OptionEnum>();
			option.Name = name;
			option.Callback = callback;
			option.Text = text;
			option.Tooltip = tooltip;

			return AddRow(column, option, hidden);
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

			var option = Pool.Get<OptionRadio>();
			option.Name = name;
			option.Id = id;
			option.Index = index;
			option.Callback = callback;
			option.Radio = radio;
			option.Tooltip = tooltip;

			radio.Options.Add(option);

			return AddRow(column, option, hidden);
		}
		public Tab AddDropdown(int column, string name, Func<PlayerSession, int> index, Action<PlayerSession, int> callback, string[] options, string[] optionsIcons = null, float optionsIconScale = 0f, string tooltip = null, bool hidden = false)
		{
			var option = Pool.Get<OptionDropdown>();
			option.Name = name;
			option.Index = index;
			option.Callback = callback;
			option.Options = options;
			option.OptionsIcons = optionsIcons;
			option.OptionsIconScale = optionsIconScale;
			option.Tooltip = tooltip;

			return AddRow(column, option, hidden);
		}
		public Tab AddRange(int column, string name, float min, float max, Func<PlayerSession, float> value, Action<PlayerSession, float> callback, Func<PlayerSession, string> text = null, string tooltip = null, bool hidden = false)
		{
			var option = Pool.Get<OptionRange>();
			option.Name = name;
			option.Min = min;
			option.Max = max;
			option.Value = value;
			option.Callback = callback;
			option.Text = text;
			option.Tooltip = tooltip;

			return AddRow(column, option, hidden);
		}
		public Tab AddButtonArray(int column, float spacing, params OptionButton[] buttons)
		{
			var option = Pool.Get<OptionButtonArray>();
			option.Name = string.Empty;
			option.Spacing = spacing;
			option.Buttons = buttons;

			return AddRow(column, option);
		}
		public Tab AddButtonArray(int column, params OptionButton[] buttons)
		{
			var option = Pool.Get<OptionButtonArray>();
			option.Name = string.Empty;
			option.Spacing = 0.01f;
			option.Buttons = buttons;

			return AddRow(column, option);
		}
		public Tab AddInputButton(int column, string name, float buttonPriority, OptionInput input, OptionButton button, string tooltip = null, bool hidden = false)
		{
			var option = Pool.Get<OptionInputButton>();
			option.Name = name;
			option.ButtonPriority = buttonPriority;
			option.Input = input;
			option.Button = button;
			option.Tooltip = tooltip;

			return AddRow(column, option, hidden);
		}
		public Tab AddColor(int column, string name, Func<string> color, Action<PlayerSession, string, string, float> callback, string tooltip = null, bool hidden = false)
		{
			var option = Pool.Get<OptionColor>();
			option.Name = name;
			option.Color = color;
			option.Callback = callback;
			option.Tooltip = tooltip;

			return AddRow(column, option, hidden);
		}
		public Tab AddWidget(int column, int height, Action<PlayerSession, CUI, CuiElementContainer, string> callback)
		{
			var space = Pool.Get<OptionSpace>();

			for (int i = 0; i < height; i++)
			{
				AddRow(column, space);
			}

			var option = Pool.Get<OptionWidget>();
			option.Name = string.Empty;
			option.Height = height;
			option.Callback = callback;

			return AddRow(column, option);
		}
		public Tab AddChart(int column, IEnumerable<Components.Graphics.Chart.Layer> layers,
			IEnumerable<string> verticalLabels, IEnumerable<string> horizontalLabels,
			Components.Graphics.Chart.ChartSettings settings)
		{
			var space = Pool.Get<OptionSpace>();

			for (int i = 0; i < 8; i++)
			{
				AddRow(column, space);
			}

			var option = Pool.Get<OptionChart>();
			option.Setup(layers, verticalLabels, horizontalLabels, settings);

			return AddRow(column, option);
		}

		public void CreateDialog(string title, Action<PlayerSession> onConfirm, Action<PlayerSession> onDecline)
		{
			Dialog = new TabDialog(title, onConfirm, onDecline);
		}
		public void ResetHiddens()
		{
			foreach (var row in Columns.SelectMany(column => column.Value))
			{
				row.CurrentlyHidden = row.Hidden;
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

		public class OptionPool : List<Option>
		{
			public void ClearToPool()
			{
				ReturnToPool();
				Clear();
			}
			public void ReturnToPool()
			{
				for (int i = 0; i < Count; i++)
				{
					var option = this[i];
					Pool.Free(ref option);
				}
			}
		}

		public class Option
		{
			public string Name;
			public string Tooltip;
			public bool Hidden;
			public bool CurrentlyHidden;

			public Option() { }
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

			public OptionName() { }
			public OptionName(string name, TextAnchor align, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden) { Align = align; }
		}
		public class OptionText : Option
		{
			public int Size;
			public string Color;
			public TextAnchor Align;
			public CUI.Handler.FontTypes Font;
			public bool IsInput;

			public OptionText() { }
			public OptionText(string name, int size, string color, TextAnchor align, CUI.Handler.FontTypes font, bool isInput, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden) { Align = align; Size = size; Color = color; Font = font; IsInput = isInput; }
		}
		public class OptionInput : Option
		{
			public Func<PlayerSession, string> Placeholder;
			public int CharacterLimit;
			public bool ReadOnly;
			public Action<PlayerSession, IEnumerable<string>> Callback;

			public OptionInput() { }
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

			public OptionButton() { }
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

			public OptionToggle() { }
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

			public OptionEnum() { }
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

			public OptionRange() { }
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
			public Action<bool, PlayerSession> Callback;
			public Radio Radio;

			public OptionRadio() { }
			public OptionRadio(string name, string id, int index, Action<bool, PlayerSession> callback, Radio radio, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Id = id;
				Callback = (value, ap) => { try { callback?.Invoke(value, ap); } catch (Exception ex) { Logger.Error($"Failed OptionRadio.Callback callback ({name}): {ex.Message}"); } };
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

			public OptionDropdown() { }
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

			public OptionInputButton() { }
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

			public OptionButtonArray() { }
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

			public OptionColor() { }
			public OptionColor(string name, Func<string> color, Action<PlayerSession, string, string, float> callback, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Color = color;
				Callback = callback;
			}
		}
		public class OptionWidget : Option
		{
			public int Height = 1;
			public string WidgetPanel;
			public Action<PlayerSession, CUI, CuiElementContainer, string> Callback;

			public OptionWidget() { }
			public OptionWidget(string name, int height, Action<PlayerSession, CUI, CuiElementContainer, string> callback, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
				Height = height;
				Callback = callback;
			}
		}
		public class OptionSpace : Option
		{
			public OptionSpace() { }
			public OptionSpace(string name, string tooltip = null, bool hidden = false) : base(name, tooltip, hidden)
			{
			}
		}
		public class OptionChart : Option
		{
			public static ChartCacheDatabase Cache = new();

			public class ChartCacheDatabase : Dictionary<string, ChartCache>
			{
				public ChartCache GetOrProcessCache(string identifier, Components.Graphics.Chart chart, Action<ChartCache> onProcessed)
				{
					if (string.IsNullOrEmpty(identifier))
					{
						return default;
					}

					if (TryGetValue(identifier, out var chartCache) && chartCache.Status != ChartCache.StatusTypes.Failure)
					{
						onProcessed?.Invoke(chartCache);
						return chartCache;
					}

					chartCache.Dispose();
					chartCache = default;
					chartCache.Pool = new();
					chartCache.Status = ChartCache.StatusTypes.Processing;

					chart.StartProcess((data, exception) =>
					{
						if (exception != null)
						{
							chartCache.Status = ChartCache.StatusTypes.Failure;
							this[identifier] = chartCache;
							onProcessed?.Invoke(chartCache);
							return;
						}

						chartCache.Status = ChartCache.StatusTypes.Finalized;
						chartCache.Crc = FileStorage.server.GetCRC(data, FileStorage.Type.png);
						chartCache.Data = data;
						this[identifier] = chartCache;
						onProcessed?.Invoke(chartCache);
					});

					return this[identifier] = chartCache;
				}
			}

			public struct ChartCache
			{
				public enum StatusTypes
				{
					Finalized,
					Processing,
					Failure
				}

				public StatusTypes Status;
				public uint Crc;
				public byte[] Data;
				public List<ulong> Pool;

				public bool HasPlayerReceivedData(ulong player)
				{
					var has = Pool.Contains(player);

					if (!has)
					{
						Pool.Add(player);
					}

					return has;
				}

				public void Dispose()
				{
					if (Data != null)
					{
						Array.Clear(Data, 0, Data.Length);
					}

					Data = null;
					Crc = default;
					Status = default;
					Pool?.Clear();
					Pool = null;
				}
			}

			public const int Height = 8;
			public Components.Graphics.Chart.ChartSettings Settings;
			public Components.Graphics.Chart Chart;

			internal static int _lastValue;
			internal string _identifier { get; private set; }

			public string GetIdentifier()
			{
				if (string.IsNullOrEmpty(_identifier))
				{
					_identifier = GenerateIdentifier();
				}

				return _identifier;
			}

			public static string GenerateIdentifier()
			{
				return $"chart_{_lastValue++}";
			}

			public OptionChart() { }

			public void Setup(IEnumerable<Components.Graphics.Chart.Layer> layers,
				IEnumerable<string> verticalLabels, IEnumerable<string> horizontalLabels,
				Components.Graphics.Chart.ChartSettings settings)
			{
				Components.Graphics.Chart.ChartRect rect = default;
				const float xOffset = 75;
				const int width = 10750;
				rect.Width = width - (xOffset * 2f);
				rect.Height = 450;
				rect.X = xOffset;
				rect.Y = 100;

				Chart = Components.Graphics.Chart.Create(width, 600, settings, rect, layers, verticalLabels.ToArray(),
					horizontalLabels.ToArray(), System.Drawing.Brushes.White, System.Drawing.Color.Transparent);

				GetIdentifier();
			}
		}
	}
}
