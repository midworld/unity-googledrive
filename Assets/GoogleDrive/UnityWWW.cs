using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace Midworld
{
	class UnityWWW : UnityCoroutine
	{
		const int TIMEOUT = 5000;

		public class Request
		{
			public string method = "GET";
			public string protocol = "HTTP/1.1";
			public Uri uri;
			public Hashtable headers = new Hashtable();
			public byte[] postData = null;

			public Request(string url)
			{
				this.uri = new Uri(url);
			}

			public Request(string url, string method) : this(url)
			{
				this.method = method;	
			}
		}

		public class Response
		{
			public string httpVersion;
			public int statusCode;
			public string reasonPhrase;
			public Dictionary<string, string> headers = new Dictionary<string,string>();
			public byte[] bytes = null;
			public String text { get { return Encoding.UTF8.GetString(bytes); } }
		}

		public Request request { get; protected set; }

		public Response response { get; protected set; }

		public Exception error { get; protected set; }

		public UnityWWW(string url) : this(new Request(url)) { }

		public UnityWWW(Request request)
		{
			this.request = request;

			ThreadPool.QueueUserWorkItem((arg) =>
			{
				try
				{
					TcpClient client = new TcpClient();
					client.Connect(request.uri.Host, request.uri.Port);

					Stream stream = client.GetStream();

					if (request.uri.Scheme == "https")
					{
						stream = new SslStream(stream, false,
							new RemoteCertificateValidationCallback((sender, cert, chain, error) => true));
						(stream as SslStream).AuthenticateAsClient(request.uri.Host);
					}

					/* write */ {
						BinaryWriter writer = new BinaryWriter(stream);
						writer.Write(Encoding.UTF8.GetBytes(request.method + " " +
							request.uri.PathAndQuery + " " + request.protocol + "\r\n"));

						request.headers["Host"] = request.uri.Host;
						foreach (DictionaryEntry item in request.headers)
						{
							if (item.Key is string && item.Value is string)
							{
								writer.Write(Encoding.UTF8.GetBytes(item.Key + ":" +
									item.Value + "\r\n"));
							}
							else if (item.Key is string && item.Value is string[])
							{
								writer.Write(Encoding.UTF8.GetBytes(item.Key + ":" +
									string.Join(",", item.Value as string[]) + "\r\n"));
							}
						}

						if (request.postData != null)
						{
							writer.Write(Encoding.UTF8.GetBytes("Content-Length:" +
								request.postData.Length + "\r\n\r\n"));
							writer.Write(request.postData);
						}
						else
						{
							writer.Write(Encoding.UTF8.GetBytes("\r\n"));
						}
					}

					/* read */
					{
						response = new Response();

						stream.ReadTimeout = TIMEOUT;

						BufferedStream bufferedStream = new BufferedStream(stream);

						/* read headers */
						{
							List<byte> line = new List<byte>(4096);
							List<string> lines = new List<string>();

							do
							{
								byte b = (byte)bufferedStream.ReadByte();

								if (b == (byte)'\r')
									continue;
								else if (b == (byte)'\n')
								{
									if (line.Count == 0)
										break;

									lines.Add(Encoding.UTF8.GetString(line.ToArray()));

									line.Clear();
									continue;
								}

								line.Add(b);
							} while (true);

							string[] statusLine = lines[0].Split(' ');
							response.httpVersion = statusLine[0];
							response.statusCode = int.Parse(statusLine[1]);
							response.reasonPhrase = statusLine[2];

							for (int i = 1; i < lines.Count; i++)
							{
								string k = lines[i].Substring(0, lines[i].IndexOf(':'));
								string v = lines[i].Substring(lines[i].IndexOf(':') + 1);

								response.headers.Add(k.Trim(), v.Trim());
							}
						}

						/* read body */ {
							int contentLength = 0;
							if (response.headers.ContainsKey("Content-Length"))
								contentLength = int.Parse(response.headers["Content-Length"]);

							response.bytes = new byte[contentLength];
							int bytesReceived = 0;

							while (bytesReceived < contentLength)
							{
								bytesReceived += bufferedStream.Read(response.bytes, 
									bytesReceived, response.bytes.Length - bytesReceived);
							}
						}
					}

					stream.Close();
					client.Close();
				}
				catch (Exception e)
				{
					error = e;
				}
				finally
				{
					isDone = true;
				}
			});
		}
	}
}
