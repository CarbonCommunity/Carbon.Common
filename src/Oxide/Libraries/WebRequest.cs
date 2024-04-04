using System.Net;
using System.Text;
using Facepunch;
using Logger = Carbon.Logger;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

#pragma warning disable CS4014

namespace Oxide.Core.Libraries;

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
	public WebRequests()
	{
		ServicePointManager.Expect100Continue = false;
		ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, error) => true;
		ServicePointManager.DefaultConnectionLimit = 200;
	}

	public WebRequest Enqueue(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
	{
		return new WebRequest(url, callback, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body
		}.Start();
	}
	public WebRequest EnqueueData(string url, string body, Action<int, byte[]> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
	{
		return new WebRequest(url, callback, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body
		}.Start();
	}

	public async Task<WebRequest> EnqueueAsync(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
	{
		var tcs = new TaskCompletionSource<bool>();

		var request = new WebRequest(url, (code, data) =>
		{
			tcs.SetResult(true);
			callback?.Invoke(code, data);
		}, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body
		}.Start();

		await tcs.Task;

		return request;
	}
	public async Task<WebRequest> EnqueueDataAsync(string url, string body, Action<int, byte[]> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
	{
		var tcs = new TaskCompletionSource<bool>();

		var request = new WebRequest(url, (code, data) =>
		{
			tcs.SetResult(true);
			callback?.Invoke(code, data);
		}, owner)
		{
			Method = method.ToString(),
			RequestHeaders = headers,
			Timeout = timeout,
			Body = body
		}.Start();

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
		public Action<int, string> SuccessCallback { get; set; }
		public Action<int, byte[]> SuccessDataCallback { get; set; }

		public float Timeout { get; set; }
		public string Method { get; set; }
		public string Url { get; }
		public string Body { get; set; }

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

		public WebRequest(string url, Action<int, string> callback, Plugin owner)
		{
			Url = url;
			SuccessCallback = callback;
			Owner = owner;
			_uri = new Uri(url);
			_data = false;
		}
		public WebRequest(string url, Action<int, byte[]> callback, Plugin owner)
		{
			Url = url;
			SuccessDataCallback = callback;
			Owner = owner;
			_uri = new Uri(url);
			_data = true;
		}

		public WebRequest Start()
		{
			_client = new Client();
			_client.Headers.Add("User-Agent", Community.Runtime.Analytics.UserAgent);
			_client.Credentials = CredentialCache.DefaultCredentials;
			_client.Proxy = null;
			_client.Encoding = Encoding.UTF8;

			if (RequestHeaders != null && RequestHeaders.Count > 0)
			{
				foreach (var header in RequestHeaders)
				{
					_client.Headers[header.Key] = header.Value;
				}
			}

			switch (Method)
			{
				case "GET":
					if (_data)
					{
						_client.DownloadDataCompleted += (_, e) =>
						{
							ResponseDuration = DateTime.Now - _time;

							try
							{
								if (e == null)
								{
									OnComplete(true);
									return;
								}

								if (e.Error != null)
								{
									if (e.Error is WebException web)
										ResponseCode = (int)(web.Response as HttpWebResponse).StatusCode;
									ResponseError = e.Error;
									ResponseObject = e.Result;
									OnComplete(true);
									return;
								}

								ResponseCode = _client.StatusCode;
								ResponseObject = e.Result;
								OnComplete(false);
							}
							catch
							{
								OnComplete(true);
							}
						};
					}
					else
					{
						_client.DownloadStringCompleted += (_, e) =>
						{
							ResponseDuration = DateTime.Now - _time;
							ResponseCode = _client.StatusCode;

							try
							{
								if (e == null)
								{
									OnComplete(true);
									return;
								}

								if (e.Error != null)
								{
									if (e.Error is WebException web)
										ResponseCode = (int)(web.Response as HttpWebResponse).StatusCode;
									ResponseError = e.Error;
									ResponseObject = e.Result;
									OnComplete(true);
									return;
								}

								ResponseObject = e.Result;
								OnComplete(false);
							}
							catch
							{
								OnComplete(true);
							}
						};
					}

					try
					{
						_time = DateTime.Now;

						if (_data)
						{
							_client.DownloadDataAsync(_uri);
						}
						else
						{
							_client.DownloadStringAsync(_uri);
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);

						ResponseError = ex;
						OnComplete(true);
					}

					break;

				case "PUT":
				case "PATCH":
				case "POST":
				case "DELETE":
					if (_data)
					{
						_client.UploadDataCompleted += (object sender, UploadDataCompletedEventArgs e) =>
						{
							ResponseDuration = DateTime.Now - _time;
							ResponseCode = _client.StatusCode;

							try
							{
								if (e == null)
								{
									OnComplete(true);
									return;
								}

								if (e.Error != null)
								{
									if (e.Error is WebException web)
										ResponseCode = (int)(web.Response as HttpWebResponse).StatusCode;
									ResponseError = e.Error;
									ResponseObject = e.Result;
									OnComplete(true);
									return;
								}

								ResponseObject = e.Result;
								OnComplete(false);
							}
							catch
							{
								OnComplete(true);
							}
						};
					}
					else
					{
						_client.UploadStringCompleted += (_, e) =>
						{
							ResponseDuration = DateTime.Now - _time;
							ResponseCode = _client.StatusCode;

							try
							{
								if (e == null)
								{
									OnComplete(true);
									return;
								}

								if (e.Error != null)
								{
									if (e.Error is WebException web)
										ResponseCode = (int)(web.Response as HttpWebResponse).StatusCode;
									ResponseError = e.Error;
									ResponseObject = e.Result;
									OnComplete(true);
									return;
								}

								ResponseObject = e.Result;
								OnComplete(false);
							}
							catch
							{
								OnComplete(true);
							}
						};
					}

					try
					{
						_time = DateTime.Now;

						if (_data)
						{
							_client.UploadDataAsync(_uri, Method, Encoding.Default.GetBytes(Body));
						}
						else
						{
							_client.UploadStringAsync(_uri, Method, Body ?? string.Empty);
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed executing '{Method}' webrequest [internal] ({Url})", ex);

						ResponseCode = _client.StatusCode;
						ResponseError = ex;
						OnComplete(true);
					}

					break;
			}

			return this;
		}

		private void OnComplete(bool failure)
		{
			Owner?.TrackStart();

			var text = "Web request callback raised an exception";

			if (Owner && Owner != null)
			{
				text += $" in '{Owner.ToPrettyString()}' plugin";
			}

			try
			{
				if (_data)
				{
					SuccessDataCallback?.Invoke(ResponseCode, ResponseObject as byte[]);
				}
				else
				{
					SuccessCallback?.Invoke(ResponseCode, ResponseObject?.ToString());
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"{text} [{ResponseCode}]", ex);
			}

			Owner?.TrackEnd();
			Dispose();
		}
		public void Dispose()
		{
			Owner = null;

			_uri = null;

			_client?.Dispose();
			_client = null;
		}

		public class Client : WebClient
		{
			public int StatusCode { get; private set; }

			public Client()
			{
				Encoding = Encoding.UTF8;
			}

			protected override WebResponse GetWebResponse(System.Net.WebRequest request, IAsyncResult result)
			{
				var response = base.GetWebResponse(request, result);

				StatusCode = (int)(request.GetResponse() as HttpWebResponse).StatusCode;

				return response;
			}
			protected override WebResponse GetWebResponse(System.Net.WebRequest request)
			{
				var response = base.GetWebResponse(request);

				StatusCode = (int)(request.GetResponse() as HttpWebResponse).StatusCode;

				return response;
			}

			protected override System.Net.WebRequest GetWebRequest(Uri address)
			{
				if (!Community.IsConfigReady || string.IsNullOrEmpty(Community.Runtime.Config.WebRequestIp))
				{
					return base.GetWebRequest(address);
				}

				var request = base.GetWebRequest(address) as HttpWebRequest;

				request.AutomaticDecompression = DecompressionMethods.GZip;
				request.ServicePoint.BindIPEndPointDelegate = (servicePoint, remoteEndPoint, retryCount) =>
				{
					return new IPEndPoint(IPAddress.Parse(Community.Runtime.Config.WebRequestIp), 0);
				};

				return request;
			}

			public new void Dispose()
			{
				base.Dispose();
			}
		}
	}
}
