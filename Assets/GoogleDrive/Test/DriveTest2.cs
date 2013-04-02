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

	void Start()
	{
		files = new List<GoogleDrive.Files.File>();
	}

	bool tasking = false;

#if UNITY_EDITOR
	Action<string> getCode = null;
	string codeText = "";
#endif

	IEnumerator Auth()
	{
		tasking = true;

#if UNITY_EDITOR || UNITY_IPHONE
		GoogleDrive.Auth.clientID =
			"897584417662-rnkgkl5tlpnsau7c4oc0g2jp08cpluom.apps.googleusercontent.com";
		GoogleDrive.Auth.clientSecret =
			"tGNLbYnrdRO2hdFmwJAo5Fbt";
		GoogleDrive.Auth.redirectURI =
			"urn:ietf:wg:oauth:2.0:oob";
#elif UNITY_ANDROID
		GoogleDrive.Auth.clientID =
			"897584417662-hs5soq7srr706129i8t8qq7b8cc7cgha.apps.googleusercontent.com";
		// Android doesn't need the client secret.
		GoogleDrive.Auth.redirectURI =
			"urn:ietf:wg:oauth:2.0:oob";
#endif

#if UNITY_EDITOR
		if (GoogleDrive.Auth.HasAccessToken())
		{
			if (GoogleDrive.Auth.IsTokenExpired())
			{
				if (GoogleDrive.Auth.CanRefreshToken())
				{
					yield return StartCoroutine(GoogleDrive.Auth.RefreshToken());
				}
			}
			else
			{
				yield return StartCoroutine(GoogleDrive.Auth.ValidateToken());
			}
		}

		if (!GoogleDrive.Auth.isAuthorized)
		{
			// Open authorization page.
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("IExplore.exe");
			startInfo.Arguments = @"https://accounts.google.com/o/oauth2/auth?" +
				@"scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fdrive&" +
				@"redirect_uri=" + GoogleDrive.Auth.redirectURI + "&" +
				@"response_type=code&" +
				@"client_id=" + GoogleDrive.Auth.clientID;

			System.Diagnostics.Process browser = new System.Diagnostics.Process();
			browser.StartInfo = startInfo;
			browser.Start();

			// Wait for authorization code.
			string code = null;

			getCode = (copiedCode) =>
			{
				code = copiedCode;

				PlayerPrefs.SetString("GoogleDriveAuthCode", code);

				getCode = null;
			};

			while (code == null)
				yield return null;

			yield return StartCoroutine(GoogleDrive.Auth.GetTokenByAuthorizationCode(code));
		}
#elif UNITY_ANDROID
		GoogleDrive.Auth.apiKey = "AIzaSyAcvilb4ZVQjyhP-1_wJ52hJORjiKHsV9o";

		yield return StartCoroutine(GoogleDrive.Auth.Authorize());
#endif

		if (GoogleDrive.Auth.isAuthorized)
		{
			Debug.Log("Authorization succeeded(" + GoogleDrive.Auth.selectedAccountName + ").");
		}
		else
		{
			Debug.Log("Authorization failed.");
		}

		tasking = false;
	}

	List<GoogleDrive.Files.File> files = null;
	Dictionary<string, Texture2D> iconTable = new Dictionary<string, Texture2D>();

	void UpdateList(GoogleDrive.Files.List list)
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
			GoogleDrive.Files.List list = new GoogleDrive.Files.List(maxResults);

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

			if (list.error is GoogleDrive.AuthException)
			{
				yield return StartCoroutine(Auth());

				if (GoogleDrive.Auth.isAuthorized)
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

		/*WWW www = new WWW(url);

		yield return www;

		if (www.error == null)
			thumbnail = www.texture;
		else
			Debug.LogError(www.error);*/

		Midworld.UnityWWW.Request request = new Midworld.UnityWWW.Request(url);
		request.headers.Add("Authorization", "Bearer " + GoogleDrive.Auth.token);

		Midworld.UnityWWW www = new Midworld.UnityWWW(request);
		yield return StartCoroutine(www);

		if (www.error == null)
		{
			string headers = "";

			foreach (KeyValuePair<string, string> kv in www.response.headers)
			{
				headers += kv.Key + " : " + kv.Value + "\n";
			}

			Debug.Log(headers);

			thumbnail = new Texture2D(0, 0);
			thumbnail.LoadImage(www.response.bytes);
		}
		else
		{
			Debug.LogError(www.error);
		}

		/*HTTP.Request req = new HTTP.Request("GET", url);
		req.AddHeader("Authorization", "Bearer " + GoogleDrive.Auth.token);
		req.Send();
		while (!req.isDone && req.exception == null)
		{
			yield return null;
		}

		try
		{
			if (req.exception == null)
			{
				HTTP.Response res = req.response;

				thumbnail = new Texture2D(0, 0);
				thumbnail.LoadImage(res.bytes);
			}
		}
		catch (Exception e)
		{
			Debug.LogError(e);
		}*/

		tasking = false;
	}

	IEnumerator Revoke()
	{
		tasking = true;

		yield return StartCoroutine(GoogleDrive.Auth.RevokeToken());

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
