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
		const int MAX_REDIRECTION = 5;

		public string httpVersion { get; protected set; }

		public int statusCode { get; protected set; }

		public string reasonPhrase { get; protected set; }

		public Hashtable headers { get; protected set; }

		public byte[] bytes { get; protected set; }

		public String text { get { return Encoding.UTF8.GetString(bytes); } }

		public Exception error { get; protected set; }

		public UnityWebResponse(UnityWebRequest request)
		{
			ThreadPool.QueueUserWorkItem((arg) =>
			{
				try
				{
					int redirection = 0;

					TcpClient client = new TcpClient();
					Uri uri = request.uri;
					client.Connect(uri.Host, uri.Port);

					do
					{
						client.SendTimeout = TIMEOUT;
						client.ReceiveTimeout = TIMEOUT;
						
						Stream stream = client.GetStream();
						
						if (uri.Scheme == "https")
						{
							stream = new SslStream(stream, false,
								new RemoteCertificateValidationCallback((sender, cert, chain, error) => true));
							(stream as SslStream).AuthenticateAsClient(uri.Host);
						}

						/* request */
						{
							BinaryWriter writer = new BinaryWriter(stream);
							writer.Write(Encoding.UTF8.GetBytes(request.method + " " +
								uri.PathAndQuery + " " + request.protocol + "\r\n"));

							request.headers["Host"] = uri.Host;
							request.headers["Connection"] = "Close";
							if (!request.headers.ContainsKey("Accept-Charset"))
								request.headers["Accept-Charset"] = "utf-8";
							if (!request.headers.ContainsKey("User-Agent"))
								request.headers["User-Agent"] = "Mozilla/5.0 (Unity3d)";

							foreach (DictionaryEntry kv in request.headers)
							{
								if (kv.Key is string && kv.Value is string)
								{
									writer.Write(Encoding.UTF8.GetBytes(kv.Key + ": " +
											kv.Value + "\r\n"));
								}
								else if (kv.Key is string && kv.Value is string[])
								{
									for (int i = 0; i < (kv.Value as string[]).Length; i++)
									{
										writer.Write(Encoding.UTF8.GetBytes(kv.Key + ": " +
												(kv.Value as string[])[i] + "\r\n"));
									}
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

						/* response */
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
								this.reasonPhrase = string.Join(" ", statusLine, 2, statusLine.Length - 2);

								this.headers = new Hashtable();

								for (int i = 1; i < lines.Count; i++)
								{
									string k = lines[i].Substring(0, lines[i].IndexOf(':')).Trim();
									string v = lines[i].Substring(lines[i].IndexOf(':') + 1).Trim();

									if (!this.headers.ContainsKey(k))
									{
										this.headers.Add(k, v);
									}
									else
									{
										if (this.headers[k] is string)
										{
											string a = this.headers[k] as string;
											string[] b = { a, v };

											this.headers[k] = b;
										}
										else if (this.headers[k] is string[])
										{
											string[] a = this.headers[k] as string[];
											string[] b = new string[a.Length + 1];
											
											a.CopyTo(b, 0);
											b[a.Length - 1] = v;

											this.headers[k] = b;
										}
									}
								}
							}

							// test---
							UnityEngine.Debug.LogWarning(DumpHeaders());

							/* redirection */
							if ((this.statusCode == 301 ||
								this.statusCode == 302 ||
								this.statusCode == 303 ||
								this.statusCode == 307) &&
								this.headers.ContainsKey("Location") &&
								redirection < MAX_REDIRECTION)
							{
								string oldHost = uri.Host;
								uri = new Uri(this.headers["Location"] as string);

								if (oldHost != uri.Host)
								{
									stream.Close();
									client.Close();

									client = new TcpClient();
									client.Connect(uri.Host, uri.Port);
								}

								redirection++;
								continue;
							}

							/* read body */
							{
								int contentLength = -1;
								//if (this.headers.ContainsKey("Content-Length"))
								//	contentLength = int.Parse(this.headers["Content-Length"] as string);

								if (contentLength >= 0)
								{
									this.bytes = new byte[contentLength];
									int bytesReceived = 0;

									while (bytesReceived < contentLength)
									{
										bytesReceived += bufferedStream.Read(this.bytes,
											bytesReceived, this.bytes.Length - bytesReceived);
									}
								}
								else
								{
									MemoryStream ms = new MemoryStream(4096);
									byte[] buffer = new byte[4096];
									int bytesReceived;

									while ((bytesReceived = bufferedStream.Read(buffer, 0, buffer.Length)) > 0)
									{
										ms.Write(buffer, 0, bytesReceived);
									}

									this.bytes = ms.ToArray();
								}
							}
						}

						stream.Close();
						client.Close();
						break;
					} while (redirection < MAX_REDIRECTION);
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

		public string DumpHeaders()
		{
			if (this.headers == null)
				return "";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine(string.Format("{0} {1} {2}",
				this.httpVersion, this.statusCode, this.reasonPhrase));

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
