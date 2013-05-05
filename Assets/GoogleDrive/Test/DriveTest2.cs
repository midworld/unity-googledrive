using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;

class DriveTest2 : MonoBehaviour
{
	public Font font = null;
	public Transform cube = null;

	bool fontSet = false;

	// -------------------------------
	class A : IEnumerable, IEnumerator
	{
		IEnumerator main;

		public A()
		{
			main = Main();
		}

		public A(bool a)
		{
			main = Main2();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}

		public bool MoveNext()
		{
			//Debug.Log(this + " " + main);
			return main.MoveNext();
		}

		public void Reset()
		{
			main.Reset();
		}

		public object Current
		{
			get
			{
				return main.Current;
			}
		}

		IEnumerator Main()
		{
			int i = 0;
			while (true)
			{
				Debug.Log("" + (i++));
				yield return new WaitForSeconds(1);

				//IEnumerator a = new A(true);
				//while (a.MoveNext())
				//    yield return a.Current;
				foreach (var a in new A(true))
					yield return a;
			}
		}

		IEnumerator Main2()
		{
			int i = 0;
			while (i < 3)
			{
				Debug.LogWarning("" + (i++));
				yield return new WaitForSeconds(1);
			}
		}
	}

	IEnumerator X()
	{
		yield return "hello";
		yield return "wold";
		yield break;
		yield return "!";
	}

	IEnumerator Y()
	{
		IEnumerator x = X();
		while (x.MoveNext())
			Debug.Log("x: " + x.Current);
		Debug.Log("last: " + x.Current);
		yield return null;
	}

	struct AAA
	{
		public string aaa;
	}
	// -------------------------------

	void Start()
	{
		files = new List<GoogleDriveOld.Files.File>();

		//StartCoroutine(new A());

		//StartCoroutine(Y()); // last: world

		//GameObject o = new GameObject();
		//o.AddComponent<DriveTest3>();

		var a = new AAA();
		Debug.Log(a.aaa == null ? "null" : a.aaa);
	}

	bool tasking = false;

	// for editor
	Action<string> getCode = null;
	string codeText = "";

	IEnumerator Auth()
	{
		tasking = true;

#if UNITY_EDITOR || UNITY_IPHONE
		GoogleDriveOld.Auth.clientID =
			"897584417662-rnkgkl5tlpnsau7c4oc0g2jp08cpluom.apps.googleusercontent.com";
		GoogleDriveOld.Auth.clientSecret =
			"tGNLbYnrdRO2hdFmwJAo5Fbt";
		GoogleDriveOld.Auth.redirectURI =
			//"urn:ietf:wg:oauth:2.0:oob";
			"http://localhost:9270";
#elif UNITY_ANDROID
		//GoogleDrive.Auth.clientID =
		//    "897584417662-hs5soq7srr706129i8t8qq7b8cc7cgha.apps.googleusercontent.com";
		//// Android doesn't need the client secret.
		//GoogleDrive.Auth.redirectURI =
		//    "urn:ietf:wg:oauth:2.0:oob";
#endif

#if UNITY_EDITOR
		if (GoogleDriveOld.Auth.HasAccessToken())
		{
			if (GoogleDriveOld.Auth.IsTokenExpired())
			{
				if (GoogleDriveOld.Auth.CanRefreshToken())
				{
					yield return StartCoroutine(GoogleDriveOld.Auth.RefreshToken());
					yield return StartCoroutine(GoogleDriveOld.Auth.ValidateToken());
				}
			}
			else
			{
				yield return StartCoroutine(GoogleDriveOld.Auth.ValidateToken());
			}
		}

		if (!GoogleDriveOld.Auth.isAuthorized)
		{
			// Open authorization page.
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("IExplore.exe");
			startInfo.Arguments = @"https://accounts.google.com/o/oauth2/auth?" +
				@"scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.email+" +
				@"https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fdrive&" +
				@"redirect_uri=" + GoogleDriveOld.Auth.redirectURI + "&" +
				@"response_type=code&" +
				@"client_id=" + GoogleDriveOld.Auth.clientID;

			System.Diagnostics.Process browser = new System.Diagnostics.Process();
			browser.StartInfo = startInfo;
			browser.Start();

			//string url = @"https://accounts.google.com/o/oauth2/auth?" +
			//    @"scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.email+" +
			//    @"https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fdrive&" +
			//    @"redirect_uri=" + GoogleDriveOld.Auth.redirectURI + "&" +
			//    @"response_type=code&" +
			//    @"client_id=" + GoogleDriveOld.Auth.clientID;
			//System.Diagnostics.Process browser = System.Diagnostics.Process.Start(url);

			//try
			//{
			//    //browser.HasExited
			//}
			//catch (InvalidOperationException e)
			//{
			//}

			//string a = "";
			//while (!browser.HasExited)
			//{
			//    try
			//    {
			//        browser.Refresh();

			//        if (browser.MainWindowTitle != a)
			//        {
			//            a = browser.MainWindowTitle;
			//        }
			//    }
			//    catch (Exception e)
			//    {
			//        Debug.LogWarning(e);
			//    }

			//    //Debug.Log(a);
			//    yield return null;
			//}

			//while (!browser.MainWindowTitle.Contains("Success code="))
			//	yield return null;

			//string title = browser.MainWindowTitle;
			//string code = title.Substring(title.IndexOf("Success code=") + 13);

			AuthRedirectionServer server = new AuthRedirectionServer();
			server.StartServer(9270);

			while (!browser.HasExited)
			{
				if (server.AuthorizationCode != null)
				{
					browser.CloseMainWindow();
					browser.Close();
					break;
				}
				else
					yield return null;
			}

			server.StopServer();

			string code = server.AuthorizationCode;

			if (code == null)
			{
				Debug.LogError("auth failed.");
				yield break;
			}

			PlayerPrefs.SetString("GoogleDriveAuthCode", code);
			
			//// Wait for authorization code.
			//string code = null;

			//getCode = (copiedCode) =>
			//{
			//    code = copiedCode;

			//    PlayerPrefs.SetString("GoogleDriveAuthCode", code);

			//    getCode = null;
			//};

			//while (code == null)
			//    yield return null;

			yield return StartCoroutine(GoogleDriveOld.Auth.GetTokenByAuthorizationCode(code));
			yield return StartCoroutine(GoogleDriveOld.Auth.ValidateToken()); // get the email address
		}
#elif UNITY_ANDROID
		//GoogleDrive.Auth.apiKey = "AIzaSyAcvilb4ZVQjyhP-1_wJ52hJORjiKHsV9o";

		//yield return StartCoroutine(GoogleDrive.Auth.Authorize());
#endif

		if (GoogleDriveOld.Auth.isAuthorized)
		{
			Debug.Log("Authorization succeeded(" + GoogleDriveOld.Auth.selectedAccountName + ").");

			//yield return StartCoroutine(GoogleDrive.Auth.ValidateToken());
		}
		else
		{
			Debug.Log("Authorization failed.");
		}

		{
			int maxRepeat = 2;
			string nextPageToken = null;

			for (int i = 0; i < maxRepeat; i++)
			{
				var req = new Midworld.UnityWebRequest("https://www.googleapis.com/drive/v2/files?" +
					"maxResults=1&" +
					(nextPageToken != null ? "pageToken=" + nextPageToken : ""));
				req.headers["Authorization"] = "Bearer " + GoogleDriveOld.Auth.token;

				var res = req.GetResponse();

				yield return StartCoroutine(res);
				
				Debug.Log(res.text);

				JsonFx.Json.JsonReader reader = new JsonFx.Json.JsonReader(res.text);
				var json = reader.Deserialize<Dictionary<string, object>>();

				if (json.ContainsKey("nextPageToken"))
					nextPageToken = json["nextPageToken"] as string;
				else
					break;
			}
		}

		tasking = false;
	}

	List<GoogleDriveOld.Files.File> files = null;
	Dictionary<string, Texture2D> iconTable = new Dictionary<string, Texture2D>();

	void UpdateList(GoogleDriveOld.Files.List list)
	{
		files = list.items;

		for (int i = 0; i < files.Count; i++)
		{
			if (files[i].iconLink == null)
				continue;

			if (!iconTable.ContainsKey(files[i].iconLink))
			{
				iconTable.Add(files[i].iconLink, null);
				StartCoroutine(DownloadIcon(files[i].iconLink));
			}
		}
	}

	IEnumerator List(int maxResults = -1)
	{
		tasking = true;

		do
		{
			GoogleDriveOld.Files.List list = new GoogleDriveOld.Files.List(maxResults);

			if (maxResults == -1)
			{
				yield return StartCoroutine(list);
			}
			else
			{
				while (!list.isDone)
				{
					UpdateList(list);

					yield return null;
				}
			}

			if (list.error is GoogleDriveOld.AuthException)
			{
				yield return StartCoroutine(Auth());

				if (GoogleDriveOld.Auth.isAuthorized)
					continue;
			}
			else
			{
				UpdateList(list);
			}
		} while (false);

		tasking = false;
	}

	IEnumerator DownloadIcon(string link)
	{
		WWW www = new WWW(link);

		yield return www;

		if (www.error == null)
			iconTable[link] = www.texture;
		else
			Debug.LogError(www.error);
	}

	Texture2D thumbnail = null;

	IEnumerator DownloadImage(string url)
	{
		tasking = true;

		var request = new Midworld.UnityWebRequest(url);
		request.headers["Authorization"] = "Bearer " + GoogleDriveOld.Auth.token;

		var response = request.GetResponse();
		yield return StartCoroutine(response);

		if (response.error == null)
		{
			string headers = "";

			foreach (DictionaryEntry kv in response.headers)
			{
				headers += kv.Key + " : " + kv.Value + "\n";
			}

			Debug.Log(headers);

			thumbnail = new Texture2D(0, 0);
			thumbnail.LoadImage(response.bytes);
		}
		else
		{
			Debug.LogError(response.error);
		}

		tasking = false;
	}

	IEnumerator Revoke()
	{
		tasking = true;

		yield return StartCoroutine(GoogleDriveOld.Auth.RevokeToken());

		tasking = false;
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}

		if (cube != null)
		{
			cube.RotateAround(Vector3.up, Time.deltaTime);
		}
	}

	int currentPage = 0;
	const int ITEMS_PER_PAGE = 20;

	void OnGUI()
	{
		if (!fontSet)
		{
			fontSet = true;

			GUI.skin.label.font = font;
			GUI.skin.button.font = font;
		}

		if (tasking)
		{
			string waitString = "Wait a second";
			
			for (int i = 0; i < ((int)(Time.timeSinceLevelLoad * 2)) % 4; i++)
			{
				waitString += ".";
			}

			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect(0, 0, Screen.width, Screen.height), waitString);
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;

			GUI.enabled = false;
		}

		if (thumbnail != null || getCode != null)
		{
			GUI.enabled = false;
		}

		GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height - 100));
		{
			if (files != null)
			{
				if (GUI.Button(new Rect(10, 10, 40, 40), "<<"))
				{
					currentPage = Mathf.Max(0, currentPage - 1);
				}

				GUI.skin.label.alignment = TextAnchor.MiddleCenter;
				
				int totalPage = files.Count / ITEMS_PER_PAGE + (files.Count % ITEMS_PER_PAGE > 0 ? 1 : 0);
				if (currentPage >= totalPage)
					currentPage = Mathf.Max(0, totalPage - 1);

				GUI.Label(new Rect(10, 10, Screen.width - 20, 40), (currentPage + 1) + " / " + Mathf.Max(1, totalPage));
				
				GUI.skin.label.alignment = TextAnchor.MiddleLeft;

				if (GUI.Button(new Rect(Screen.width - 50, 10, 40, 40), ">>"))
				{
					currentPage = Mathf.Min(totalPage - 1, currentPage + 1);
				}

				GUI.skin.button.padding.left = 34;
				GUI.skin.button.alignment = TextAnchor.MiddleLeft;

				for (int i = 0; i < ITEMS_PER_PAGE; i++)
				{
					int index = currentPage * ITEMS_PER_PAGE + i;
					
					if (files.Count <= index)
						break;

					if (GUI.Button(new Rect(10, 60 + 1 + i * 32, Screen.width - 20, 30), files[index].title))
					{
						// ...

						if (files[index].thumbnailLink != null)
						{
							StartCoroutine(DownloadImage(files[index].thumbnailLink));
						}
					}
					
					if (files[index].iconLink != null &&
						iconTable.ContainsKey(files[index].iconLink) &&
						iconTable[files[index].iconLink] != null)
					{
						GUI.DrawTexture(new Rect(14, 60 + 4 + i * 32, 24, 24), iconTable[files[index].iconLink]);
					}
				}

				GUI.skin.button.padding.left = 0;
				GUI.skin.button.alignment = TextAnchor.MiddleCenter;
			}
		}
		GUI.EndGroup();

		GUI.BeginGroup(new Rect(0, Screen.height - 100, Screen.width, 100));
		{
			if (GUI.Button(new Rect(10, 10, 120, 80), "Auth"))
			{
				StartCoroutine(Auth());
			}

			if (GUI.Button(new Rect(140, 10, 120, 80), "List"))
			{
				StartCoroutine(List());
			}

			if (GUI.Button(new Rect(270, 10, 120, 80), "List (20)"))
			{
				StartCoroutine(List(20));
			}

			if (GUI.Button(new Rect(400, 10, 120, 80), "Revoke"))
			{
				StartCoroutine(Revoke());
			}
		}
		GUI.EndGroup();

		if (thumbnail != null)
		{
			GUI.enabled = true;

			if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), thumbnail))
			{
				thumbnail = null;
			}
		}

#if UNITY_EDITOR
		if (getCode != null)
		{
			GUI.enabled = true;

			GUI.Window(0, new Rect(10, (Screen.height - 160) / 2, Screen.width - 20, 160), (id) =>
			{
				GUI.Label(new Rect(10, 20, Screen.width - 40, 30), "Paste the code here:");

				codeText = GUI.TextField(new Rect(10, 60, Screen.width - 40, 30), codeText);

				if (GUI.Button(new Rect((Screen.width - 300) / 2, 100, 300, 50), "OK"))
				{
					getCode(codeText);
					codeText = "";
				}
			}, "Authorization Dialog");
		}
#endif
	}
}
