﻿using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using ProtoBuf;
using QRCoder;
using Color = System.Drawing.Color;
using Defines = Carbon.Core.Defines;
using Graphics = System.Drawing.Graphics;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Modules;

public class ImageDatabaseModule : CarbonModule<ImageDatabaseConfig, EmptyModuleData>
{
	public override string Name => "ImageDatabase";
	public override Type Type => typeof(ImageDatabaseModule);
	public override bool EnabledByDefault => true;
	public override bool ForceEnabled => true;

	internal readonly List<QueuedThread> _queue = new();
	internal ImageDatabaseDataProto _protoData { get; set; }

	internal Dictionary<string, string> _defaultImages = new()
	{
		["carbonb"] = "https://carbonmod.gg/assets/media/carbonlogo_b.png",
		["carbonw"] = "https://carbonmod.gg/assets/media/carbonlogo_w.png",
		["carbonbs"] = "https://carbonmod.gg/assets/media/carbonlogo_bs.png",
		["carbonws"] = "https://carbonmod.gg/assets/media/carbonlogo_ws.png",
		["cflogo"] = "https://carbonmod.gg/assets/media/cui/codefling-logo.png",
		["checkmark"] = "https://carbonmod.gg/assets/media/cui/checkmark.png",
		["umodlogo"] = "https://carbonmod.gg/assets/media/cui/umod-logo.png",
		["clouddl"] = "https://carbonmod.gg/assets/media/cui/cloud-dl.png",
		["trashcan"] = "https://carbonmod.gg/assets/media/cui/trash-can.png",
		["shopping"] = "https://carbonmod.gg/assets/media/cui/shopping-cart.png",
		["installed"] = "https://carbonmod.gg/assets/media/cui/installed.png",
		["reload"] = "https://carbonmod.gg/assets/media/cui/reload.png",
		["update-pending"] = "https://carbonmod.gg/assets/media/cui/update-pending.png",
		["magnifying-glass"] = "https://carbonmod.gg/assets/media/cui/magnifying-glass.png",
		["star"] = "https://carbonmod.gg/assets/media/cui/star.png",
		["glow"] = "https://b0f7b4d5.carbon-website.pages.dev/assets/media/cui/glow.png"
	};
	internal IEnumerator _executeQueue(QueuedThread thread, Action<List<QueuedThreadResult>> onFinished)
	{
		thread.Start();

		while (thread != null && !thread.IsDone) { yield return null; }

		onFinished?.Invoke(thread.Result);
	}
	internal string _getProtoDataPath()
	{
		return Path.Combine(Defines.GetModulesFolder(), Name, "data.db");
	}

	internal const int MaximumBytes = 4104304;

	[ConsoleCommand("imagedb.pending")]
	[AuthLevel(2)]
	private void ShowPending(ConsoleSystem.Arg arg)
	{
		arg.ReplyWith($"Queued {_queue.Count} batches of {_queue.Sum(x => x._urlQueue.Count):n0} URLs.");
	}

	[ConsoleCommand("imagedb.clearinvalid")]
	[AuthLevel(2)]
	private void ClearInvalid(ConsoleSystem.Arg arg)
	{
		var toDelete = Facepunch.Pool.GetList<KeyValuePair<uint, FileStorage.CacheData>>();

		foreach (var file in FileStorage.server._cache)
		{
			if (file.Value.data.Length >= MaximumBytes)
			{
				toDelete.Add(new KeyValuePair<uint, FileStorage.CacheData>(file.Key, file.Value));
			}
		}

		foreach (var data in toDelete)
		{
			FileStorage.server.Remove(data.Key, FileStorage.Type.png, data.Value.entityID);
		}

		arg.ReplyWith($"Removed {toDelete.Count:n0} invalid stored files from FileStorage (above the maximum size of {ByteEx.Format(MaximumBytes, shortName: true).ToUpper()}).");
		Facepunch.Pool.FreeList(ref toDelete);
	}

	public override void OnServerInit(bool initial)
	{
		base.OnServerInit(initial);

		if(!initial) return;

		if (!Validate()) return;

		Save();
		LoadDefaultImages();

	}
	public override void OnServerSaved()
	{
		base.OnServerSaved();

		SaveDatabase();
	}

	public override void Load()
	{
		var path = _getProtoDataPath();

		if (OsEx.File.Exists(path))
		{
			using var stream = new MemoryStream(OsEx.File.ReadBytes(path));
			try
			{
				_protoData = Serializer.Deserialize<ImageDatabaseDataProto>(stream);
			}
			catch
			{
				_protoData = new ImageDatabaseDataProto();
				Save();
			}
		}
		else
		{
			_protoData = new ImageDatabaseDataProto();
		}

		base.Load();
	}
	public override void Save()
	{
		base.Save();

		SaveDatabase();
	}
	public void SaveDatabase()
	{
		var path = _getProtoDataPath();
		OsEx.Folder.Create(Path.GetDirectoryName(path));

		using var stream = new MemoryStream();
		Serializer.Serialize(stream, _protoData ??= new ImageDatabaseDataProto());

		var result = stream.ToArray();
		OsEx.File.Create(path, result);
		Array.Clear(result,0,result.Length);
		result = null;
	}
	private void LoadDefaultImages()
	{
		Queue(_defaultImages);
	}

	public override bool PreLoadShouldSave(bool newConfig, bool newData)
	{
		var shouldSave = false;

		if (_protoData.Map == null)
		{
			_protoData.Map = new Dictionary<string, uint>();
			shouldSave = true;
		}

		if (_protoData.CustomMap == null)
		{
			_protoData.CustomMap = new Dictionary<string, string>();
			shouldSave = true;
		}

		return shouldSave;
	}

	public bool Validate()
	{
		if (_protoData.Identifier != CommunityEntity.ServerInstance.net.ID.Value)
		{
			PutsWarn($"The server identifier has changed. Wiping old image database. [old {_protoData.Identifier}, new {CommunityEntity.ServerInstance.net.ID.Value}]"); _protoData.Map.Clear();
			_protoData.CustomMap.Clear();
			_protoData.Map.Clear();
			_protoData.Identifier = CommunityEntity.ServerInstance.net.ID.Value;
			return true;
		}

		var invalidations = Facepunch.Pool.GetList<string>();

		foreach (var pointer in _protoData.Map)
		{
			if (FileStorage.server.Get(pointer.Value, FileStorage.Type.png, new NetworkableId(_protoData.Identifier)) == null)
			{
				invalidations.Add(pointer.Key);
			}
		}

		foreach (var invalidation in invalidations)
		{
			_protoData.Map.Remove(invalidation);
		}

		var invalidated = invalidations.Count > 0;
		Facepunch.Pool.FreeList(ref invalidations);

		return invalidated;
	}

	public void QueueBatch(bool @override, params string[] urls)
	{
		QueueBatch(0f, @override, urls);
	}
	public void QueueBatch(float scale, bool @override, params string[] urls)
	{
		if (urls == null || urls.Length == 0)
		{
			return;
		}

		QueueBatch(scale, @override, results =>
		{
			foreach (var result in results)
			{
				if (result.Data.Length >= MaximumBytes)
				{
					Puts($"Failed storing {urls.Length:n0} jobs [scale:{scale}]: {result.Data.Length} more or equal than {MaximumBytes}");
					continue;
				}

				var id = FileStorage.server.Store(result.Data, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));
				if (id != 0) _protoData.Map[GetId(result.Url, scale)] = id;
			}
		}, urls);
	}
	public void QueueBatch(float scale, bool @override, Action<List<QueuedThreadResult>> onComplete, params string[] urls)
	{
		if (urls == null || urls.Length == 0)
		{
			return;
		}

		var thread = new QueuedThread
		{
			Scale = scale
		};
		try
		{
			thread.ImageUrls.AddRange(urls);
			_queue.Add(thread);

			if (!@override)
			{
				foreach (var url in urls)
				{
					if (GetImage(url, scale, true) != 0) thread.ImageUrls.Remove(url);
				}
			}
			else
			{
				foreach (var url in thread.ImageUrls)
				{
					DeleteImage(url, 0);
					if (scale != 0f) DeleteImage(url, scale);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed processing queue batch", ex);
		}

		if (ConfigInstance.InitializedBatchLogs && thread.ImageUrls.Count > 0) Puts($"Added {thread.ImageUrls.Count:n0} to the queue (scale: {(scale == 0 ? "default" : $"{scale:0.0}")})...");

		Community.Runtime.CorePlugin.persistence.StartCoroutine(_executeQueue(thread, results =>
		{
			try
			{
				if (results != null)
				{
					onComplete?.Invoke(results);
					if (ConfigInstance.CompletedBatchLogs && results.Count > 0) Puts($"Completed queue of {results.Count:n0} urls (scale: {(scale == 0 ? "default" : $"{scale:0.0}")}).");
				}

				_queue.Remove(thread);
			}
			catch (Exception ex)
			{
				PutsError($"Failed QueueBatch of {urls.Length:n0}", ex);
			}
		}));

		Community.Runtime.CorePlugin.timer.In(ConfigInstance.TimeoutPerUrl * urls.Length, () =>
		{
			if (thread._disposed) return;

			try
			{
				thread.DisposalSave();
				onComplete?.Invoke(thread.Result);
				if (ConfigInstance.CompletedBatchLogs && thread.Result.Count > 0) Puts($"Completed queue of {thread.Result.Count:n0} urls (scale: {(scale == 0 ? "default" : $"{scale:0.0}")}).");
				thread.Dispose();
				_queue.Remove(thread);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed timeout process", ex);
			}
		});
	}
	public void QueueBatchCallback(float scale, bool @override, Action<List<QueuedThreadResult>> onComplete, params string[] urls)
	{
		if (urls == null || urls.Length == 0)
		{
			return;
		}

		QueueBatch(scale, @override, results =>
		{
			foreach (var result in results)
			{
				if (result.Data.Length >= MaximumBytes)
				{
					Puts($"Failed storing {urls.Length:n0} jobs [scale:{scale}]: {result.Data.Length} more or equal than {MaximumBytes}");
					continue;
				}

				var id = FileStorage.server.Store(result.Data, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));
				if (id != 0) _protoData.Map[GetId(result.Url, scale)] = id;
			}

			onComplete?.Invoke(results);
		}, urls);
	}

	public void Queue(float scale, bool @override, Dictionary<string, string> mappedUrls)
	{
		if (mappedUrls == null || mappedUrls.Count == 0)
		{
			return;
		}

		var urls = Facepunch.Pool.GetList<string>();

		foreach (var url in mappedUrls)
		{
			urls.Add(url.Value);
			AddMap(url.Key, url.Value);
		}

		QueueBatch(scale, @override, urls.ToArray());

		Facepunch.Pool.FreeList(ref urls);
	}
	public void Queue(bool @override, Dictionary<string, string> mappedUrls)
	{
		if (mappedUrls == null || mappedUrls.Count == 0)
		{
			return;
		}

		Queue(0, @override, mappedUrls);
	}
	public void Queue(Dictionary<string, string> mappedUrls)
	{
		if (mappedUrls == null || mappedUrls.Count == 0)
		{
			return;
		}

		Queue(true, mappedUrls);
	}

	public void AddMap(string key, string url)
	{
		_protoData.CustomMap[key] = url;
	}
	public void RemoveMap(string key)
	{
		if (_protoData.CustomMap.ContainsKey(key)) _protoData.CustomMap.Remove(key);
	}

	public uint GetImage(string keyOrUrl, float scale = 0, bool silent = false)
	{
		if (string.IsNullOrEmpty(keyOrUrl)) return default;

		if (_protoData.CustomMap.TryGetValue(keyOrUrl, out var realUrl))
		{
			keyOrUrl = realUrl;
		}

		var id = GetId(keyOrUrl, scale);

		if (_protoData.Map.TryGetValue(id, out var uid))
		{
			if (!silent && ConfigInstance.RetrievedImageLogs) Puts($"Retrieved image '{keyOrUrl}'{(scale == 0 ? "" : $" (scale:{scale:0.0})")}.");
			return uid;
		}

		return scale != 0 ? GetImage(keyOrUrl, 0, silent) : 0;
	}
	public string GetImageString(string keyOrUrl, float scale = 0, bool silent = false)
	{
		return GetImage(keyOrUrl, scale, silent).ToString();
	}
	public bool DeleteImage(string url, float scale = 0)
	{
		var id = GetId(url, scale);

		if (_protoData.Map.TryGetValue(id, out var uid))
		{
			if (ConfigInstance.DeletedImageLogs) Puts($"Deleted image '{url}' (scale: {(scale == 0 ? "default" : $"{scale:0.0}")}).");

			FileStorage.server.Remove(uid, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));
			_protoData.Map.Remove(id);
			return true;
		}

		return false;
	}

	public uint GetQRCode(string text, int pixels = 20, bool transparent = false, bool quietZones = true, bool whiteMode = false)
	{
		if (_protoData.Map.TryGetValue($"qr_{Community.Protect(text)}_{pixels}_0", out uint uid)) return uid;

		if (text.StartsWith("http"))
		{
			PayloadGenerator.Url generator = new PayloadGenerator.Url(text);
			text = generator.ToString();
		}

		using (var qrGenerator = new QRCodeGenerator())
		using (var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
		using (var qrCode = new QRCode(qrCodeData))
		{
			var qrCodeImage = qrCode.GetGraphic(pixels, whiteMode ? Color.White : Color.Black, transparent ? Color.Transparent : whiteMode ? Color.Black : Color.White, quietZones);

			using var output = new MemoryStream();
			qrCodeImage.Save(output, ImageFormat.Png);
			qrCodeImage.Dispose();

			var raw = output.ToArray();
			uid = FileStorage.server.Store(raw, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));
			_protoData.Map.Add($"qr_{Community.Protect(text)}_{pixels}_0", uid);
			return uid;
		}
	}

	internal static string GetId(string url, float scale)
	{
		return $"{url}_{scale}";
	}

	public class QueuedThread : BaseThreadedJob, IDisposable
	{
		public List<string> ImageUrls { get; internal set; } = new();
		public float Scale { get; set; } = 1f;

		public List<QueuedThreadResult> Result { get; internal set; } = new();

		internal Queue<string> _urlQueue = new();
		internal int _processed;
		internal WebRequests _webRequests;
		internal WebRequests.WebRequest.Client _client;
		internal bool _finishedProcessing;
		internal bool _disposed;

		public override void Start()
		{
			_webRequests = new WebRequests();
			foreach (var url in ImageUrls) { _urlQueue.Enqueue(url); }

			base.Start();
		}
		public override void ThreadFunction()
		{
			base.ThreadFunction();

			_client = new WebRequests.WebRequest.Client();
			{
				_client.Headers.Add("User-Agent", Community.Runtime.Analytics.UserAgent);
				_client.Credentials = CredentialCache.DefaultCredentials;
				_client.Proxy = null;

				_client.DownloadDataCompleted += (_, e) =>
				{
					if (e.Error != null)
					{
						return;
					}

					_processed++;
					Result.Add(new QueuedThreadResult
					{
						Url = (string)e.UserState,
						Data = e.Result
					});

					_doQueue();
				};

				_doQueue();
			}

			while (_processed != ImageUrls.Count)
			{
			}

			_client.Dispose();
			_client = null;

			_processImages();

			while (!_finishedProcessing)
			{
				continue;
			}
		}
		public override void Dispose()
		{
			ImageUrls.Clear();
			Result.Clear();
			Scale = default;
			_urlQueue.Clear();
			_client?.Dispose();
			_webRequests = null;
			_finishedProcessing = default;
			_disposed = true;

			ImageUrls = null;
			Result = null;
			_urlQueue = null;

			base.Dispose();
		}

		public void DisposalSave()
		{
			_processImages();
		}

		internal void _doQueue()
		{
			if (_urlQueue.Count == 0) return;

			try
			{
				var pick = _urlQueue.Dequeue();
				_client.DownloadDataAsync(new Uri(pick), pick);
			}
			catch (Exception ex)
			{
				System.Console.WriteLine(ex);
				_processed++;
			}
		}
		internal void _processImages()
		{
			if (Scale == 1f)
			{
				_finishedProcessing = true;
				return;
			}

			var results = Facepunch.Pool.GetList<QueuedThreadResult>();
			results.AddRange(Result);

			foreach (var result in results)
			{
				try
				{
					using var stream = new MemoryStream(result.Data);
					using var image = Image.FromStream(stream);
					using var graphics = Graphics.FromImage(image);
					using var resized = new Bitmap((int)(image.Width * Scale), (int)(image.Height * Scale));
					resized.MakeTransparent();
					using var resizedGraphic = Graphics.FromImage(resized);
					resizedGraphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
					resizedGraphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
					resizedGraphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
					resizedGraphic.DrawImage(image, new Rectangle(0, 0, (int)(image.Width * Scale), (int)(image.Height * Scale)));
					using var output = new MemoryStream();
					resized.Save(output, ImageFormat.Png);
					resized.Dispose();
					result.Data = output.ToArray();
				}
				catch { }
			}

			Facepunch.Pool.FreeList(ref results);

			_finishedProcessing = true;
		}
	}
	public class QueuedThreadResult : IDisposable
	{
		public string Url { get; set; }
		public byte[] Data { get; set; }

		public void Dispose()
		{
			Data = null;
		}
	}
}

public class ImageDatabaseConfig
{
	public float TimeoutPerUrl { get; set; } = 2f;
	public bool InitializedBatchLogs { get; set; } = false;
	public bool CompletedBatchLogs { get; set; } = false;
	public bool RetrievedImageLogs { get; set; } = false;
	public bool DeletedImageLogs { get; set; } = false;
}

[ProtoContract]
public class ImageDatabaseDataProto
{
	[ProtoMember(1)]
	public ulong Identifier { get; set; }

	[ProtoMember(2)]
	public Dictionary<string, uint> Map { get; set; }

	[ProtoMember(3)]
	public Dictionary<string, string> CustomMap { get; set; }
}
