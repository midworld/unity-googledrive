using System;
using System.Collections;

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
		}

		public UnityWebResponse GetResponse()
		{
			return new UnityWebResponse(this);
		}
	}
}
