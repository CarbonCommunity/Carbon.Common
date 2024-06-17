/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

using Oxide.Game.Rust.Cui;

namespace Carbon.Components;

public readonly partial struct CUI
{
	public struct Transform
	{
		public float XMin;
		public float XMax;
		public float YMin;
		public float YMax;

		public float OxMin;
		public float OxMax;
		public float OyMin;
		public float OyMax;

		public readonly string AnchorMin => $"{XMin} {YMin}";
		public readonly string AnchorMax => $"{XMax} {YMax}";
		public readonly string OffsetMin => $"{OxMin} {OyMin}";
		public readonly string OffsetMax => $"{OxMax} {OyMax}";

		public Transform WithXMin(float value)
		{
			XMin = value;
			return this;
		}
		public Transform WithXMax(float value)
		{
			XMax = value;
			return this;
		}
		public Transform WithYMin(float value)
		{
			YMin = value;
			return this;
		}
		public Transform WithYMax(float value)
		{
			YMax = value;
			return this;
		}
		public Transform WithAnchor(float xMin, float xMax, float yMin, float yMax)
		{
			XMin = xMin;
			XMax = xMax;
			YMin = yMin;
			YMax = yMax;
			return this;
		}
		public Transform WithAnchor(float xMin, float xMax, float yMin, float yMax, float oxMin, float oxMax, float oyMin, float oyMax)
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

		public Transform WithXMinOffset(float value)
		{
			OxMin = value;
			return this;
		}
		public Transform WithXMaxOffset(float value)
		{
			OxMax = value;
			return this;
		}
		public Transform WithYMinOffset(float value)
		{
			OyMin = value;
			return this;
		}
		public Transform WithYMaxOffset(float value)
		{
			OyMax = value;
			return this;
		}
		public Transform WithOffset(float oxMin, float oxMax, float oyMin, float oyMax)
		{
			OxMin = oxMin;
			OxMax = oxMax;
			OyMin = oyMin;
			OyMax = oyMax;
			return this;
		}

		public static Transform Default
		{
			get
			{
				Transform result = default;
				result.XMax = 1;
				result.YMax = 1;
				return result;
			}
		}
		public static Transform Full => Default;
		public static Transform HalfCut
		{
			get
			{
				Transform result = default;
				result.XMin = result.XMax = result.YMin = result.YMax = 0.5f;
				return result;
			}
		}
		public static Transform Pixel
		{
			get
			{
				return default;
			}
		}

		public static Transform CreateAnchor(float xMin, float xMax, float yMin, float yMax, float oxMin = 0, float oxMax = 0, float oyMin = 0, float oyMax = 0)
		{
			Transform transform = default;
			transform.XMin = xMin;
			transform.XMax = xMax;
			transform.YMin = yMin;
			transform.YMax = yMax;
			transform.OxMin = oxMin;
			transform.OxMax = oxMax;
			transform.OyMin = oyMin;
			transform.OyMax = oyMax;
			return transform;
		}
	}

	public struct Render
	{
		public const string BlurMaterial = "assets/content/ui/uibackgroundblur.mat";

		public string Color;
		public string Material;
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
		public Render WithBlur(bool wants)
		{
			return wants ? WithBlur() : this;
		}
		public Render WithFade(float fadeIn = 0, float fadeOut = 0)
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
				result.FadeIn = 0;
				result.FadeOut = 0;
				return result;
			}
		}
		public static Render White
		{
			get
			{
				Render result = default;
				result.Color = Cache.CUI.WhiteColor;
				return result;
			}
		}
		public static Render Black
		{
			get
			{
				Render result = default;
				result.Color = Cache.CUI.BlackColor;
				return result;
			}
		}
		public static Render Blank
		{
			get
			{
				Render result = default;
				result.Color = Cache.CUI.BlankColor;
				return result;
			}
		}
		public static Render Red
		{
			get
			{
				Render result = default;
				result.Color = "1 0 0 1";
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
		public static Needs CreateCursor
		{
			get
			{
				Needs result = default;
				result.Cursor = true;
				return result;
			}
		}
		public static Needs CreateKeyboard
		{
			get
			{
				Needs result = default;
				result.Keyboard = true;
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
