/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

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

		public static Position CreateAnchor(float xMin, float xMax, float yMin, float yMax)
		{
			Position position = default;
			position.XMin = xMin;
			position.XMax = xMax;
			position.YMin = yMin;
			position.YMax = yMax;
			return position;
		}
		public static Position CreateAnchor(float xMin, float xMax, float yMin, float yMax, float oxMin, float oxMax, float oyMin, float oyMax)
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
		public string Material;
		public bool Blur;
	}

	public struct Outline
	{
		public string Color;
		public string Distance;
		public bool UseGraphicAlpha;
	}
}
