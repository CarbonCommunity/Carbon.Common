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
	internal static string ProcessColor(string color)
	{
		return color.StartsWith("#") ? CUI.HexToRustColor(color) : color;
	}

	public static CUI.Pair<string, CuiElement> UpdatePanel(this CUI cui, string id, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, bool blur = false, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreatePanel(null, null, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, blur, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateText(this CUI cui, string id, string color, string text, int size, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, TextAnchor align = TextAnchor.MiddleCenter, CUI.Handler.FontTypes font = CUI.Handler.FontTypes.RobotoCondensedRegular, VerticalWrapMode verticalOverflow = VerticalWrapMode.Overflow, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateText(null, null, color, text, size, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, align, font, verticalOverflow, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement, CuiElement> UpdateButton(this CUI cui, string id, string color, string textColor, string text, int size, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, CUI.Handler.FontTypes font = CUI.Handler.FontTypes.RobotoCondensedRegular, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateButton(null, null, color, textColor, text, size, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement, CuiElement> UpdateProtectedButton(this CUI cui, string id, string color, string textColor, string text, int size, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, CUI.Handler.FontTypes font = CUI.Handler.FontTypes.RobotoCondensedRegular, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateProtectedButton(null, null, color, textColor, text, size, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateInputField(this CUI cui, string id, string color, string text, int size, int characterLimit, bool readOnly, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, CUI.Handler.FontTypes font = CUI.Handler.FontTypes.RobotoCondensedRegular, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = UnityEngine.UI.InputField.LineType.SingleLine, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string destroyUi = null)
	{
		return cui.CreateInputField(null, null, color, text, size, characterLimit, readOnly, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, autoFocus, hudMenuInput, lineType, fadeIn, fadeOut, needsCursor, needsKeyboard, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateProtectedInputField(this CUI cui, string id, string color, string text, int size, int characterLimit, bool readOnly, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, string command = null, TextAnchor align = TextAnchor.MiddleCenter, CUI.Handler.FontTypes font = CUI.Handler.FontTypes.RobotoCondensedRegular, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = UnityEngine.UI.InputField.LineType.SingleLine, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string destroyUi = null)
	{
		return cui.CreateProtectedInputField(null, null, color, text, size, characterLimit, readOnly, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, command, align, font, autoFocus, hudMenuInput, lineType, fadeIn, fadeOut, needsCursor, needsKeyboard, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateImage(this CUI cui, string id, uint png, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateImage(null, null, png, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateImage(this CUI cui, string id, string url, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateImage(null, null, url, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateImage(this CUI cui, string id, string url, float scale, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateImage(null, null, url, scale, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateSimpleImage(this CUI cui, string id, string png, string sprite, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateSimpleImage(null, null, png, sprite, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateSprite(this CUI cui, string id, string sprite, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateSprite(null, null, sprite, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateItemImage(this CUI cui, string id, int itemID, ulong skinID, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateItemImage(null, null, itemID, skinID, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateQRCodeImage(this CUI cui, string id, string text, string brandUrl, string brandColor, string brandBgColor, int pixels, bool transparent, bool quietZones, string color, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateQRCodeImage(null, null, text, brandUrl, brandColor, brandBgColor, pixels, transparent, quietZones, color, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateClientImage(this CUI cui, string id, string url, string color, string material = null, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string destroyUi = null)
	{
		return cui.CreateClientImage(null, null, url, color, material, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, outlineColor, outlineDistance, outlineUseGraphicAlpha, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateCountdown(this CUI cui, string id, int startTime, int endTime, int step, string command, float fadeIn = 0f, float fadeOut = 0f, string destroyUi = null)
	{
		return cui.CreateCountdown(null, null, startTime, endTime, step, command, fadeIn, fadeOut, id, destroyUi, true);
	}
	public static CUI.Pair<CuiElement> UpdateScrollView(this CUI cui, string id, bool vertical, bool horizontal, ScrollRect.MovementType movementType, float elasticity, bool inertia, float decelerationRate, float scrollSensitivity, string maskSoftness, out CuiRectTransform contentTransformComponent, out CuiScrollbar horizontalScrollBar, out CuiScrollbar verticalScrollBar, float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f, float OxMin = 0f, float OxMax = 0f, float OyMin = 0f, float OyMax = 0f, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string destroyUi = null)
	{
		return cui.CreateScrollView(null, null, vertical, horizontal, movementType, elasticity, inertia, decelerationRate, scrollSensitivity, maskSoftness, out contentTransformComponent, out horizontalScrollBar, out verticalScrollBar, xMin, xMax, yMin, yMax, OxMin, OxMax, OyMin, OyMax, fadeIn, fadeOut, needsCursor, needsKeyboard, id, destroyUi, true);
	}

	public static CUI.Pair<CuiImageComponent> Panel(this CUI.Handler cui, CuiElementContainer container, string parent, CUI.Position position, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string id = null, string destroyUi = null, bool update = false)
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
		rect.AnchorMin = position.AnchorMin;
		rect.AnchorMax = position.AnchorMax;
		rect.OffsetMin = position.OffsetMin;
		rect.OffsetMax = position.OffsetMax;
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
	public static CUI.Pair<CuiTextComponent> Text(this CUI.Handler cui, CuiElementContainer container, string parent, string color, string text, int size, TextAnchor align, CUI.Handler.FontTypes font, VerticalWrapMode verticalOverflow,CUI.Position position, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string id = null, string destroyUi = null, bool update = false)
	{
		id ??= cui.AppendId();

		var element = cui.TakeFromPool(id, parent, render.FadeOut, destroyUi, update);
		var outlineComp = (CuiOutlineComponent)default;

		var label = cui.TakeFromPoolText();
		label.Text = string.IsNullOrEmpty(text) ? string.Empty : text;
		label.FontSize = size;
		label.Align = align;
		label.Font = cui.GetFont(font);
		label.Color = ProcessColor(color);
		label.FadeIn = render.FadeIn;
		label.VerticalOverflow = verticalOverflow;
		element.Components.Add(label);

		var rect = cui.TakeFromPoolRect();
		rect.AnchorMin = position.AnchorMin;
		rect.AnchorMax = position.AnchorMax;
		rect.OffsetMin = position.OffsetMin;
		rect.OffsetMax = position.OffsetMax;
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
	public static CUI.Pair<CuiButtonComponent, CuiElement, CuiTextComponent, CuiRectTransformComponent> Button(this CUI.Handler cui, CuiElementContainer container, string parent, string textColor, string text, int size, string command, TextAnchor align, CUI.Handler.FontTypes font, bool @protected, CUI.Position position, CUI.Render render, CUI.Needs needs, CUI.Outline outline, string id = null, string destroyUi = null, bool update = false)
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
		rect.AnchorMin = position.AnchorMin;
		rect.AnchorMax = position.AnchorMax;
		rect.OffsetMin = position.OffsetMin;
		rect.OffsetMax = position.OffsetMax;
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
	public static CUI.Pair<CuiElement> InputField(this CUI.Handler cui, CuiElementContainer container, string parent, string color, string text, int size, int characterLimit, bool readOnly, float xMin, float xMax, float yMin, float yMax, float OxMin, float OxMax, float OyMin, float OyMax, string command, TextAnchor align, CUI.Handler.FontTypes font, bool @protected, bool autoFocus = false, bool hudMenuInput = false, InputField.LineType lineType = UnityEngine.UI.InputField.LineType.SingleLine, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, fadeOut, destroyUi, update);

		var inputField = cui.TakeFromPoolInputField();
		inputField.Color = ProcessColor(color);
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

		if (needsCursor) element.Components.Add(cui.TakeFromPoolNeedsCursor());
		if (needsKeyboard && !inputField.ReadOnly) element.Components.Add(cui.TakeFromPoolNeedsKeyboard());

		if (!update || (update && (xMin != 0 || xMax != 1 || yMin != 0 || yMax != 1)))
		{
			var rect = cui.TakeFromPoolRect();
			rect.AnchorMin = $"{xMin} {yMin}";
			rect.AnchorMax = $"{xMax} {yMax}";
			rect.OffsetMin = $"{OxMin} {OyMin}";
			rect.OffsetMax = $"{OxMax} {OyMax}";
			element.Components.Add(rect);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<string, CuiElement>(id, element);
	}
	public static CUI.Pair<CuiElement> Image(this CUI.Handler cui, CuiElementContainer container, string parent, string png, string url, string color, string material, float xMin, float xMax, float yMin, float yMax, float OxMin, float OxMax, float OyMin, float OyMax, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, fadeOut, destroyUi, update);

		var rawImage = cui.TakeFromPoolRawImage();
		rawImage.Png = png;
		rawImage.Url = url;
		rawImage.FadeIn = fadeIn;
		rawImage.Color = ProcessColor(color);
		if (material != null) rawImage.Material = material;
		element.Components.Add(rawImage);

		if (!update || (update && (xMin != 0 || xMax != 1 || yMin != 0 || yMax != 1)))
		{
			var rect = cui.TakeFromPoolRect();
			rect.AnchorMin = $"{xMin} {yMin}";
			rect.AnchorMax = $"{xMax} {yMax}";
			rect.OffsetMin = $"{OxMin} {OyMin}";
			rect.OffsetMax = $"{OxMax} {OyMax}";
			element.Components.Add(rect);
		}

		if (needsCursor) element.Components.Add(cui.TakeFromPoolNeedsCursor());
		if (needsKeyboard) element.Components.Add(cui.TakeFromPoolNeedsKeyboard());

		if (outlineColor != null)
		{
			var outline = cui.TakeFromPoolOutline();
			outline.Color = ProcessColor(outlineColor);
			outline.Distance = outlineDistance;
			outline.UseGraphicAlpha = outlineUseGraphicAlpha;
			element.Components.Add(outline);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<string, CuiElement>(id, element);
	}
	public static CUI.Pair<CuiElement> SimpleImage(this CUI.Handler cui, CuiElementContainer container, string parent, string png, string sprite, string color, string material, float xMin, float xMax, float yMin, float yMax, float OxMin, float OxMax, float OyMin, float OyMax, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, fadeOut, destroyUi, update);

		var simpleImage = cui.TakeFromPoolImage();
		simpleImage.Png = png;
		simpleImage.Sprite = sprite;
		simpleImage.FadeIn = fadeIn;
		simpleImage.Color = ProcessColor(color);
		if (material != null) simpleImage.Material = material;
		element.Components.Add(simpleImage);

		if (!update || (update && (xMin != 0 || xMax != 1 || yMin != 0 || yMax != 1)))
		{
			var rect = cui.TakeFromPoolRect();
			rect.AnchorMin = $"{xMin} {yMin}";
			rect.AnchorMax = $"{xMax} {yMax}";
			rect.OffsetMin = $"{OxMin} {OyMin}";
			rect.OffsetMax = $"{OxMax} {OyMax}";
			element.Components.Add(rect);
		}

		if (needsCursor) element.Components.Add(cui.TakeFromPoolNeedsCursor());
		if (needsKeyboard) element.Components.Add(cui.TakeFromPoolNeedsKeyboard());

		if (outlineColor != null)
		{
			var outline = cui.TakeFromPoolOutline();
			outline.Color = ProcessColor(outlineColor);
			outline.Distance = outlineDistance;
			outline.UseGraphicAlpha = outlineUseGraphicAlpha;
			element.Components.Add(outline);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<string, CuiElement>(id, element);
	}
	public static CUI.Pair<CuiElement> Sprite(this CUI.Handler cui, CuiElementContainer container, string parent, string sprite, string color, string material, float xMin, float xMax, float yMin, float yMax, float OxMin, float OxMax, float OyMin, float OyMax, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, fadeOut, destroyUi, update);

		var rawImage = cui.TakeFromPoolRawImage();
		rawImage.Sprite = sprite;
		rawImage.FadeIn = fadeIn;
		rawImage.Color = ProcessColor(color);
		if (material != null) rawImage.Material = material;
		element.Components.Add(rawImage);

		if (!update || (update && (xMin != 0 || xMax != 1 || yMin != 0 || yMax != 1)))
		{
			var rect = cui.TakeFromPoolRect();
			rect.AnchorMin = $"{xMin} {yMin}";
			rect.AnchorMax = $"{xMax} {yMax}";
			rect.OffsetMin = $"{OxMin} {OyMin}";
			rect.OffsetMax = $"{OxMax} {OyMax}";
			element.Components.Add(rect);
		}

		if (needsCursor) element.Components.Add(cui.TakeFromPoolNeedsCursor());
		if (needsKeyboard) element.Components.Add(cui.TakeFromPoolNeedsKeyboard());

		if (outlineColor != null)
		{
			var outline = cui.TakeFromPoolOutline();
			outline.Color = ProcessColor(outlineColor);
			outline.Distance = outlineDistance;
			outline.UseGraphicAlpha = outlineUseGraphicAlpha;
			element.Components.Add(outline);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<string, CuiElement>(id, element);
	}
	public static CUI.Pair<CuiElement> ItemImage(this CUI.Handler cui, CuiElementContainer container, string parent, int itemID, ulong skinID, string color, string material, float xMin, float xMax, float yMin, float yMax, float OxMin, float OxMax, float OyMin, float OyMax, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string outlineColor = null, string outlineDistance = null, bool outlineUseGraphicAlpha = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, fadeOut, destroyUi, update);

		var rawImage = cui.TakeFromPoolImage();
		rawImage.ItemId = itemID;
		rawImage.SkinId = skinID;
		rawImage.FadeIn = fadeIn;
		rawImage.Color = ProcessColor(color);
		if (material != null) rawImage.Material = material;
		element.Components.Add(rawImage);

		if (!update || (update && (xMin != 0 || xMax != 1 || yMin != 0 || yMax != 1)))
		{
			var rect = cui.TakeFromPoolRect();
			rect.AnchorMin = $"{xMin} {yMin}";
			rect.AnchorMax = $"{xMax} {yMax}";
			rect.OffsetMin = $"{OxMin} {OyMin}";
			rect.OffsetMax = $"{OxMax} {OyMax}";
			element.Components.Add(rect);
		}

		if (needsCursor) element.Components.Add(cui.TakeFromPoolNeedsCursor());
		if (needsKeyboard) element.Components.Add(cui.TakeFromPoolNeedsKeyboard());

		if (outlineColor != null)
		{
			var outline = cui.TakeFromPoolOutline();
			outline.Color = ProcessColor(outlineColor);
			outline.Distance = outlineDistance;
			outline.UseGraphicAlpha = outlineUseGraphicAlpha;
			element.Components.Add(outline);
		}

		if (!update) container?.Add(element);
		return new CUI.Pair<string, CuiElement>(id, element);
	}
	public static CUI.Pair<CuiElement> Countdown(this CUI.Handler cui, CuiElementContainer container, string parent, int startTime, int endTime, int step, string command, float fadeIn = 0f, float fadeOut = 0f, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, fadeOut, destroyUi, update);

		var countdown = cui.TakeFromPoolCountdown();
		countdown.StartTime = startTime;
		countdown.EndTime = endTime;
		countdown.Step = step;
		countdown.Command = command;
		countdown.FadeIn = fadeIn;
		element.Components.Add(countdown);

		if (!update) container?.Add(element);
		return new CUI.Pair<string, CuiElement>(id, element);
	}
	public static CUI.Pair<CuiElement> ScrollView(this CUI.Handler cui, CuiElementContainer container, string parent, bool vertical, bool horizontal, ScrollRect.MovementType movementType, float elasticity, bool inertia, float decelerationRate, float scrollSensitivity, string maskSoftness, out CuiRectTransform contentTransformComponent, out CuiScrollbar horizontalScrollBar, out CuiScrollbar verticalScrollBar, float xMin, float xMax, float yMin, float yMax, float OxMin, float OxMax, float OyMin, float OyMax, float fadeIn = 0f, float fadeOut = 0f, bool needsCursor = false, bool needsKeyboard = false, string id = null, string destroyUi = null, bool update = false)
	{
		if (id == null) id = cui.AppendId();
		var element = cui.TakeFromPool(id, parent, fadeOut, destroyUi, update);

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

		if (!update || (update && (xMin != 0 || xMax != 1 || yMin != 0 || yMax != 1)))
		{
			var rect = cui.TakeFromPoolRect();
			rect.AnchorMin = $"{xMin} {yMin}";
			rect.AnchorMax = $"{xMax} {yMax}";
			rect.OffsetMin = $"{OxMin} {OyMin}";
			rect.OffsetMax = $"{OxMax} {OyMax}";
			element.Components.Add(rect);
		}

		if (needsCursor) element.Components.Add(cui.TakeFromPoolNeedsCursor());
		if (needsKeyboard) element.Components.Add(cui.TakeFromPoolNeedsKeyboard());

		if (!update) container?.Add(element);
		return new CUI.Pair<string, CuiElement>(id, element);
	}

	public static readonly uint AddUiString = StringPool.Get("AddUi");

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
