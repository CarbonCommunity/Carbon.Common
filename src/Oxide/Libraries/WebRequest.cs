using System.Net;
using System.Text;
using Logger = Carbon.Logger;

namespace Oxide.Core.Libraries;

#pragma warning disable CS4014

public enum RequestMethod
{
	DELETE,
	GET,
	PATCH,
	POST,
	PUT
}

public class WebRequests : Library
{
	private readonly Queue<WebRequest> _queue = new();
	private readonly object _lockQueue = new();
	private readonly AutoResetEvent _workEvent = new(false);
	private readonly Thread _workerThread;
	private readonly int _minAvailableWorkerThreads;
	private readonly int _minAvailableCompletionPortThreads;
	private volatile bool _shutdown;

	public WebRequests()
	{
		ServicePointManager.Expect100Continue = false;
		ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, error) => true;
		ServicePointManager.DefaultConnectionLimit = 200;

		ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
		_minAvailableWorkerThreads = (int)(maxWorkerThreads * 0.75);
		_minAvailableCompletionPortThreads = (int)(maxCompletionPortThreads * 0.6);

		_workerThread = new Thread(Worker) { IsBackground = true, Name = "Carbon.WebRequests" };
		_workerThread.Start();
	}

	public override void Shutdown()
	{
		if (_shutdown)
		{
			return;
		}

		_shutdown = true;
		_workEvent.Set();
		_workerThread.Join(500);
	}

	private WebRequest QueueRequest(WebRequest request)
	{
		lock (_lockQueue)
		{
			_queue.Enqueue(request);
		}

		_workEvent.Set();
		return request;
	}

	private void Worker()
	{
		try
		{
			while (!_shutdown)
			{
				ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
				if (availableWorkerThreads <= _minAvailableWorkerThreads || availableCompletionPortThreads <= _minAvailableCompletionPortThreads)
				{
					Thread.Sleep(100);
					continue;
				}

				WebRequest request = null;

				lock (_lockQueue)
				{
					if (_queue.Count > 0)
					{
						request = _queue.Dequeue();
					}
				}

				if (request != null)
				{
					try
					{
						request.Start();
					}
					catch (Exception ex)
					{
						request.FailStart(ex);
					}

					continue;
				}

				_workEvent.WaitOne();
			}
		}
		catch (ThreadAbortException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
		catch (Exception ex)
		{
			if (!_shutdown)
			{
				Logger.Error("WebRequests worker crashed", ex);
			}
		}
	}

	public WebRequest Enqueue(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f, DecompressionMethods decompressionMethod = DecompressionMethods.None)
	{
		return QueueRequest(new WebRequest(url, callback, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body,
			DecompressionMethod = decompressionMethod
		});
	}
	public WebRequest EnqueueData(string url, string body, Action<int, byte[]> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f, DecompressionMethods decompressionMethod = DecompressionMethods.None)
	{
		return QueueRequest(new WebRequest(url, callback, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body,
			DecompressionMethod = decompressionMethod
		});
	}

	public async Task<WebRequest> EnqueueAsync(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f, DecompressionMethods decompressionMethod = DecompressionMethods.None)
	{
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		WebRequest request = default;

		request = new WebRequest(url, (code, data) =>
		{
			try
			{
				callback?.Invoke(code, data);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed executing '{request.Method}' async webrequest [callback] ({request.Url})", ex);
			}
		}, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body,
			DecompressionMethod = decompressionMethod
		};

		request.CompletionCallback = _ => tcs.TrySetResult(true);

		QueueRequest(request);

		await tcs.Task;

		return request;
	}
	public async Task<WebRequest> EnqueueDataAsync(string url, string body, Action<int, byte[]> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f, DecompressionMethods decompressionMethod = DecompressionMethods.None)
	{
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		WebRequest request = default;

		request = new WebRequest(url, (code, data) =>
		{
			try
			{
				callback?.Invoke(code, data);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed executing '{request.Method}' async webrequest [callback] ({request.Url})", ex);
			}
		}, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body,
			DecompressionMethod = decompressionMethod
		};

		request.CompletionCallback = _ => tcs.TrySetResult(true);

		QueueRequest(request);

		await tcs.Task;

		return request;
	}

	[Obsolete("EnqueueGet is deprecated, use Enqueue instead")]
	public void EnqueueGet(string url, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null, float timeout = 0f)
	{
		Enqueue(url, null, callback, owner, RequestMethod.GET, headers, timeout);
	}

	[Obsolete("EnqueuePost is deprecated, use Enqueue instead")]
	public void EnqueuePost(string url, string body, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null, float timeout = 0f)
	{
		Enqueue(url, body, callback, owner, RequestMethod.POST, headers, timeout);
	}

	[Obsolete("EnqueuePut is deprecated, use Enqueue instead")]
	public void EnqueuePut(string url, string body, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null, float timeout = 0f)
	{
		Enqueue(url, body, callback, owner, RequestMethod.PUT, headers, timeout);
	}

	public class WebRequest : IDisposable
	{
		public Action<int, string> Callback { get; set; }
		public Action<int, byte[]> DataCallback { get; set; }

		public float Timeout { get; set; }
		public string Method { get; set; }
		public string Url { get; }
		public string Body { get; set; }
		public DecompressionMethods DecompressionMethod { get; set; } = DecompressionMethods.GZip;

		public TimeSpan ResponseDuration { get; protected set; }
		public int ResponseCode { get; protected set; }
		public object ResponseObject { get; protected set; } = string.Empty;
		public Exception ResponseError { get; protected set; }

		public Plugin Owner { get; protected set; }
		public Dictionary<string, string> RequestHeaders { get; set; }

		internal DateTime _time;
		internal bool _data;
		internal Uri _uri;
		internal Client _client;
		internal int _disposed;
		internal int _started;
		internal int _completed;
		internal Action<WebRequest> CompletionCallback;

		public WebRequest(string url, Action<int, string> callback, Plugin owner)
		{
			Url = url;
			Callback = callback;
			Owner = owner;
			_uri = new Uri(url);
			_data = false;
		}
		public WebRequest(string url, Action<int, byte[]> callback, Plugin owner)
		{
			Url = url;
			DataCallback = callback;
			Owner = owner;
			_uri = new Uri(url);
			_data = true;
		}

		public WebRequest Start()
		{
			if (Volatile.Read(ref _disposed) == 1)
			{
				OnComplete();
				return this;
			}

			if (Interlocked.Exchange(ref _started, 1) == 1)
			{
				return this;
			}

			if (Owner != null && !Owner.IsLoaded)
			{
				ResponseError = new OperationCanceledException($"Owner plugin unloaded before '{Method}' webrequest start ({Url})");
				OnComplete();
				return this;
			}

			var client = new Client();
			_client = client;

			if (Volatile.Read(ref _disposed) == 1 || _uri == null)
			{
				client.Dispose();
				_client = null;
				OnComplete();
				return this;
			}

			client.Headers["User-Agent"] = Community.Runtime.Analytics.UserAgent;

			if (Method != "GET")
			{
				client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
			}

			client.Credentials = CredentialCache.DefaultCredentials;
			client.Proxy = null;
			client.Encoding = Encoding.UTF8;
			client.AutomaticDecompression = DecompressionMethod;

			if (RequestHeaders != null && RequestHeaders.Count > 0)
			{
				foreach (var header in RequestHeaders)
				{
					client.Headers[header.Key] = header.Value;
				}
			}

			switch (Method)
			{
				case "GET":
					_time = DateTime.Now;

					try
					{
						if (_data)
						{
							client.DownloadDataCompleted += (_, e) =>
							{
								ResponseDuration = DateTime.Now - _time;
								ResponseCode = client.StatusCode;

								try
								{
									if (e == null)
									{
										OnComplete();
										return;
									}

									if (e.Cancelled)
									{
										OnComplete();
										return;
									}

									if (e.Error != null)
									{
										ResponseError = e.Error;
										Logger.Error($"Failed executing '{Method}' webrequest [response] ({Url})", e.Error);
										OnComplete();
										return;
									}

									ResponseObject = e.Result;
									OnComplete();
								}
								catch (Exception ex)
								{
									Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);
									OnComplete();
								}
							};
							client.DownloadDataAsync(_uri);
						}
						else
						{
							client.DownloadStringCompleted += (_, e) =>
							{
								ResponseDuration = DateTime.Now - _time;
								ResponseCode = client.StatusCode;

								try
								{
									if (e == null)
									{
										OnComplete();
										return;
									}

									if (e.Cancelled)
									{
										OnComplete();
										return;
									}

									if (e.Error != null)
									{
										ResponseError = e.Error;
										Logger.Error($"Failed executing '{Method}' webrequest [response] ({Url})", e.Error);
										OnComplete();
										return;
									}

									ResponseObject = e.Result;
									OnComplete();
								}
								catch (Exception ex)
								{
									Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);
									OnComplete();
								}
							};
							client.DownloadStringAsync(_uri);
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);
						ResponseCode = client.StatusCode;
						ResponseError = ex;
						OnComplete();
					}

					break;

				case "PUT":
				case "PATCH":
				case "POST":
				case "DELETE":
					_time = DateTime.Now;

					try
					{
						if (_data)
						{
							client.UploadDataCompleted += (_, e) =>
							{
								ResponseDuration = DateTime.Now - _time;
								ResponseCode = client.StatusCode;

								try
								{
									if (e == null)
									{
										OnComplete();
										return;
									}

									if (e.Cancelled)
									{
										OnComplete();
										return;
									}

									if (e.Error != null)
									{
										ResponseError = e.Error;
										Logger.Error($"Failed executing '{Method}' webrequest [response] ({Url})", e.Error);
										OnComplete();
										return;
									}

									ResponseObject = e.Result;
									OnComplete();
								}
								catch (Exception ex)
								{
									Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);
									OnComplete();
								}
							};
							client.UploadDataAsync(_uri, Method, Encoding.UTF8.GetBytes(Body ?? string.Empty));
						}
						else
						{
							client.UploadStringCompleted += (_, e) =>
							{
								ResponseDuration = DateTime.Now - _time;
								ResponseCode = client.StatusCode;

								try
								{
									if (e == null)
									{
										OnComplete();
										return;
									}

									if (e.Cancelled)
									{
										OnComplete();
										return;
									}

									if (e.Error != null)
									{
										ResponseError = e.Error;
										Logger.Error($"Failed executing '{Method}' webrequest [response] ({Url})", e.Error);
										OnComplete();
										return;
									}

									ResponseObject = e.Result;
									OnComplete();
								}
								catch (Exception ex)
								{
									Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);
									OnComplete();
								}
							};
							client.UploadStringAsync(_uri, Method, string.IsNullOrEmpty(Body) ? string.Empty : Body);
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);
						ResponseCode = client.StatusCode;
						ResponseError = ex;
						OnComplete();
					}

					break;

				default:
					ResponseError = new NotSupportedException($"Unsupported webrequest method '{Method}'");
					OnComplete();
					break;
			}

			return this;
		}

		private void OnComplete()
		{
			if (Interlocked.Exchange(ref _completed, 1) == 1)
			{
				return;
			}

			try
			{
				CompletionCallback?.Invoke(this);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed running '{Method}' webrequest completion callback ({Url})", ex);
			}

			Interface.Oxide.NextTick(() =>
			{
				if (Owner != null && !Owner.IsLoaded)
				{
					Dispose();
					return;
				}

				var owner = Owner;
				owner?.TrackStart();

				var text = "Web request callback raised an exception";

				if (owner != null)
				{
					text += $" in '{owner.ToPrettyString()}' plugin";
				}

				try
				{
					if (_data)
					{
						DataCallback?.Invoke(ResponseCode, ResponseObject as byte[]);
					}
					else
					{
						Callback?.Invoke(ResponseCode, ResponseObject?.ToString());
					}
				}
				catch (Exception ex)
				{
					Logger.Error($"{text} [{ResponseCode}]", ex);
				}

				owner?.TrackEnd();
				Dispose();
			});
		}

		internal void FailStart(Exception ex)
		{
			ResponseError = ex;
			Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);
			OnComplete();
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 1)
			{
				return;
			}

			Owner = null;

			_uri = null;

			_client?.Dispose();
			_client = null;
		}

		public class Client : WebClient
		{
			public int StatusCode { get; private set; }
			public DecompressionMethods AutomaticDecompression { get; set; } = DecompressionMethods.GZip;

			public Client()
			{
				Encoding = Encoding.UTF8;
			}

			protected override WebResponse GetWebResponse(System.Net.WebRequest request, IAsyncResult result)
			{
				WebResponse response = null;

				try
				{
					response = base.GetWebResponse(request, result);

					if (response is HttpWebResponse httpResponse)
					{
						StatusCode = (int)httpResponse.StatusCode;
					}
				}
				catch (WebException exp)
				{
					response = exp.Response;

					if (response is HttpWebResponse httpResponse)
					{
						StatusCode = (int)httpResponse.StatusCode;
					}
				}

				return response;
			}

			protected override WebResponse GetWebResponse(System.Net.WebRequest request)
			{
				WebResponse response = null;

				try
				{
					response = base.GetWebResponse(request);

					if (response is HttpWebResponse httpResponse)
					{
						StatusCode = (int)httpResponse.StatusCode;
					}
				}
				catch (WebException exp)
				{
					response = exp.Response;

					if (response is HttpWebResponse httpResponse)
					{
						StatusCode = (int)httpResponse.StatusCode;
					}
				}

				return response;
			}

			protected override System.Net.WebRequest GetWebRequest(Uri address)
			{
				var request = base.GetWebRequest(address) as HttpWebRequest;

				request.UserAgent = Community.Runtime.Analytics.UserAgent;
				request.AutomaticDecompression = AutomaticDecompression;

				if (Community.IsConfigReady && !string.IsNullOrEmpty(Community.Runtime.Config.WebRequestIp))
				{
					request.ServicePoint.BindIPEndPointDelegate = (_, _, _) => new IPEndPoint(IPAddress.Parse(Community.Runtime.Config.WebRequestIp), 0);
				}

				return request;
			}

			public new void Dispose()
			{
				base.Dispose();
			}
		}
	}
}
