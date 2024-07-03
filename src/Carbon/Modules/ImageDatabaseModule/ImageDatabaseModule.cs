using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using Facepunch;
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

public partial class ImageDatabaseModule : CarbonModule<ImageDatabaseConfig, EmptyModuleData>
{
	public override string Name => "ImageDatabase";
	public override Type Type => typeof(ImageDatabaseModule);
	public override VersionNumber Version => new(1, 0, 0);
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
		["glow"] = "https://carbonmod.gg/assets/media/cui/glow.png",
		["gear"] = "https://carbonmod.gg/assets/media/cui/gear.png",
		["close"] = "https://carbonmod.gg/assets/media/cui/close.png",
		["fade"] = "https://carbonmod.gg/assets/media/cui/fade.png",
		["graph"] = "https://carbonmod.gg/assets/media/cui/graph.png",
		["maximize"] = "https://carbonmod.gg/assets/media/cui/maximize.png",
		["minimize"] = "https://carbonmod.gg/assets/media/cui/minimize.png"
	};

	internal IEnumerator _executeQueue(QueuedThread thread, Action<List<QueuedThreadResult>> onFinished)
	{
		if (thread == null)
		{
			yield break;
		}

		thread.Start();

		while (!thread.IsDone)
		{
			yield return null;
		}

		onFinished?.Invoke(thread.Result);

		thread.Dispose();
	}
	internal string _getProtoDataPath()
	{
		return Path.Combine(Defines.GetModulesFolder(), Name, "data.db");
	}

	internal const int MaximumBytes = 4104304;

	[ConsoleCommand("imagedb.loaddefaults")]
	[AuthLevel(2)]
	private void LoadDefaults(ConsoleSystem.Arg arg)
	{
		LoadDefaultImages();
		arg.ReplyWith($"Loading all default images.");
	}

	[ConsoleCommand("imagedb.deleteimage")]
	[AuthLevel(2)]
	private void DeleteImg(ConsoleSystem.Arg arg)
	{
		arg.ReplyWith(DeleteImage(arg.GetString(0))
			? $"Deleted image"
			: $"Couldn't delete image. Probably because it doesn't exist");
	}

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

		if (Validate())
		{
			Save();
		}

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
		Queue(_defaultImages.Where(x => !HasImage(x.Key))
			.ToDictionary(x => x.Key, x => x.Value));
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
			PutsWarn($"The server identifier has changed. Wiping old image database. [old {_protoData.Identifier}, new {CommunityEntity.ServerInstance.net.ID.Value}]");
			_protoData.CustomMap.Clear();
			_protoData.Map.Clear();
			_protoData.Identifier = CommunityEntity.ServerInstance.net.ID.Value;
			return true;
		}

		if (!HasImage("checkmark"))
		{
			_protoData.CustomMap.Clear();
			_protoData.Map.Clear();
			return true;
		}

		return false;
	}

	public void QueueBatch(bool @override, IEnumerable<string> urls)
	{
		if (urls == null || !urls.Any())
		{
			return;
		}

		var urlCount = urls.Count();

		QueueBatch(@override, results =>
		{
			foreach (var result in results.Where(result => result.Data != null && result.Data.Length != 0))
			{
				if (result.Data.Length >= MaximumBytes)
				{
					Puts($"Failed storing {urlCount:n0} jobs: {result.Data.Length} more or equal than {MaximumBytes}");
					continue;
				}

				var id = FileStorage.server.Store(result.Data, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));

				if (id != 0)
				{
					_protoData.Map[GetId(result.Url)] = id;
				}
			}
		}, urls);
	}
	public void QueueBatch(bool @override, Action<List<QueuedThreadResult>> onComplete, IEnumerable<string> urls)
	{
		if (urls == null || !urls.Any())
		{
			return;
		}

		var thread = new QueuedThread();
		var existent = Pool.GetList<QueuedThreadResult>();
		var urlCount = urls.Count();

		try
		{
			thread.ImageUrls.AddRange(urls);
			_queue.Add(thread);

			if (!@override)
			{
				foreach (var url in urls)
				{
					var image = GetImage(url);

					if (image == 0) continue;
					existent.Add(new QueuedThreadResult { CRC = image, Url = url, Success = true });
					thread.ImageUrls.Remove(url);
				}
			}
			else
			{
				foreach (var url in thread.ImageUrls)
				{
					DeleteAllImages(url);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Failed processing queue batch", ex);
		}

		Community.Runtime.Core.persistence.StartCoroutine(_executeQueue(thread, results =>
		{
			try
			{
				if (results != null)
				{
					foreach (var result in results)
					{
						if (result.Data.Length >= MaximumBytes)
						{
							Puts($"Failed storing {urlCount:n0} jobs: {result.Data.Length} more or equal than {MaximumBytes}");
							continue;
						}

						var id = FileStorage.server.Store(result.Data, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));

						if (id != 0)
						{
							_protoData.Map[GetId(result.Url)] = id;
						}
					}

					results.InsertRange(0, existent);

					onComplete?.Invoke(results);
				}

				Pool.FreeList(ref existent);
				_queue.Remove(thread);
			}
			catch (Exception ex)
			{
				PutsError($"Failed QueueBatch of {urls.Count():n0}", ex);
			}
		}));

		Community.Runtime.Core.timer.In(ConfigInstance.TimeoutPerUrl * urls.Count(), () =>
		{
			if (thread._disposed) return;

			try
			{
				thread.Result.InsertRange(0, existent);
				onComplete?.Invoke(thread.Result);

				thread.Dispose();
				_queue.Remove(thread);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed timeout process", ex);
			}
		});
	}

	public void Queue(bool @override, Dictionary<string, string> mappedUrls)
	{
		if (mappedUrls == null || mappedUrls.Count == 0)
		{
			return;
		}

		var urls = new List<string>(); // Required for thread consistency (previously Pool.GetList<>)

		foreach (var url in mappedUrls)
		{
			urls.Add(url.Value);
			AddMap(url.Key, url.Value);
		}

		QueueBatch(@override, urls);
	}
	public void Queue(bool @override, Action<List<QueuedThreadResult>> onComplete, Dictionary<string, string> mappedUrls)
	{
		if (mappedUrls == null || mappedUrls.Count == 0)
		{
			return;
		}

		var urls = new List<string>(); // Required for thread consistency (previously Pool.GetList<>)

		foreach (var url in mappedUrls)
		{
			urls.Add(url.Value);
			AddMap(url.Key, url.Value);
		}

		QueueBatch(@override, onComplete, urls);
	}
	public void Queue(Dictionary<string, string> mappedUrls)
	{
		if (mappedUrls == null || mappedUrls.Count == 0)
		{
			return;
		}

		Queue(false, mappedUrls);
	}
	public void Queue(Action<List<QueuedThreadResult>> onComplete, Dictionary<string, string> mappedUrls)
	{
		if (mappedUrls == null || mappedUrls.Count == 0)
		{
			return;
		}

		Queue(false, onComplete, mappedUrls);
	}

	public void AddImage(string keyOrUrl, byte[] imageData, FileStorage.Type type = FileStorage.Type.png)
	{
		_protoData.Map[keyOrUrl] = FileStorage.server.Store(imageData, type, RelationshipManager.ServerInstance.net.ID);
	}
	public void AddMap(string key, string url)
	{
		_protoData.CustomMap[key] = url;
	}
	public void RemoveMap(string key)
	{
		if (_protoData.CustomMap.ContainsKey(key)) _protoData.CustomMap.Remove(key);
	}

	public uint GetImage(string keyOrUrl)
	{
		if (string.IsNullOrEmpty(keyOrUrl))
		{
			return default;
		}

		if (_protoData.CustomMap.TryGetValue(keyOrUrl, out var realUrl))
		{
			keyOrUrl = realUrl;
		}

		var id = GetId(keyOrUrl);

		return !_protoData.Map.TryGetValue(id, out var uid) ? default : uid;
	}
	public string GetImageString(string keyOrUrl)
	{
		return GetImage(keyOrUrl).ToString();
	}
	public bool HasImage(string keyOrUrl)
	{
		return FileStorage.server.Get(GetImage(keyOrUrl), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID) != null;
	}
	public bool DeleteImage(string url)
	{
		var id = GetId(url);

		if (!_protoData.Map.TryGetValue(id, out var uid))
		{
			return false;
		}

		FileStorage.server.Remove(uid, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));
		_protoData.Map.Remove(id);
		return true;
	}
	public void DeleteAllImages(string url)
	{
		var temp = PoolEx.GetDictionary<string, uint>();

		foreach (var map in _protoData.Map)
		{
			temp.Add(map.Key, map.Value);
		}

		foreach (var map in temp.Where(x => x.Key.StartsWith(url)))
		{
			FileStorage.server.Remove(map.Value, FileStorage.Type.png, new NetworkableId(_protoData.Identifier));
			_protoData.Map.Remove(map.Key);
		}

		PoolEx.FreeDictionary(ref temp);
	}

	public uint GetQRCode(string text, int pixels = 20, bool transparent = false, bool quietZones = true, bool whiteMode = false)
	{
		if (_protoData.Map.TryGetValue($"qr_{Community.Protect(text)}_{pixels}_0", out uint uid))
		{
			return uid;
		}

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

	public static string GetId(string url)
	{
		return url;
	}

	public class QueuedThread : BaseThreadedJob
	{
		public List<string> ImageUrls { get; internal set; } = new();

		public List<QueuedThreadResult> Result { get; internal set; } = new();

		internal Queue<string> _urlQueue = new();
		internal int _processed;
		internal WebRequests.WebRequest.Client _client;
		internal bool _finishedProcessing;
		internal bool _disposed;

		public override void Start()
		{
			foreach (var url in ImageUrls)
			{
				_urlQueue.Enqueue(url);
			}

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
					_processed++;

					if (e.Error != null)
					{
						return;
					}

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

			while (!_finishedProcessing)
			{
			}
		}
		public override void Dispose()
		{
			ImageUrls.Clear();
			Result.Clear();
			_urlQueue.Clear();
			_client?.Dispose();
			_finishedProcessing = default;
			_disposed = true;

			ImageUrls = null;
			Result = null;
			_urlQueue = null;

			base.Dispose();
		}

		internal void _doQueue()
		{
			if (_urlQueue.Count == 0)
			{
				return;
			}

			var url = _urlQueue.Dequeue();

			try
			{
				_client.DownloadDataAsync(new Uri(url), url);
			}
			catch (Exception exception)
			{
				Logger.Error($"Failed enqueuing '{url}'", exception);
			}
		}
	}
	public class QueuedThreadResult : IDisposable
	{
		public string Url { get; set; }
		public byte[] Data { get; set; }
		public uint CRC { get; set; }
		public bool Success { get; set; }

		public void Dispose()
		{
			Url = null;
			Data = null;
			Success = default;
		}
	}
}

public class ImageDatabaseConfig
{
	public float TimeoutPerUrl { get; set; } = 2f;
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
