using System;
using System.Collections;
using System.Text;

namespace Midworld
{
	class UnityWebRequest
	{
		public string method { get; set; }

		public string protocol { get; set; }

		public Uri uri { get; protected set; }

		public Hashtable headers { get; protected set; }

		public byte[] postData = null;

		public UnityWebRequest(string uri) : this(new Uri(uri)) { }

		public UnityWebRequest(Uri uri)
		{
			this.method = "GET";
			this.protocol = "HTTP/1.1";
			this.uri = uri;

			headers = new Hashtable();

			this.headers["Host"] = uri.Host;
			//this.headers["Connection"] = "Close";
			this.headers["Connection"] = "Keep-Alive";
			this.headers["Accept-Charset"] = "utf-8";
			this.headers["User-Agent"] = "Mozilla/5.0 (Unity3d)";
		}

		public UnityWebResponse GetResponse()
		{
			return GetResponse(null);
		}

		public UnityWebResponse GetResponse(Action<UnityWebResponse> callback)
		{
			UnityWebResponse response = new UnityWebResponse(this);
			response.done = (coroutine) =>
			{
				callback(coroutine as UnityWebResponse);
			};

			return response;
		}

		public string DumpHeaders()
		{
			if (this.headers == null)
				return "";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine(string.Format("{0} {1} {2}",
				this.method, uri.PathAndQuery, this.protocol));

			foreach (DictionaryEntry kv in this.headers)
			{
				if (kv.Value is string)
				{
					sb.AppendLine(string.Format("{0}: {1}",
						kv.Key, kv.Value));
				}
				else
				{
					for (int i = 0; i < (kv.Value as string[]).Length; i++)
					{
						sb.AppendLine(string.Format("{0}: {1}",
							kv.Key, (kv.Value as string[])[i]));
					}
				}
			}

			return sb.ToString();
		}
	}
}
