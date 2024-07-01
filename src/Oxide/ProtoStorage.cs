﻿using ProtoBuf;
using Logger = Carbon.Logger;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Oxide.Core;

public class ProtoStorage
{
	public static IEnumerable<string> GetFiles(string subDirectory)
	{
		var fileDataPath = GetFileDataPath(subDirectory.Replace("..", ""));

		if (!Directory.Exists(fileDataPath))
		{
			yield break;
		}

		foreach (string value in Directory.GetFiles(fileDataPath, "*.data"))
		{
			yield return Utility.GetFileNameWithoutExtension(value);
		}

		yield break;
	}

	public static T Load<T>(params string[] subPaths)
	{
		var fileName = GetFileName(subPaths);
		var fileDataPath = GetFileDataPath(fileName);

		try
		{
			if (File.Exists(fileDataPath))
			{
				using var fileStream = new MemoryStream(OsEx.File.ReadBytes(fileDataPath));
				return Serializer.Deserialize<T>(fileStream);
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to load protobuf data from " + fileName, ex);
		}

		return default;
	}

	public static void Save<T>(T data, params string[] subPaths)
	{
		var fileName = GetFileName(subPaths);
		var fileDataPath = GetFileDataPath(fileName);
		var directoryName = Path.GetDirectoryName(fileDataPath);

		try
		{
			if (directoryName != null && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}

			using var stream = new MemoryStream();
			Serializer.Serialize(stream, data);
			using var temp = TempArray<byte>.New(stream.ToArray());
			OsEx.File.Create(fileDataPath, temp.Array);
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to save protobuf data to " + fileName, ex);
		}
	}

	public static bool Exists(params string[] subPaths)
	{
		return File.Exists(GetFileDataPath(GetFileName(subPaths)));
	}

	public static string GetFileName(params string[] subPaths)
	{
		return string.Join(Path.DirectorySeparatorChar.ToString(), subPaths).Replace("..", "") + ".data";
	}

	public static string GetFileDataPath(string name)
	{
		return Path.Combine(Interface.Oxide.DataDirectory, name);
	}
}
