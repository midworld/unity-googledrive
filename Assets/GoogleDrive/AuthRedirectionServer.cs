using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Text;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IPHONE || UNITY_FLASH)
/// <summary>
/// Listen server for Google authorization result(Internal)
/// </summary>
class AuthRedirectionServer
{
	string authorizationCode = null;

	/// <summary>
	/// Authorization code.
	/// </summary>
	/// <remarks>This value is null until authorization finished.</remarks>
	public string AuthorizationCode
	{
		get
		{
			return authorizationCode;
		}
		private set
		{
			authorizationCode = value;
		}
	}

	/// <summary>
	/// HTTP server.
	/// </summary>
	TcpListener server = null;

	/// <summary>
	/// Listhen thread.
	/// </summary>
	Thread listenThread = null;

	/// <summary>
	/// Start a HTTP server.
	/// </summary>
	/// <param name="port">Port number.</param>
	public bool StartServer(int port)
	{
		try
		{
			Debug.Log("Starting a server...");
			server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
			server.Start();

			listenThread = new Thread(Listen);
			listenThread.Start();

			Debug.Log("The server is started.");
		}
		catch (Exception e)
		{
			Debug.LogError(e);

			if (server != null)
				server.Stop();

			server = null;

			return false;
		}

		return true;
	}

	/// <summary>
	/// Listen thread.
	/// </summary>
	void Listen()
	{
		while (AuthorizationCode == null)
		{
			try
			{
				TcpClient client = server.AcceptTcpClient();
#if UNITY_EDITOR
				Debug.Log("Connected!");
#endif
				NetworkStream stream = client.GetStream();
				stream.ReadTimeout = 2000;

				//Thread.Sleep(100);

				//stream.WriteByte(0);
				//stream.Flush();

				MemoryStream ms = new MemoryStream();
				byte[] bytes = new byte[4096];
				int readBytes;

				while ((readBytes = stream.Read(bytes, 0, bytes.Length)) > 0)
				{
					ms.Write(bytes, 0, readBytes);
					ms.Flush();

					string s = Encoding.UTF8.GetString(ms.ToArray());
					if (s.Contains("\r\n\r\n"))
					{
#if UNITY_EDITOR
						Debug.Log(s);
#endif
						// get auth code
						string code = s.Substring(0, s.IndexOf("\r\n")).Split(' ')[1];
						code = code.Substring(code.IndexOf("/?code=") + 7);

						AuthorizationCode = code;

						// response "close this window"
						byte[] body = Encoding.UTF8.GetBytes(
							"<html><head></head><body>Please close this window.</body></html>\r\n");
						byte[] header = Encoding.UTF8.GetBytes(
							"HTTP/1.1 200 OK\r\n" +
							"Connection: Close\r\n" +
							"Content-Type: text/html\r\n" +
							"Content-Length: " + body.Length + "\r\n" +
							"\r\n\r\n");

						stream.Write(header, 0, header.Length);
						stream.Write(body, 0, body.Length);

						break;
					}
				}

				client.Close();
				ms.Dispose();
			}
			catch (Exception e)
			{
#if UNITY_EDITOR
				Debug.LogWarning(e);
#endif
			}
		}
	}

	/// <summary>
	/// Stop the server.
	/// </summary>
	public void StopServer()
	{
		try
		{
			Debug.Log("Stopping the server...");
			server.Stop();

			listenThread.Abort();

			Debug.Log("The server is stopped.");
		}
		catch (Exception e)
		{
			Debug.LogError(e);
		}
		finally
		{
			server = null;
			listenThread = null;
		}
	}
}
#endif
