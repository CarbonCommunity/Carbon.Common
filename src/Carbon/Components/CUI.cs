using Oxide.Game.Rust.Cui;
using UnityEngine.UI;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public readonly struct CUI : IDisposable
{
	public Handler Manager { get; }
	public ImageDatabaseModule ImageDatabase { get; }
	public Handler.Cache CacheInstance => Manager.CacheInstance;

	public enum ClientPanels
	{
		Overall,
		Overlay,
		OverlayNonScaled,
		Hud,
		HudMenu,
		Under,
		UnderNonScaled,

		// C4C-Only
		LoadingBG,
		LoadingFG
	}
	public string GetClientPanel(ClientPanels panel)
	{
		return panel switch
		{
			ClientPanels.Overall => "Overall",
            ClientPanels.Overlay => "Overlay",
			ClientPanels.Hud => "Hud",
			ClientPanels.HudMenu => "Hud.Menu",
			ClientPanels.Under => "Under",
			ClientPanels.UnderNonScaled => "UnderNonScaled",
			ClientPanels.LoadingBG => "Loading.BG",
			ClientPanels.LoadingFG => "Loading.FG",
			_ => "OverlayNonScaled",
		};
	}

	public CUI(Handler manager)
	{
		Manager = manager;
		ImageDatabase = BaseModule.GetModule<ImageDatabaseModule>();
	}

	#region Update

	public Handler.UpdatePool UpdatePool()
	{
		return new Handler.UpdatePool();
	}

    #endregion

    #region Methods
    public CuiElementContainer CreateContainer(string panel, string color = "0 0 0 0", float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, ClientPanels parent = ClientPanels.OverlayNonScaled, string destroyUi = null)
    {
        return CreateContainerParent(panel, color, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, GetClientPanel(parent), destroyUi);
    }
    public CuiElementContainer CreateContainerParent(string panel, string color = "0 0 0 0", float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string parentName = "OverlayNonScaled", string destroyUi = null)
	{
		var container = Manager.TakeFromPoolContainer();
		container.Name = panel;

		var element = Manager.TakeFromPool(panel, parentName);
		element.FadeOut = fadeOut;
		element.DestroyUi = destroyUi;

		if (!string.IsNullOrEmpty(color) && color != "0 0 0 0")
		{
			var image = Manager.TakeFromPoolImage();
			image.Color = color;
			image.FadeIn = fadeIn;
			element.Components.Add(image);
		}

		var rect = Manager.TakeFromPoolRect();
		rect.AnchorMin = $"{xMin} {yMin}";
		rect.AnchorMax = $"{xMax} {yMax}";
		rect.OffsetMin = $"{OxMin} {OyMin}";
		rect.OffsetMax = $"{OxMax} {OyMax}";
		element.Components.Add(rect);

		if (needsCursor) element.Components.Add(Manager.TakeFromPoolNeedsCursor());
		if (needsKeyboard) element.Components.Add(Manager.TakeFromPoolNeedsKeyboard());

		container.Add(element);
		container.Add(Manager.TakeFromPool(Manager.AppendId(), parentName));
		return container;
	}
	public Pair<string, CuiElement> CreatePanel(CuiElementContainer container, string parent, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, bool blur = false, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Panel(container, parent, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, blur, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateText(CuiElementContainer container, string parent, string color, string text, int size, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, VerticalWrapMode verticalOverflow = VerticalWrapMode.Overflow, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Text(container, parent, color, text, size, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, align, font, verticalOverflow, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement, CuiElement> CreateButton(CuiElementContainer container, string parent, string color, string textColor, string text, int size, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Button(container, parent, color, textColor, text, size, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, false, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement, CuiElement> CreateProtectedButton(CuiElementContainer container, string parent, string color, string textColor, string text, int size, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Button(container, parent, color, textColor, text, size, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, true, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateInputField(CuiElementContainer container, string parent, string color, string text, int size, int characterLimit, bool readOnly, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = InputField.LineType.SingleLine, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.InputField(container, parent, color, text, size, characterLimit, readOnly, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, false, autoFocus, hudMenuInput, lineType, fadeIn, fadeOut, needsCursor, needsKeyboard, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateProtectedInputField(CuiElementContainer container, string parent, string color, string text, int size, int characterLimit, bool readOnly, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = InputField.LineType.SingleLine, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.InputField(container, parent, color, text, size, characterLimit, readOnly, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, true, autoFocus, hudMenuInput, lineType, fadeIn, fadeOut, needsCursor, needsKeyboard, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateImage(CuiElementContainer container, string parent, uint png, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Image(container, parent, png.ToString(), null, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateImage(CuiElementContainer container, string parent, string url, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (!HasImage(url))
		{
			return Manager.Panel(container, parent, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax,
				false, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance,
				outlineUseGraphicAlpha, id, destroyUi, update);
		}

		return Manager.Image(container, parent, GetImage(url), null, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateImage(CuiElementContainer container, string parent, string url, float scale, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (!HasImage(url))
		{
			return Manager.Panel(container, parent, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax,
				false, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance,
				outlineUseGraphicAlpha, id, destroyUi, update);
		}

		return Manager.Image(container, parent, GetImage(url, scale), null, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateSimpleImage(CuiElementContainer container, string parent, string png, string sprite, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.SimpleImage(container, parent, png, sprite, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateSprite(CuiElementContainer container, string parent, string sprite, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Sprite(container, parent, sprite, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateItemImage(CuiElementContainer container, string parent, int itemID, ulong skinID, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.ItemImage(container, parent, itemID, skinID, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateQRCodeImage(CuiElementContainer container, string parent, string text, string brandUrl, string brandColor, string brandBgColor, int pixels, bool transparent, bool quietZones, string color, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		var qr = CreateImage(container, parent, ImageDatabase.GetQRCode(text, pixels, transparent, quietZones, true), color, null, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);

		if (!string.IsNullOrEmpty(brandUrl))
		{
			var panel = CreatePanel(container, qr, brandBgColor,
				xMin: 0.4f, xMax: 0.6f, yMin: 0.4f, yMax: 0.6f);

			CreateImage(container, panel, url: brandUrl, color: brandColor,
				material: null, xMin: 0.15f, 0.85f, yMin: 0.15f, yMax: 0.85f);
		}

		return qr;
	}
	public Pair<string, CuiElement> CreateClientImage(CuiElementContainer container, string parent, string url, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Image(container, parent, null, url, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateCountdown(CuiElementContainer container, string parent, int startTime, int endTime, int step, string command, float fadeIn = 0f, float fadeOut = 0f, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Countdown(container, parent, startTime, endTime, step, command, fadeIn, fadeOut, id, destroyUi, update);
	}
	public Pair<string, CuiElement> CreateScrollView(CuiElementContainer container, string parent,bool vertical, bool horizontal, ScrollRect.MovementType movementType, float elasticity, bool inertia, float decelerationRate, float scrollSensitivity, string maskSoftness, out CuiRectTransform contentTransformComponent, out CuiScrollbar horizontalScrollBar, out CuiScrollbar verticalScrollBar, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.ScrollView(container, parent, vertical, horizontal, movementType, elasticity, inertia, decelerationRate, scrollSensitivity, maskSoftness, out contentTransformComponent, out horizontalScrollBar, out verticalScrollBar, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, id, destroyUi, update);
	}

	public static string HexToRustColor(string hexColor, float? alpha = null, bool includeAlpha = true)
	{
		if (!ColorUtility.TryParseHtmlString(hexColor, out var color))
		{
			return $"1 1 1{(includeAlpha ? $" {alpha.GetValueOrDefault(1)}" : "")}";
		}

		return $"{color.r} {color.g} {color.b}{(includeAlpha ? $" {alpha ?? color.a}" : "")}";
	}
	public static string RustToHexColor(string rustColor, float? alpha = null, bool includeAlpha = true)
	{
		var colors = rustColor.Split(' ');
		var color = new Color(colors[0].ToFloat(), colors[1].ToFloat(), colors[2].ToFloat(), includeAlpha ? alpha ?? (colors.Length > 2 ? colors[3].ToFloat() : 1f) : 1);
		var result = includeAlpha ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color);
		Array.Clear(colors, 0, colors.Length);
		return $"#{result}";
	}

	#endregion

	#region ImageDatabase

	public string GetImage(string url, float scale = 0)
	{
		return ImageDatabase.GetImageString(url, scale, true);
	}

	public bool HasImage(string url, float scale = 0)
	{
		return ImageDatabase.HasImage(url, scale);
	}

	public void QueueImages(float scale, IEnumerable<string> urls)
	{
		ImageDatabase.QueueBatch(scale, false, urls);
	}
	public void QueueImages(IEnumerable<string> urls)
	{
		QueueImages(0, urls);
	}

	public void ClearImages(float scale, IEnumerable<string> urls)
	{
		foreach (var url in urls)
		{
			ImageDatabase.DeleteImage(url, scale);
		}
	}
	public void ClearImages(IEnumerable<string> urls)
	{
		ClearImages(0, urls);
	}

	#endregion

	#region Send

	public void Send(CuiElementContainer container, BasePlayer player)
	{
		Manager.Send(container, player);
	}
	public void Destroy(CuiElementContainer container, BasePlayer player)
	{
		Manager.Destroy(container, player);
	}
	public void Destroy(string name, BasePlayer player)
	{
		Manager.Destroy(name, player);
	}

	#endregion

	public struct Pair<T1, T2>
	{
		public T1 Id;
		public T2 Element;

		public Pair(T1 id, T2 element)
		{
			Id = id;
			Element = element;
		}

		public static implicit operator string(Pair<T1, T2> value)
		{
			return value.Id.ToString();
		}
	}
	public struct Pair<T1, T2, T3>
	{
		public T1 Id;
		public T2 Element1;
		public T3 Element2;

		public Pair(T1 id, T2 element1, T3 element2)
		{
			Id = id;
			Element1 = element1;
			Element2 = element2;
		}

		public static implicit operator string(Pair<T1, T2, T3> value)
		{
			return value.Id.ToString();
		}
	}

	public void Dispose()
	{
		Manager.SendToPool();
	}

	public class Handler
	{
		internal string Identifier { get; } = RandomEx.GetRandomString(4);

		public Cache CacheInstance = new();
		public int Pooled => _containerPool.Count + _elements.Count + _images.Count + _rawImages.Count + _texts.Count + _buttons.Count + _inputFields.Count + _rects.Count + _needsCursors.Count + _needsKeyboards.Count;
		public int Used => _queue.Count;

		#region Properties

		internal int _currentId { get; set; }

		internal List<object> _queue = new();
		internal List<CuiElementContainer> _containerPool = new();

		internal List<CuiElement> _elements = new();
		internal List<ICuiComponent> _images = new();
		internal List<ICuiComponent> _rawImages = new();
		internal List<ICuiComponent> _texts = new();
		internal List<ICuiComponent> _buttons = new();
		internal List<ICuiComponent> _inputFields = new();
		internal List<ICuiComponent> _rects = new();
		internal List<ICuiComponent> _needsCursors = new();
		internal List<ICuiComponent> _needsKeyboards = new();
		internal List<ICuiComponent> _countdowns = new();
		internal List<ICuiComponent> _outlines = new();
		internal List<ICuiComponent> _scrollViews = new();
		internal List<ICuiComponent> _scrollbars = new();

		#endregion

		#region Default Instances

		internal CuiRectPosition _defaultPosition = new(0f, 1f, 0f, 1f);
		internal CuiImageComponent _defaultImage = new();
		internal CuiRawImageComponent _defaultRawImage = new();
		internal CuiRectTransformComponent DefaultRectTransformComponent = new()
		{
			OffsetMax = "0 0"
		};
		internal CuiTextComponent _defaultText = new();
		internal CuiButtonComponent _defaultButton = new();
		internal CuiInputFieldComponent _defaultInputField = new();
		internal CuiCountdownComponent _defaultCountdown = new();
		internal CuiOutlineComponent _defaultOutline = new();
		internal CuiScrollViewComponent _defaultScrollView = new();
		internal CuiScrollbar _defaultScrollBar = new();

		#endregion

		#region Pooling

		public string AppendId()
		{
			_currentId++;
			return $"{Identifier}_{_currentId}";
		}
		public void SendToPool<T>(T element) where T : ICuiComponent
		{
			if (element == null) return;

			switch (element)
			{
				case CuiImageComponent: _images.Add(element); break;
				case CuiRawImageComponent: _rawImages.Add(element); break;
				case CuiTextComponent: _texts.Add(element); break;
				case CuiButtonComponent: _buttons.Add(element); break;
				case CuiRectTransformComponent: _rects.Add(element); break;
				case CuiInputFieldComponent: _inputFields.Add(element); break;
				case CuiNeedsCursorComponent: _needsCursors.Add(element); break;
				case CuiNeedsKeyboardComponent: _needsKeyboards.Add(element); break;
				case CuiCountdownComponent: _countdowns.Add(element); break;
				case CuiOutlineComponent: _outlines.Add(element); break;
				case CuiScrollViewComponent scrollView:
				{
					SendToPool(scrollView.HorizontalScrollbar);
					SendToPool(scrollView.VerticalScrollbar);

					_scrollViews.Add(element);
					break;
				}
				case CuiScrollbar: _scrollbars.Add(element); break;
			}
		}
		public void SendToPool()
		{
			foreach (var entry in _queue)
			{
				if (entry is CuiElement element)
				{
					_elements.Add(element);
				}
				else if (entry is CuiElementContainer elementContainer)
				{
					_containerPool.Add(elementContainer);
				}
				else
				{
					SendToPool(entry as ICuiComponent);
				}
			}

			_queue.Clear();
			_currentId = 0;
		}

		#endregion

		#region Pooled Elements

		public CuiElement TakeFromPool(string name = null, string parent = "Hud", float fadeOut = 0f, string destroyUi = null, bool update = false)
		{
			var element = (CuiElement)null;

			if (_elements.Count == 0)
			{
				element = new CuiElement();
			}
			else
			{
				element = _elements[0];
				_elements.RemoveAt(0);
			}

			element.Name = name;
			element.Update = update;
			element.Parent = parent;
			element.Components.Clear();
			element.DestroyUi = destroyUi;
			element.FadeOut = fadeOut;

			_queue.Add(element);
			return element;
		}
		public CuiElementContainer TakeFromPoolContainer()
		{
			var element = (CuiElementContainer)null;

			if (_containerPool.Count == 0)
			{
				element = new CuiElementContainer();
			}
			else
			{
				element = _containerPool[0];
				element.Clear();

				_containerPool.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiRectPosition TakeFromPoolDimensions()
		{
			var element = new CuiRectPosition(_defaultPosition.xMin, _defaultPosition.yMin, _defaultPosition.xMax, _defaultPosition.yMax);

			return element;
		}
		public CuiImageComponent TakeFromPoolImage()
		{
			var element = (CuiImageComponent)null;

			if (_images.Count == 0)
			{
				element = new CuiImageComponent();
			}
			else
			{
				element = _images[0] as CuiImageComponent;
				element.Sprite = _defaultImage.Sprite;
				element.Material = _defaultImage.Material;
				element.Color = _defaultImage.Color;
				element.SkinId = _defaultImage.SkinId;
				element.ImageType = _defaultImage.ImageType;
				element.Png = _defaultImage.Png;
				element.FadeIn = _defaultImage.FadeIn;
				element.ItemId = _defaultImage.ItemId;
				element.SkinId = _defaultImage.SkinId;
				_images.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiRawImageComponent TakeFromPoolRawImage()
		{
			var element = (CuiRawImageComponent)null;

			if (_rawImages.Count == 0)
			{
				element = new CuiRawImageComponent();
			}
			else
			{
				element = _rawImages[0] as CuiRawImageComponent;
				element.Sprite = _defaultRawImage.Sprite;
				element.Color = _defaultRawImage.Color;
				element.Material = _defaultRawImage.Material;
				element.Url = _defaultRawImage.Url;
				element.Png = _defaultRawImage.Png;
				element.FadeIn = _defaultRawImage.FadeIn;
				_rawImages.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiRectTransformComponent TakeFromPoolRect()
		{
			var element = (CuiRectTransformComponent)null;

			if (_rects.Count == 0)
			{
				element = new CuiRectTransformComponent();
			}
			else
			{
				element = _rects[0] as CuiRectTransformComponent;
				element.AnchorMin = DefaultRectTransformComponent.AnchorMin;
				element.AnchorMax = DefaultRectTransformComponent.AnchorMax;
				element.OffsetMin = DefaultRectTransformComponent.OffsetMin;
				element.OffsetMax = DefaultRectTransformComponent.OffsetMax;
				_rects.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiTextComponent TakeFromPoolText()
		{
			var element = (CuiTextComponent)null;

			if (_texts.Count == 0)
			{
				element = new CuiTextComponent();
			}
			else
			{
				element = _texts[0] as CuiTextComponent;
				element.Text = _defaultText.Text;
				element.FontSize = _defaultText.FontSize;
				element.Font = _defaultText.Font;
				element.Align = _defaultText.Align;
				element.Color = _defaultText.Color;
				element.FadeIn = _defaultText.FadeIn;
				element.VerticalOverflow = _defaultText.VerticalOverflow;
				_texts.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiButtonComponent TakeFromPoolButton()
		{
			var element = (CuiButtonComponent)null;

			if (_buttons.Count == 0)
			{
				element = new CuiButtonComponent();
			}
			else
			{
				element = _buttons[0] as CuiButtonComponent;
				element.Command = _defaultButton.Command;
				element.Close = _defaultButton.Close;
				element.Sprite = _defaultButton.Sprite;
				element.Material = _defaultButton.Material;
				element.Color = _defaultButton.Color;
				element.ImageType = _defaultButton.ImageType;
				element.FadeIn = _defaultButton.FadeIn;
				_buttons.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiInputFieldComponent TakeFromPoolInputField()
		{
			var element = (CuiInputFieldComponent)null;

			if (_inputFields.Count == 0)
			{
				element = new CuiInputFieldComponent();
			}
			else
			{
				element = _inputFields[0] as CuiInputFieldComponent;
				element.Text = _defaultInputField.Text;
				element.FontSize = _defaultInputField.FontSize;
				element.Font = _defaultInputField.Font;
				element.Align = _defaultInputField.Align;
				element.Color = _defaultInputField.Color;
				element.CharsLimit = _defaultInputField.CharsLimit;
				element.Command = _defaultInputField.Command;
				element.IsPassword = _defaultInputField.IsPassword;
				element.ReadOnly = _defaultInputField.ReadOnly;
				element.NeedsCursor = _defaultInputField.NeedsCursor;
				element.NeedsKeyboard = _defaultInputField.NeedsKeyboard;
				element.LineType = _defaultInputField.LineType;
				element.Autofocus = _defaultInputField.Autofocus;
				element.HudMenuInput = _defaultInputField.HudMenuInput;
				_inputFields.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiNeedsCursorComponent TakeFromPoolNeedsCursor()
		{
			var element = (CuiNeedsCursorComponent)null;

			if (_needsCursors.Count == 0)
			{
				element = new CuiNeedsCursorComponent();
			}
			else
			{
				element = _needsCursors[0] as CuiNeedsCursorComponent;
				_needsCursors.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiNeedsKeyboardComponent TakeFromPoolNeedsKeyboard()
		{
			var element = (CuiNeedsKeyboardComponent)null;

			if (_needsKeyboards.Count == 0)
			{
				element = new CuiNeedsKeyboardComponent();
			}
			else
			{
				element = _needsKeyboards[0] as CuiNeedsKeyboardComponent;
				_needsKeyboards.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiCountdownComponent TakeFromPoolCountdown()
		{
			var element = (CuiCountdownComponent)null;

			if (_countdowns.Count == 0)
			{
				element = new CuiCountdownComponent();
			}
			else
			{
				element = _countdowns[0] as CuiCountdownComponent;
				element.EndTime = _defaultCountdown.EndTime;
				element.StartTime = _defaultCountdown.StartTime;
				element.Step = _defaultCountdown.Step;
				element.Command = _defaultCountdown.Command;
				element.FadeIn = _defaultCountdown.FadeIn;
				_countdowns.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiOutlineComponent TakeFromPoolOutline()
		{
			var element = (CuiOutlineComponent)null;

			if (_outlines.Count == 0)
			{
				element = new CuiOutlineComponent();
			}
			else
			{
				element = _outlines[0] as CuiOutlineComponent;
				element.Color = _defaultOutline.Color;
				element.Distance = _defaultOutline.Distance;
				element.UseGraphicAlpha = _defaultOutline.UseGraphicAlpha;
				_outlines.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiScrollViewComponent TakeFromPoolScrollView()
		{
			var element = (CuiScrollViewComponent)null;

			if (_scrollViews.Count == 0)
			{
				element = new CuiScrollViewComponent();
			}
			else
			{
				element = _scrollViews[0] as CuiScrollViewComponent;
				element.Vertical = _defaultScrollView.Vertical;
				element.Horizontal = _defaultScrollView.Horizontal;
				element.MovementType = _defaultScrollView.MovementType;
				element.Elasticity = _defaultScrollView.Elasticity;
				element.Inertia = _defaultScrollView.Inertia;
				element.DecelerationRate = _defaultScrollView.DecelerationRate;
				element.ScrollSensitivity = _defaultScrollView.ScrollSensitivity;
				element.MaskSoftness = _defaultScrollView.MaskSoftness;
				element.ContentTransform = new();
				element.ContentTransform.AnchorMin = "0 0";
				element.ContentTransform.AnchorMax = "1 1";
				element.ContentTransform.OffsetMin = "0 0";
				element.ContentTransform.OffsetMax = "0 0";
				element.HorizontalScrollbar = TakeFromPoolScrollbar();
				element.VerticalScrollbar = TakeFromPoolScrollbar();

				_scrollViews.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}
		public CuiScrollbar TakeFromPoolScrollbar()
		{
			var element = (CuiScrollbar)null;

			if (_scrollbars.Count == 0)
			{
				element = new CuiScrollbar();
			}
			else
			{
				element = _scrollbars[0] as CuiScrollbar;
				element.Invert = _defaultScrollBar.Invert;
				element.AutoHide = _defaultScrollBar.AutoHide;
				element.HandleSprite = _defaultScrollBar.HandleSprite;
				element.Size = _defaultScrollBar.Size;
				element.HandleColor = _defaultScrollBar.HandleColor;
				element.HighlightColor = _defaultScrollBar.HighlightColor;
				element.PressedColor = _defaultScrollBar.PressedColor;
				element.TrackSprite = _defaultScrollBar.TrackSprite;
				element.TrackColor = _defaultScrollBar.TrackColor;

				_scrollbars.RemoveAt(0);
			}

			_queue.Add(element);
			return element;
		}

		#endregion

		#region Classes

		public struct CuiRectPosition
		{
			public float xMin { get; set; }
			public float yMin { get; set; }
			public float xMax { get; set; }
			public float yMax { get; set; }

			public CuiRectPosition(float xMin, float xMax, float yMin, float yMax)
			{
				this.xMin = xMin;
				this.yMin = yMin;
				this.xMax = xMax;
				this.yMax = yMax;
			}

			public string GetMin() => $"{xMin} {yMin}";
			public string GetMax() => $"{xMax} {yMax}";
		}

		public enum FontTypes
		{
			Arial,
			RobotoCondensedBold, RobotoCondensedRegular,
			PermanentMarker, DroidSansMono
		}

		public string GetFont(FontTypes type)
		{
			return type switch
			{
				FontTypes.RobotoCondensedBold => "robotocondensed-bold.ttf",
				FontTypes.RobotoCondensedRegular => "robotocondensed-regular.ttf",
				FontTypes.PermanentMarker => "permanentmarker.ttf",
				FontTypes.DroidSansMono => "droidsansmono.ttf",
				_ => "robotocondensed-regular.ttf"
			};

		}

		#endregion

		#region Network

		public void Send(CuiElementContainer container, BasePlayer player)
		{
			container.Send(player);
		}
		public void SendUpdate(Pair<string, CuiElement> pair, BasePlayer player)
		{
			pair.SendUpdate(player);
		}
		public void Destroy(CuiElementContainer container, BasePlayer player)
		{
			container.Destroy(player);
		}
		public void Destroy(string name, BasePlayer player)
		{
			CUIStatics.Destroy(name, player);
		}

		#endregion

		public class UpdatePool : CuiElementContainer, IDisposable
		{
			internal bool _hasDisposed;

			public void Add(Pair<string, CuiElement> pair)
			{
				if (pair.Element != null)
				{
					if (!pair.Element.Update)
					{
						Logger.Warn($"You're trying to update element '{pair.Element.Name}' (of parent '{pair.Element.Parent}') which doesn't allow updates. Ignoring.");
						return;
					}

					Add(pair.Element);
				}
			}
			public void Add(Pair<string, CuiElement, CuiElement> pair)
			{
				if (pair.Element1 != null)
				{
					if (!pair.Element1.Update)
					{
						Logger.Warn($"You're trying to update element '{pair.Element1.Name}' (of parent '{pair.Element1.Parent}') which doesn't allow updates. Ignoring.");
						return;
					}

					Add(pair.Element1);
				}

				if (pair.Element2 != null)
				{
					if (!pair.Element2.Update)
					{
						// Logger.Warn($"You're trying to update element '{pair.Element2.Name}' (of parent '{pair.Element2.Parent}') which doesn't allow updates. Ignoring.");
						return;
					}

					Add(pair.Element2);
				}
			}

			public void Send(BasePlayer player)
			{
				CUIStatics.Send(this, player);

				Dispose();
			}

			public void Dispose()
			{
				if (_hasDisposed) return;

				Clear();

				_hasDisposed = true;
			}
		}

		public class Cache
		{
			internal Dictionary<string, byte[]> _cuiData = new();

			public bool TryStore(string id, CuiElementContainer container)
			{
				if (TryTake(id, out _))
				{
					return false;
				}

				_cuiData.Add(id, container.GetData());
				return true;
			}
			public bool TryTake(string id, out byte[] data)
			{
				data = default;

				if (_cuiData.TryGetValue(id, out var content))
				{
					data = content;
					return true;
				}

				return false;
			}
			public bool TrySend(string id, BasePlayer player)
			{
				if (!TryTake(id, out var data))
				{
					return false;
				}

				CUIStatics.SendData(data, player);
				return true;
			}
		}
	}
}
