using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DriveTest : MonoBehaviour
{
	public Transform cube = null;

	GoogleDrive drive;

	void Start()
	{
		StartCoroutine(InitGoogleDrive());

		#region ESCAPE
		//string a = @"title contains '한글'";
		//byte[] aa = System.Text.Encoding.UTF8.GetBytes(a);
		//string b = "";
		//for (int i = 0; i < aa.Length; i++)
		//{
		//    char c = (char)aa[i];

		//    if ('a' <= c && c <= 'z' ||
		//        'A' <= c && c <= 'Z' ||
		//        '0' <= c && c <= '9')
		//    {
		//        b += c;
		//    }
		//    else if (c == ' ')
		//    {
		//        b += '+';
		//    }
		//    else
		//    {
		//        b += "%" + aa[i].ToString("x2");
		//    }
		//}
		//Debug.Log(b);
		#endregion
	}

	void Update()
	{
		if (cube != null)
			cube.RotateAround(Vector3.up, Time.deltaTime);

		if (Input.GetKey(KeyCode.Escape))
			Application.Quit();
	}

	bool initInProgress = false;

	IEnumerator InitGoogleDrive()
	{
		initInProgress = true;

		drive = new GoogleDrive();
		drive.ClientID = "897584417662-rnkgkl5tlpnsau7c4oc0g2jp08cpluom.apps.googleusercontent.com";
		drive.ClientSecret = "tGNLbYnrdRO2hdFmwJAo5Fbt";

		var authorization = drive.Authorize();
		yield return StartCoroutine(authorization);

		if (authorization.Current is Exception)
		{
			Debug.LogWarning(authorization.Current as Exception);
			goto finish;
		}
		else
			Debug.Log("User Account: " + drive.UserAccount);

		// Get all files in AppData folder and view text file.
		{
			var listFiles = drive.ListFiles(drive.AppData);
			yield return StartCoroutine(listFiles);
			var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);

			if (files != null)
			{
				foreach (var file in files)
				{
					Debug.Log(file);

					if (file.Title.EndsWith(".txt"))
					{
						var download = drive.DownloadFile(file);
						yield return StartCoroutine(download);
						
						var data = GoogleDrive.GetResult<byte[]>(download);
						Debug.Log(System.Text.Encoding.UTF8.GetString(data));
					}
				}
			}
			else
			{
				Debug.LogError(listFiles.Current);
			}
		}

	finish:
		initInProgress = false;
	}

	bool revokeInProgress = false;

	IEnumerator Revoke()
	{
		revokeInProgress = true;

		yield return StartCoroutine(drive.Unauthorize());

		revokeInProgress = false;
	}

	void OnGUI()
	{
		if (initInProgress)
		{
			GUI.enabled = false;
			GUI.Button(new Rect(10, 10, 200, 90), "Init");
			GUI.enabled = true;
		}
		else if (GUI.Button(new Rect(10, 10, 200, 90), "Init"))
		{
			StartCoroutine(InitGoogleDrive());
		}

		if (drive == null || revokeInProgress || !drive.IsAuthorized)
		{
			GUI.enabled = false;
			GUI.Button(new Rect(220, 10, 200, 90), "Revoke");
			GUI.enabled = true;
		}
		else if (GUI.Button(new Rect(220, 10, 200, 90), "Revoke"))
		{
			StartCoroutine(Revoke());
		}

		if (drive == null || uploadScreenshotInProgress || !drive.IsAuthorized)
		{
			GUI.enabled = false;
			GUI.Button(new Rect(10, 110, 200, 90), "Upload Screenshot");
			GUI.enabled = true;
		}
		else if (GUI.Button(new Rect(10, 110, 200, 90), "Upload Screenshot"))
		{
			StartCoroutine(UploadScreenshot());
		}

		if (drive == null || uploadTextInProgress || !drive.IsAuthorized)
		{
			GUI.enabled = false;
			GUI.Button(new Rect(220, 110, 200, 90), "Update 'my_text.txt'");
			GUI.enabled = true;
		}
		else if (GUI.Button(new Rect(220, 110, 200, 90), "Update 'my_text.txt'"))
		{
			StartCoroutine(UploadText());
		}

		GUI.Label(new Rect(10, 210, 200, 30), DateTime.Now.ToString());
	}

	GoogleDrive.File file = null;
	bool uploadScreenshotInProgress = false;

	IEnumerator UploadScreenshot()
	{
		if (drive == null || !drive.IsAuthorized || uploadScreenshotInProgress)
			yield break;

		uploadScreenshotInProgress = true;

		yield return new WaitForEndOfFrame();

		var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
		tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		tex.Apply();

		var png = tex.EncodeToPNG();
		
		GameObject.Destroy(tex);
		tex = null;

		if (file == null)
		{
			var upload = drive.UploadFile("my_screen.png", "image/png", png);
			yield return StartCoroutine(upload);
			
			file = GoogleDrive.GetResult<GoogleDrive.File>(upload);
		}
		else
		{
			var upload = drive.UploadFile(file, png);
			yield return StartCoroutine(upload);
			
			file = GoogleDrive.GetResult<GoogleDrive.File>(upload);
		}

		uploadScreenshotInProgress = false;
	}

	bool uploadTextInProgress = false;

	IEnumerator UploadText()
	{
		if (drive == null || !drive.IsAuthorized || uploadTextInProgress)
			yield break;

		uploadTextInProgress = true;

		// Get 'my_text.txt'.
		var list = drive.ListFilesByQueary("title = 'my_text.txt'");
		yield return StartCoroutine(list);

		GoogleDrive.File file;
		Dictionary<string, object> data;

		var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(list);
		
		if (files == null || files.Count > 0)
		{
			// Found!
			file = files[0];

			// Download file data.
			var download = drive.DownloadFile(file);
			yield return StartCoroutine(download);

			var bytes = GoogleDrive.GetResult<byte[]>(download);
			try
			{
				// Data is json format.
				var reader = new JsonFx.Json.JsonReader(Encoding.UTF8.GetString(bytes));
				data = reader.Deserialize<Dictionary<string, object>>();
			}
			catch (Exception e)
			{
				Debug.LogWarning(e);

				data = new Dictionary<string, object>();
			}
		}
		else
		{
			// Make a new file.
			file = new GoogleDrive.File(new Dictionary<string, object>
			{
				{ "title", "my_text.txt" },
				{ "mimeType", "application/octet-stream" }, // "text/plain" occurs "Parse Error (400)" sometime.
				{ "description", "test" }
			});
			data = new Dictionary<string, object>();
		}

		// Update file data.
		data["date"] = DateTime.Now.ToString();
		if (data.ContainsKey("count"))
			data["count"] = (int)data["count"] + 1;
		else
			data["count"] = 0;

		// And uploading...
		{
			var bytes = Encoding.UTF8.GetBytes(JsonFx.Json.JsonWriter.Serialize(data));

			var upload = drive.UploadFile(file, bytes);
			yield return StartCoroutine(upload);

			if (!(upload.Current is Exception))
			{
				Debug.Log("Upload complete!");
			}
		}

		uploadTextInProgress = false;
	}
}
