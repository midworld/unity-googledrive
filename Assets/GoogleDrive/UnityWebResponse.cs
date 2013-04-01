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

namespace Midworld
{
	class UnityWebResponse : UnityCoroutine
	{
		const int TIMEOUT = 5000;

		public string httpVersion { get; protected set; }

		public int statusCode { get; protected set; }

		public string reasonPhrase { get; protected set; }

		public Dictionary<string, string> headers { get; protected set; }

		public byte[] bytes { get; protected set; }

		public String text { get { return Encoding.UTF8.GetString(bytes); } }

		public Exception error { get; protected set; }

		public UnityWebResponse(UnityWebRequest request)
		{
			ThreadPool.QueueUserWorkItem((arg) =>
			{
				try
				{
					TcpClient client = new TcpClient();
					client.Connect(request.uri.Host, request.uri.Port);

					Stream stream = client.GetStream();
					stream.ReadTimeout = TIMEOUT;

					if (request.uri.Scheme == "https")
					{
						stream = new SslStream(stream, false,
							new RemoteCertificateValidationCallback((sender, cert, chain, error) => true));
						(stream as SslStream).AuthenticateAsClient(request.uri.Host);
					}

					/* write */
					{
						BinaryWriter writer = new BinaryWriter(stream);
						writer.Write(Encoding.UTF8.GetBytes(request.method + " " +
							request.uri.PathAndQuery + " " + request.protocol + "\r\n"));

						request.headers["Host"] = request.uri.Host;
						foreach (KeyValuePair<string, string> kv in request.headers)
						{
							writer.Write(Encoding.UTF8.GetBytes(kv.Key + ":" +
									kv.Value + "\r\n"));
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
							this.httpVersion = statusLine[0];
							this.statusCode = int.Parse(statusLine[1]);
							this.reasonPhrase = statusLine[2];

							this.headers = new Dictionary<string, string>();

							for (int i = 1; i < lines.Count; i++)
							{
								string k = lines[i].Substring(0, lines[i].IndexOf(':'));
								string v = lines[i].Substring(lines[i].IndexOf(':') + 1);

								this.headers.Add(k.Trim(), v.Trim());
							}
						}

						/* read body */
						{
							int contentLength = 0;
							if (this.headers.ContainsKey("Content-Length"))
								contentLength = int.Parse(this.headers["Content-Length"]);

							this.bytes = new byte[contentLength];
							int bytesReceived = 0;

							while (bytesReceived < contentLength)
							{
								bytesReceived += bufferedStream.Read(this.bytes,
									bytesReceived, this.bytes.Length - bytesReceived);
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
