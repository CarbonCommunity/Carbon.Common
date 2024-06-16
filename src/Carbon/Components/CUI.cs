using Facepunch;
using Oxide.Game.Rust.Cui;
using UnityEngine.UI;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public readonly partial struct CUI : IDisposable
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
    
    public CuiElementContainer CreateContainer(string panel, Transform transform, Render render, Needs needs, ClientPanels parent = ClientPanels.OverlayNonScaled, string destroyUi = null)
    {
        return CreateContainerParent(panel, transform, render, needs, GetClientPanel(parent), destroyUi);
    }
    public CuiElementContainer CreateContainerParent(string panel, Transform transform, Render render, Needs needs, string parentName = "OverlayNonScaled", string destroyUi = null)
	{
		var container = Manager.TakeFromPoolContainer();
		container.Name = panel;

		var element = Manager.TakeFromPool(panel, parentName);
		element.FadeOut = render.FadeOut;
		element.DestroyUi = destroyUi;

		if (!string.IsNullOrEmpty(render.Color) && render.Color != "0 0 0 0")
		{
			var image = Manager.TakeFromPoolImage();
			image.Color = render.Color;
			image.FadeIn = render.FadeIn;
			element.Components.Add(image);
		}

		var rect = Manager.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(Handler.CachedNeedsKeyboard);

		container.Add(element);
		container.Add(Manager.TakeFromPool(Manager.AppendId(), parentName));
		return container;
	}
	public Pair<CuiImageComponent> CreatePanel(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Panel(container, parent, transform, render, needs, outline, id, destroyUi, update);
	}
	public Pair<CuiTextComponent> CreateText(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string text, int size, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, VerticalWrapMode verticalOverflow = VerticalWrapMode.Overflow, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Text(container, parent, transform, render, needs, outline, text, size, align, font, verticalOverflow, id, destroyUi, update);
	}
	public Pair<CuiButtonComponent, CuiElement, CuiTextComponent, CuiRectTransformComponent> CreateButton(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string textColor, string text, int size, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Button(container, parent, transform, render, needs, outline, textColor, text, size, command, align, font, false, id, destroyUi, update);
	}
	public Pair<CuiButtonComponent, CuiElement, CuiTextComponent, CuiRectTransformComponent> CreateProtectedButton(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string textColor, string text, int size, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Button(container, parent, transform, render, needs, outline, textColor, text, size, command, align, font, true, id, destroyUi, update);
	}
	public Pair<CuiInputFieldComponent> CreateInputField(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string text, int size, int characterLimit, bool readOnly, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = InputField.LineType.SingleLine, float fadeIn = 0f, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.InputField(container, parent, transform, render, needs, outline, text, size, characterLimit, readOnly, command, align, font, false, autoFocus, hudMenuInput, lineType, id, destroyUi, update);
	}
	public Pair<CuiInputFieldComponent> CreateProtectedInputField(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string text, int size, int characterLimit, bool readOnly, string command = null, TextAnchor align = TextAnchor.MiddleCenter, Handler.FontTypes font = Handler.FontTypes.RobotoCondensedRegular, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = InputField.LineType.SingleLine, float fadeIn = 0f, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.InputField(container, parent, transform, render, needs, outline, text, size, characterLimit, readOnly, command, align, font, true, autoFocus, hudMenuInput, lineType, id, destroyUi, update);
	}
	public Pair<CuiRawImageComponent> CreateImage(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, uint png, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Image(container, parent, transform, render, needs, outline, png.ToString(), default, id, destroyUi, update);
	}
	public Pair<CuiRawImageComponent> CreateImage(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string url, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Image(container, parent, transform, render, needs, outline, GetImage(url), null, id, destroyUi, update);
	}
	public Pair<CuiRawImageComponent> CreateImage(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string url, float scale, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Image(container, parent, transform, render, needs, outline, GetImage(url, scale), null, id, destroyUi, update);
	}
	public Pair<CuiImageComponent> CreateSimpleImage(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string png, string sprite, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.SimpleImage(container, parent, transform, render, needs, outline, png, sprite, id, destroyUi, update);
	}
	public Pair<CuiRawImageComponent> CreateSprite(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string sprite, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Sprite(container, parent, transform, render, needs, outline, sprite, id, destroyUi, update);
	}
	public Pair<CuiImageComponent> CreateItemImage(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, int itemID, ulong skinID, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.ItemImage(container, parent, transform, render, needs, outline, itemID, skinID, id, destroyUi, update);
	}
	public Pair<Pair<CuiRawImageComponent>, Pair<CuiImageComponent>, Pair<CuiRawImageComponent>> CreateQRCodeImage(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string text, string brandUrl, string brandColor, string brandBgColor, int pixels, bool transparent, bool quietZones, string id = null, string destroyUi = null, bool update = false)
	{
		var qr = CreateImage(container, parent, transform, render, needs, outline, ImageDatabase.GetQRCode(text, pixels, transparent, quietZones, true), id, destroyUi, update);

		if (string.IsNullOrEmpty(brandUrl))
		{
			return new Pair<Pair<CuiRawImageComponent>, Pair<CuiImageComponent>, Pair<CuiRawImageComponent>>(id, null, qr, default, default);
		}

		var panel = CreatePanel(container, qr, Transform.CreateAnchor(0.4f, 0.6f, 0.4f, 0.6f), Render.Default.WithColor(brandBgColor), Needs.Default, Outline.Default);
		var brandLogo = CreateImage(container, panel, Transform.CreateAnchor(0.15f, 0.85f, 0.15f, 0.85f), Render.Default.WithColor(brandColor), default, default, url: brandUrl);
		return new Pair<Pair<CuiRawImageComponent>, Pair<CuiImageComponent>, Pair<CuiRawImageComponent>>(id, null, qr, panel, brandLogo);
	}
	public Pair<CuiRawImageComponent> CreateClientImage(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, string url, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Image(container, parent, transform, render, needs, outline, null, url, id, destroyUi, update);
	}
	public Pair<CuiCountdownComponent> CreateCountdown(CuiElementContainer container, string parent, Render render, int startTime, int endTime, int step, string command, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.Countdown(container, parent, render, startTime, endTime, step, command, id, destroyUi, update);
	}
	public Pair<CuiScrollViewComponent> CreateScrollView(CuiElementContainer container, string parent, Transform transform, Render render, Needs needs, Outline outline, bool vertical, bool horizontal, ScrollRect.MovementType movementType, float elasticity, bool inertia, float decelerationRate, float scrollSensitivity, string maskSoftness, out CuiRectTransform contentTransformComponent, out CuiScrollbar horizontalScrollBar, out CuiScrollbar verticalScrollBar, string id = null, string destroyUi = null, bool update = false)
	{
		return Manager.ScrollView(container, parent, transform, render, needs, outline, vertical, horizontal, movementType, elasticity, inertia, decelerationRate, scrollSensitivity, maskSoftness, out contentTransformComponent, out horizontalScrollBar, out verticalScrollBar, id, destroyUi, update);
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

	public void Dispose()
	{
		Manager.SendToPool();
	}

	public class Handler
	{
		internal string Identifier { get; } = RandomEx.GetRandomString(4);

		public Cache CacheInstance = new();
		public int Pooled => _containerPool.Count + _elements.Count + _images.Count + _rawImages.Count + _texts.Count + _buttons.Count + _inputFields.Count + _rects.Count;
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
		internal List<ICuiComponent> _countdowns = new();
		internal List<ICuiComponent> _outlines = new();
		internal List<ICuiComponent> _scrollViews = new();
		internal List<ICuiComponent> _scrollbars = new();

		#endregion

		#region Default Instances

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

		public static CuiNeedsCursorComponent CachedNeedsCursor = new();
		public static CuiNeedsKeyboardComponent CachedNeedsKeyboard = new();

		public CuiElement TakeFromPool(string name = null, string parent = "Hud", float fadeOut = 0f, string destroyUi = null, bool update = false, bool queueToPool = true)
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
		public CuiElementContainer TakeFromPoolContainer(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiImageComponent TakeFromPoolImage(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiRawImageComponent TakeFromPoolRawImage(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiRectTransformComponent TakeFromPoolRect(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiTextComponent TakeFromPoolText(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiButtonComponent TakeFromPoolButton(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiInputFieldComponent TakeFromPoolInputField(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiCountdownComponent TakeFromPoolCountdown(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiOutlineComponent TakeFromPoolOutline(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiScrollViewComponent TakeFromPoolScrollView(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}
		public CuiScrollbar TakeFromPoolScrollbar(bool queueToPool = true)
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

			if (queueToPool)
			{
				_queue.Add(element);
			}
			return element;
		}

		#endregion

		#region Classes

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

			public void Add(Pair pair)
			{
				if (pair.Element != null && pair.Element.Update)
				{
					Add(pair.Element);
				}
			}
			public void Add<T1>(Pair<T1> pair)
			{
				if (pair.Element != null && pair.Element.Update)
				{
					Add(pair.Element);
				}
			}
			public void Add<T1, T2>(Pair<T1, T2> pair)
			{
				if (pair.Element != null && pair.Element.Update)
				{
					Add(pair.Element);
				}
			}
			public void Add<T1, T2, T3>(Pair<T1, T2, T3> pair)
			{
				if (pair.Element != null && pair.Element.Update)
				{
					Add(pair.Element);
				}
			}
			public void Add<T1, T2, T3, T4>(Pair<T1, T2, T3, T4> pair)
			{
				if (pair.Element != null && pair.Element.Update)
				{
					Add(pair.Element);
				}
			}
			public void Add<T1, T2, T3, T4, T5>(Pair<T1, T2, T3, T4, T5> pair)
			{
				if (pair.Element != null && pair.Element.Update)
				{
					Add(pair.Element);
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
