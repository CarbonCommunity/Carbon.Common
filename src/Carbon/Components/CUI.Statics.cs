/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

using Network;
using Oxide.Game.Rust.Cui;
using UnityEngine.UI;

namespace Carbon.Components;

public static class CUIStatics
{
	public static readonly uint AddUiString = StringPool.Get("AddUi");

	public static string ProcessColor(string color)
	{
		return color.StartsWith("#") ? CUI.HexToRustColor(color) : color;
	}

	public static CUI.Pair<CuiImageComponent> Panel(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;

		var image = cui.TakeFromPoolImage();
		image.Color = ProcessColor(render.Color);

		if (render.HasMaterial)
		{
			image.Material = render.Material;
		}

		image.FadeIn = render.FadeIn;
		element.Components.Add(image);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (outline.Valid)
		{
			outlineComp = cui.TakeFromPoolOutline();
			outlineComp.Color = ProcessColor(outline.Color);
			outlineComp.Distance = outline.Distance;
			outlineComp.UseGraphicAlpha = outline.UseGraphicAlpha;
			element.Components.Add(outlineComp);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiImageComponent>(id, element, image, rect, outlineComp);
	}
	public static CUI.Pair<CuiTextComponent> Text(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string text, int size, TextAnchor align, CUI.Handler.FontTypes font, VerticalWrapMode verticalOverflow, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;

		var label = cui.TakeFromPoolText();
		label.Text = string.IsNullOrEmpty(text) ? string.Empty : text;
		label.FontSize = size;
		label.Align = align;
		label.Font = cui.GetFont(font);
		label.Color = ProcessColor(render.Color);
		label.FadeIn = render.FadeIn;
		label.VerticalOverflow = verticalOverflow;
		element.Components.Add(label);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (outline.Valid)
		{
			outlineComp = cui.TakeFromPoolOutline();
			outlineComp.Color = ProcessColor(outline.Color);
			outlineComp.Distance = outline.Distance;
			outlineComp.UseGraphicAlpha = outline.UseGraphicAlpha;
			element.Components.Add(outlineComp);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiTextComponent>(id, element, label, rect, outlineComp);
	}
	public static CUI.Pair<CuiButtonComponent, CuiElement, CuiTextComponent, CuiRectTransformComponent> Button(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string textColor, string text, int size, string command, TextAnchor align, CUI.Handler.FontTypes font, bool @protected, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;
		var textComp = (CuiTextComponent)default;
		var textRectComp = (CuiRectTransformComponent)default;

		var button = cui.TakeFromPoolButton();
		button.FadeIn = render.FadeIn;
		button.Color = ProcessColor(render.Color);
		button.Command = @protected ? Community.Protect(command) : command;

		if (render.HasMaterial)
		{
			button.Material = render.Material;
		}

		element.Components.Add(button);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (!update) container?.Add(element);

		var textElement = (CuiElement)null;

		if (!string.IsNullOrEmpty(text))
		{
			textElement = cui.TakeFromPool(cui.AppendId(), element.Name);

			textComp = cui.TakeFromPoolText();
			textComp.Text = text;
			textComp.FontSize = size;
			textComp.Align = align;
			textComp.Color = ProcessColor(textColor);
			textComp.Font = cui.GetFont(font);
			textElement.Components.Add(textComp);

			textRectComp = cui.TakeFromPoolRect();
			textRectComp.AnchorMin = "0.02 0";
			textRectComp.AnchorMax = "0.98 1";
			textElement.Components.Add(textRectComp);

			if (!update) container?.Add(textElement);
		}

		if (outline.Valid)
		{
			outlineComp = cui.TakeFromPoolOutline();
			outlineComp.Color = ProcessColor(outline.Color);
			outlineComp.Distance = outline.Distance;
			outlineComp.UseGraphicAlpha = outline.UseGraphicAlpha;
			element.Components.Add(outlineComp);
		}

		return new CUI.Pair<CuiButtonComponent, CuiElement, CuiTextComponent, CuiRectTransformComponent>(id, element, button, textElement, textComp, textRectComp, rect, outlineComp);
	}
	public static CUI.Pair<CuiInputFieldComponent> InputField(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string text, int size, int characterLimit, bool readOnly, string command, TextAnchor align, CUI.Handler.FontTypes font, bool @protected, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = UnityEngine.UI.InputField.LineType.SingleLine, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);

		var inputField = cui.TakeFromPoolInputField();
		inputField.Color = ProcessColor(render.Color);
		inputField.Text = string.IsNullOrEmpty(text) ? string.Empty : text;
		inputField.FontSize = size;
		inputField.Font = cui.GetFont(font);
		inputField.Align = align;
		inputField.CharsLimit = characterLimit;
		inputField.ReadOnly = readOnly;
		inputField.Command = @protected ? Community.Protect(command) : command;
		inputField.LineType = lineType;
		inputField.Autofocus = autoFocus;
		inputField.HudMenuInput = hudMenuInput;
		element.Components.Add(inputField);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard && !inputField.ReadOnly) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiInputFieldComponent>(id, element, inputField, rect);
	}
	public static CUI.Pair<CuiRawImageComponent> Image(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string png, string url, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;

		var rawImage = cui.TakeFromPoolRawImage();
		rawImage.Png = png;
		rawImage.Url = url;
		rawImage.FadeIn = render.FadeIn;
		rawImage.Color = ProcessColor(render.Color);

		if (render.HasMaterial)
		{
			rawImage.Material = render.Material;
		}

		element.Components.Add(rawImage);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (outline.Valid)
		{
			outlineComp = cui.TakeFromPoolOutline();
			outlineComp.Color = ProcessColor(outline.Color);
			outlineComp.Distance = outline.Distance;
			outlineComp.UseGraphicAlpha = outline.UseGraphicAlpha;
			element.Components.Add(outlineComp);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiRawImageComponent>(id, element, rawImage, rect, outlineComp);
	}
	public static CUI.Pair<CuiImageComponent> SimpleImage(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string png, string sprite, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;

		var simpleImage = cui.TakeFromPoolImage();
		simpleImage.Png = png;
		simpleImage.Sprite = sprite;
		simpleImage.FadeIn = render.FadeIn;
		simpleImage.Color = ProcessColor(render.Color);

		if (render.HasMaterial)
		{
			simpleImage.Material = render.Material;
		}

		element.Components.Add(simpleImage);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (outline.Valid)
		{
			outlineComp = cui.TakeFromPoolOutline();
			outlineComp.Color = ProcessColor(outline.Color);
			outlineComp.Distance = outline.Distance;
			outlineComp.UseGraphicAlpha = outline.UseGraphicAlpha;
			element.Components.Add(outlineComp);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiImageComponent>(id, element, simpleImage, rect, outlineComp);
	}
	public static CUI.Pair<CuiRawImageComponent> Sprite(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string sprite, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;

		var rawImage = cui.TakeFromPoolRawImage();
		rawImage.Sprite = sprite;
		rawImage.FadeIn = render.FadeIn;
		rawImage.Color = ProcessColor(render.Color);

		if (render.HasMaterial)
		{
			rawImage.Material = render.Material;
		}

		element.Components.Add(rawImage);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (outline.Valid)
		{
			outlineComp = cui.TakeFromPoolOutline();
			outlineComp.Color = ProcessColor(outline.Color);
			outlineComp.Distance = outline.Distance;
			outlineComp.UseGraphicAlpha = outline.UseGraphicAlpha;
			element.Components.Add(outlineComp);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiRawImageComponent>(id, element, rawImage, rect, outlineComp);
	}
	public static CUI.Pair<CuiImageComponent> ItemImage(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, int itemID, ulong skinID, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;

		var image = cui.TakeFromPoolImage();
		image.ItemId = itemID;
		image.SkinId = skinID;
		image.FadeIn = render.FadeIn;
		image.Color = ProcessColor(render.Color);

		if (render.HasMaterial)
		{
			image.Material = render.Material;
		}

		element.Components.Add(image);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (outline.Valid)
		{
			outlineComp = cui.TakeFromPoolOutline();
			outlineComp.Color = ProcessColor(outline.Color);
			outlineComp.Distance = outline.Distance;
			outlineComp.UseGraphicAlpha = outline.UseGraphicAlpha;
			element.Components.Add(outlineComp);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiImageComponent>(id, element, image, rect, outlineComp);
	}
	public static CUI.Pair<CuiCountdownComponent> Countdown(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Render render, int startTime, int endTime, int step, string command, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);

		var countdown = cui.TakeFromPoolCountdown();
		countdown.StartTime = startTime;
		countdown.EndTime = endTime;
		countdown.Step = step;
		countdown.Command = command;
		countdown.FadeIn = render.FadeIn;
		element.Components.Add(countdown);

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiCountdownComponent>(id, element, countdown);
	}
	public static CUI.Pair<CuiScrollViewComponent> ScrollView(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Transform transform, CUI.Render render, CUI.Needs needs, CUI.Outline outline, bool vertical, bool horizontal, ScrollRect.MovementType movementType, float elasticity, bool inertia, float decelerationRate, float scrollSensitivity, string maskSoftness, out CuiRectTransform contentTransformComponent, out CuiScrollbar horizontalScrollBar, out CuiScrollbar verticalScrollBar, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);

		var scrollview = cui.TakeFromPoolScrollView();
		scrollview.Vertical = vertical;
		scrollview.Horizontal = horizontal;
		scrollview.MovementType = movementType;
		scrollview.Elasticity = elasticity;
		scrollview.Inertia = inertia;
		scrollview.DecelerationRate = decelerationRate;
		scrollview.ScrollSensitivity = scrollSensitivity;
		scrollview.MaskSoftness = maskSoftness;
		contentTransformComponent = scrollview.ContentTransform;
		horizontalScrollBar = scrollview.HorizontalScrollbar;
		verticalScrollBar = scrollview.VerticalScrollbar;

		element.Components.Add(scrollview);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = transform.AnchorMin;
		rect.AnchorMax = transform.AnchorMax;
		rect.OffsetMin = transform.OffsetMin;
		rect.OffsetMax = transform.OffsetMax;
		element.Components.Add(rect);

		if (needs.Cursor) element.Components.Add(CUI.Handler.CachedNeedsCursor);
		if (needs.Keyboard) element.Components.Add(CUI.Handler.CachedNeedsKeyboard);

		if (!update) container?.Add(element);
		return new CUI.Pair<CuiScrollViewComponent>(id, element, scrollview, rect);
	}

	public static void Send(this CuiElementContainer container, BasePlayer player)
	{
		CuiHelper.AddUi(player, container);
	}
	public static byte[] GetData(this CuiElementContainer container)
	{
		var write = Net.sv.StartWrite();
		write.PacketID(Message.Type.RPCMessage);
		write.EntityID(CommunityEntity.ServerInstance.net.ID);
		write.UInt32(AddUiString);
		write.UInt64(0UL);
		write.String(container.ToJson());

		var bytes = new byte[write._length];
		Array.Copy(write.Data, bytes, write._length);

		Facepunch.Pool.Free(ref write);

		return bytes;
	}
	public static void SendData(byte[] data, BasePlayer player)
	{
		var write = Net.sv.StartWrite();
		write.PacketID(Message.Type.RPCMessage);
		write.EnsureCapacity(data.Length);
		Array.Copy(data, 0, write.Data,write.Length , data.Length);
		write._length += data.Length;
		write.Send(new SendInfo(player.Connection));
	}
	public static void SendUpdate(this CUI.Pair<string, CuiElement> pair, BasePlayer player)
	{
		var elements = Facepunch.Pool.GetList<CuiElement>();
		elements.Add(pair.Element);

		CuiHelper.AddUi(player, elements);

		Facepunch.Pool.Free(ref elements);
	}
	public static void Destroy(this CuiElementContainer container, BasePlayer player)
	{
		CuiHelper.DestroyUi(player, container.Name);
	}
	public static void Destroy(string name, BasePlayer player)
	{
		CuiHelper.DestroyUi(player, name);
	}
}
