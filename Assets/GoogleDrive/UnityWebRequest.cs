using System;
using System.Collections.Generic;

namespace Midworld
{
	class UnityWebRequest
	{
		[System.ComponentModel.DefaultValue("GET")]
		public string method { get; set; }

		[System.ComponentModel.DefaultValue("HTTP/1.1")]
		public string protocol { get; set; }

		public Uri uri { get; protected set; }

		public Dictionary<string, string> headers { get; protected set; }

		public byte[] postData = null;

		public UnityWebRequest(string uri) : this(new Uri(uri)) { }

		public UnityWebRequest(Uri uri)
		{
			this.uri = uri;

			headers = new Dictionary<string, string>();
		}

		public UnityWebResponse GetResponse()
		{
			return new UnityWebResponse(this);
		}
	}
}
