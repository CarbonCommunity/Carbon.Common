﻿/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

using Oxide.Game.Rust.Cui;

namespace Carbon.Components;

public readonly partial struct CUI
{
	public struct Position
	{
		public float XMin;
		public float XMax;
		public float YMin;
		public float YMax;

		public float OxMin;
		public float OxMax;
		public float OyMin;
		public float OyMax;

		public string AnchorMin => $"{XMin} {YMin}";
		public string AnchorMax => $"{XMax} {YMax}";
		public string OffsetMin => $"{OxMin} {OyMin}";
		public string OffsetMax => $"{OxMax} {OyMax}";

		public Position WithXMin(float value)
		{
			XMin = value;
			return this;
		}
		public Position WithXMax(float value)
		{
			XMax = value;
			return this;
		}
		public Position WithYMin(float value)
		{
			YMin = value;
			return this;
		}
		public Position WithYMax(float value)
		{
			YMax = value;
			return this;
		}

		public Position WithXMinOffset(float value)
		{
			OxMin = value;
			return this;
		}
		public Position WithXMaxOffset(float value)
		{
			OxMax = value;
			return this;
		}
		public Position WithYMinOffset(float value)
		{
			OyMin = value;
			return this;
		}
		public Position WithYMaxOffset(float value)
		{
			OyMax = value;
			return this;
		}

		public Position WithAnchor(float xMin, float xMax, float yMin, float yMax)
		{
			XMin = xMin;
			XMax = xMax;
			YMin = yMin;
			YMax = yMax;
			return this;
		}
		public Position WithAnchor(float xMin, float xMax, float yMin, float yMax, float oxMin, float oxMax, float oyMin, float oyMax)
		{
			XMin = xMin;
			XMax = xMax;
			YMin = yMin;
			YMax = yMax;
			OxMin = oxMin;
			OxMax = oxMax;
			OyMin = oyMin;
			OyMax = oyMax;
			return this;
		}

		public static Position Default
		{
			get
			{
				Position result = default;
				result.XMax = 1;
				result.YMax = 1;
				return result;
			}
		}
		public static Position CreateAnchor(float xMin, float xMax, float yMin, float yMax, float oxMin = 0, float oxMax = 0, float oyMin = 0, float oyMax = 0)
		{
			Position position = default;
			position.XMin = xMin;
			position.XMax = xMax;
			position.YMin = yMin;
			position.YMax = yMax;
			position.OxMin = oxMin;
			position.OxMax = oxMax;
			position.OyMin = oyMin;
			position.OyMax = oyMax;
			return position;
		}
	}

	public struct Render
	{
		public const string BlurMaterial = "assets/content/ui/uibackgroundblur.mat";

		public string Color;
		public string Material;
		public bool Blur;
		public float FadeIn;
		public float FadeOut;

		public bool HasMaterial => !string.IsNullOrEmpty(Material);

		public Render WithColor(string value)
		{
			Color = value;
			return this;
		}
		public Render WithMaterial(string value)
		{
			Material = value;
			return this;
		}
		public Render WithBlur()
		{
			Material = BlurMaterial;
			return this;
		}
		public Render WithFade(float fadeIn, float fadeOut)
		{
			FadeIn = fadeIn;
			FadeOut = fadeOut;
			return this;
		}

		public static Render Default
		{
			get
			{
				Render result = default;
				result.Color = Cache.CUI.WhiteColor;
				result.Material = string.Empty;
				result.Blur = false;
				result.FadeIn = 0;
				result.FadeOut = 0;
				return result;
			}
		}
	}

	public struct Outline
	{
		public string Color;
		public string Distance;
		public bool UseGraphicAlpha;

		public bool Valid => !string.IsNullOrEmpty(Color);

		public Outline WithColor(string value)
		{
			Color = value;
			return this;
		}
		public Outline WithDistance(string value)
		{
			Distance = value;
			return this;
		}
		public Outline WithGraphicAlpha(bool value)
		{
			UseGraphicAlpha = value;
			return this;
		}

		public static Outline Default
		{
			get
			{
				Outline result = default;
				result.Color = Cache.CUI.BlankColor;
				result.Distance = "0 0";
				result.UseGraphicAlpha = false;
				return result;
			}
		}
	}

	public struct Needs
	{
		public bool Cursor;
		public bool Keyboard;

		public Needs WithCursor()
		{
			Cursor = true;
			return this;
		}
		public Needs WithKeyboard()
		{
			Keyboard = true;
			return this;
		}

		public static Needs Default
		{
			get
			{
				Needs result = default;
				result.Cursor = false;
				result.Keyboard = false;
				return result;
			}
		}
	}

	public struct Pair
	{
		public string Id;
		public CuiElement Element;
		public CuiRectTransformComponent Rect;
		public CuiOutlineComponent Outline;

		public Pair(string id, CuiElement element, CuiRectTransformComponent rect = null, CuiOutlineComponent outline = null)
		{
			Id = id;
			Element = element;
			Rect = rect;
			Outline = outline;
		}

		public static implicit operator string(Pair value)
		{
			return value.Id;
		}
	}

	public struct Pair<T1>
	{
		public string Id;
		public CuiElement Element;
		public CuiRectTransformComponent Rect;
		public CuiOutlineComponent Outline;

		public T1 Pair1;

		public Pair(string id, CuiElement element, T1 pair1, CuiRectTransformComponent rect = null, CuiOutlineComponent outline = null)
		{
			Id = id;
			Element = element;
			Rect = rect;
			Outline = outline;
			Pair1 = pair1;
		}

		public static implicit operator string(Pair<T1> value)
		{
			return value.Id;
		}
	}

	public struct Pair<T1, T2>
	{
		public string Id;
		public CuiElement Element;
		public CuiRectTransformComponent Rect;
		public CuiOutlineComponent Outline;

		public T1 Pair1;
		public T2 Pair2;

		public Pair(string id, CuiElement element, T1 pair1, T2 pair2, CuiRectTransformComponent rect = null, CuiOutlineComponent outline = null)
		{
			Id = id;
			Element = element;
			Rect = rect;
			Outline = outline;
			Pair1 = pair1;
			Pair2 = pair2;
		}

		public static implicit operator string(Pair<T1, T2> value)
		{
			return value.Id;
		}
	}

	public struct Pair<T1, T2, T3>
	{
		public string Id;
		public CuiElement Element;
		public CuiRectTransformComponent Rect;
		public CuiOutlineComponent Outline;

		public T1 Pair1;
		public T2 Pair2;
		public T3 Pair3;

		public Pair(string id, CuiElement element, T1 pair1, T2 pair2, T3 pair3, CuiRectTransformComponent rect = null, CuiOutlineComponent outline = null)
		{
			Id = id;
			Element = element;
			Rect = rect;
			Outline = outline;
			Pair1 = pair1;
			Pair2 = pair2;
			Pair3 = pair3;
		}

		public static implicit operator string(Pair<T1, T2, T3> value)
		{
			return value.Id;
		}
	}

	public struct Pair<T1, T2, T3, T4>
	{
		public string Id;
		public CuiElement Element;
		public CuiRectTransformComponent Rect;
		public CuiOutlineComponent Outline;

		public T1 Pair1;
		public T2 Pair2;
		public T3 Pair3;
		public T4 Pair4;

		public Pair(string id, CuiElement element, T1 pair1, T2 pair2, T3 pair3, T4 pair4, CuiRectTransformComponent rect = null, CuiOutlineComponent outline = null)
		{
			Id = id;
			Element = element;
			Rect = rect;
			Outline = outline;
			Pair1 = pair1;
			Pair2 = pair2;
			Pair3 = pair3;
			Pair4 = pair4;
		}

		public static implicit operator string(Pair<T1, T2, T3, T4> value)
		{
			return value.Id;
		}
	}

	public struct Pair<T1, T2, T3, T4, T5>
	{
		public string Id;
		public CuiElement Element;
		public CuiRectTransformComponent Rect;
		public CuiOutlineComponent Outline;

		public T1 Pair1;
		public T2 Pair2;
		public T3 Pair3;
		public T4 Pair4;
		public T5 Pair5;

		public Pair(string id, CuiElement element, T1 pair1, T2 pair2, T3 pair3, T4 pair4, T5 pair5, CuiRectTransformComponent rect = null, CuiOutlineComponent outline = null)
		{
			Id = id;
			Element = element;
			Rect = rect;
			Outline = outline;
			Pair1 = pair1;
			Pair2 = pair2;
			Pair3 = pair3;
			Pair4 = pair4;
			Pair5 = pair5;
		}

		public static implicit operator string(Pair<T1, T2, T3, T4, T5> value)
		{
			return value.Id;
		}
	}
}
