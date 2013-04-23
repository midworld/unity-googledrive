using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveTest3 : MonoBehaviour
{
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

	bool initInProgress = false;

	IEnumerator InitGoogleDrive()
	{
		initInProgress = true;

		drive = new GoogleDrive();
		drive.ClientID = "897584417662-rnkgkl5tlpnsau7c4oc0g2jp08cpluom.apps.googleusercontent.com";
		drive.ClientSecret = "tGNLbYnrdRO2hdFmwJAo5Fbt";
		drive.RootDirectoryName = "UnityGoogleDriveTest";

		var authorization = drive.Authorize();
		yield return StartCoroutine(authorization);
		
		//StartCoroutine(authorization);
		//while (!GoogleDrive.IsDone(authorization))
		//    yield return null;

		if (authorization.Current is Exception)
			Debug.LogWarning(authorization.Current as Exception);
		else
			Debug.Log("User Account: " + drive.UserAccount);

		var appData = drive.AppData();
		yield return StartCoroutine(appData);

		string appDataId = null;

		if (appData.Current is GoogleDrive.AsyncSuccess)
		{
			var result = (appData.Current as GoogleDrive.AsyncSuccess).Result;

			if (result is Dictionary<string, object>)
			{
				var json = result as Dictionary<string, object>;

				Debug.Log(json["id"]);
				appDataId = json["id"] as string;
			}
		}

		yield return StartCoroutine(drive.InsertFile(appDataId));

		yield return StartCoroutine(drive.ListFiles());

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
			GUI.Button(new Rect(10, 110, 200, 90), "Revoke");
			GUI.enabled = true;
		}
		else if (GUI.Button(new Rect(10, 110, 200, 90), "Revoke"))
		{
			StartCoroutine(Revoke());
		}
	}
}
