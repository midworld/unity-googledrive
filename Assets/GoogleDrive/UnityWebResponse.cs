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
using Ionic.Zlib;

namespace Midworld
{
	/// <summary>
	/// Get HTTP response(Internal)
	/// </summary>
	/// <example>
	/// <code>
	/// var request = new UnityWebRequest("https://homepage.com");
	/// request.method = "POST";
	/// request.headers["Content-Type"] = "application/json";
	/// request.body = Encoding.UTF8.GetBytes("{\"hello\":\"world\"}");
	/// 
	/// var response = new UnityWebResponse(request);
	/// while (!response.isDone)
	/// 	yield return null;
	/// 	
	/// if (response.error == null)
	///		print(response.text);
	/// </code>
	/// </example>
	class UnityWebResponse : UnityCoroutine
	{
		const int TIMEOUT = 5000;
		const int MAX_REDIRECTION = 5;

		public string httpVersion { get; protected set; }

		public int statusCode { get; protected set; }

		public string reasonPhrase { get; protected set; }

		public Hashtable headers { get; protected set; }

		public byte[] bytes { get; protected set; }

		private string cachedText = null;

		public String text 
		{ 
			get 
			{
 				if (cachedText == null)
					cachedText = Encoding.UTF8.GetString(bytes);
				
				return cachedText;
			}
		}

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
					bool newConnection = true;
					Stream stream = null;

					do
					{
						#region Get stream
						if (newConnection)
						{
							client.SendTimeout = TIMEOUT;
							client.ReceiveTimeout = TIMEOUT;

							stream = client.GetStream();

							if (uri.Scheme == "https")
							{
								stream = new SslStream(stream, false,
									new RemoteCertificateValidationCallback((sender, cert, chain, error) => true));
								(stream as SslStream).AuthenticateAsClient(uri.Host);
							}
						}
						#endregion

						#region Request
						{
							BinaryWriter writer = new BinaryWriter(stream);
							writer.Write(Encoding.UTF8.GetBytes(request.method + " " +
								uri.PathAndQuery + " " + request.protocol + "\r\n"));

							request.headers["Host"] = uri.Host;
							
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

							if (request.body != null)
							{
								writer.Write(Encoding.UTF8.GetBytes("Content-Length:" +
									request.body.Length + "\r\n\r\n"));
								writer.Write(request.body);
							}
							else
							{
								writer.Write(Encoding.UTF8.GetBytes("\r\n"));
							}
						}
						#endregion

						#region Response
						{
							BufferedStream bufferedStream = new BufferedStream(stream);

							#region Read headers
							{
								List<string> lines = new List<string>();

								do
								{
									string line = ReadLine(bufferedStream);

									if (line.Length == 0)
										break;

									lines.Add(line);
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
							#endregion

							//UnityEngine.Debug.Log(DumpHeaders() +
							//    "\r\n" +
							//    "----");

							#region Read body
							{
								int contentLength = -1;
								if (this.headers.ContainsKey("Content-Length"))
									contentLength = int.Parse(this.headers["Content-Length"] as string);

								string transferEncoding = null;
								if (this.headers.ContainsKey("Transfer-Encoding"))
									transferEncoding = (this.headers["Transfer-Encoding"] as string).ToLower();

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
								else if (transferEncoding == "chunked")
								{
									MemoryStream ms = new MemoryStream(4096);
									byte[] buffer = new byte[4096];
									
									do
									{
										string chunkSizeString = ReadLine(bufferedStream);
										
										if (chunkSizeString.Length == 0)
											break;

										int chunkSize = Convert.ToInt32(chunkSizeString, 16);

										if (chunkSize == 0)
											break;

										int bytesReceived = 0;

										while (bytesReceived < chunkSize)
										{
											int read = bufferedStream.Read(buffer, 0, 
												chunkSize - bytesReceived < buffer.Length ? 
												chunkSize - bytesReceived : buffer.Length);

											ms.Write(buffer, 0, read);
											bytesReceived += read;
										}

										bufferedStream.ReadByte(); // \r
										bufferedStream.ReadByte(); // \n
									} while (true);

									this.bytes = ms.ToArray();
									ms.Dispose();
								}
								else
								{
									MemoryStream ms = new MemoryStream(4096);
									byte[] buffer = new byte[4096];
									int read = 0;

									do
									{
										read = bufferedStream.Read(buffer, 0, buffer.Length);
										ms.Write(buffer, 0, read);
									} while (read > 0);

									this.bytes = ms.ToArray();
									ms.Dispose();
								}

								cachedText = null;
							}
							#endregion

							#region Redirection
							if ((this.statusCode == 301 ||
								this.statusCode == 302 ||
								this.statusCode == 303 ||
								this.statusCode == 307) &&
								this.headers.ContainsKey("Location") &&
								redirection < MAX_REDIRECTION)
							{
								string oldHost = uri.Host;

								string location = this.headers["Location"] as string;
								uri = new Uri(uri, location);

								if (oldHost != uri.Host)
								{
									stream.Close();
									client.Close();

									client = new TcpClient();
									client.Connect(uri.Host, uri.Port);

									newConnection = true;
								}
								else
									newConnection = false;

								redirection++;
								continue;
							}
							#endregion

							#region Decoding
							if (this.headers.ContainsKey("Content-Encoding"))
							{
								bytes = Decompress((this.headers["Content-Encoding"] as string).ToLower(), bytes);
							}
							#endregion

#if UNITY_EDITOR
							// test---
							UnityEngine.Debug.LogWarning(request.DumpHeaders() +
								(request.body == null ? "" : "Content-Length: " + request.body.Length + "\r\n") +
								"\r\n" +
								(request.body == null ? "" : Encoding.UTF8.GetString(request.body)));
							UnityEngine.Debug.LogWarning(DumpHeaders() +
								"\r\n" +
								this.text);
#endif
						}
						#endregion

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

		string ReadLine(BufferedStream stream)
		{
			List<byte> line = new List<byte>(4096);

			do
			{
				int i = stream.ReadByte();

				if (i == -1)
					break; 

				byte b = (byte)i;

				if (b == (byte)'\r')
					continue;
				else if (b == (byte)'\n')
					break;

				line.Add(b);
			} while (true);

			return Encoding.UTF8.GetString(line.ToArray());
		}

		static byte[] Decompress(string encoding, byte[] data)
		{
			Stream stream;

			if (encoding == "gzip")
				stream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress);
			else if (encoding == "deflate")
				stream = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress);
			else
				return null;

			MemoryStream output = new MemoryStream(data.Length);

			byte[] buffer = new byte[4096];
			int read;

			while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, read);
			}

			return output.ToArray();
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
