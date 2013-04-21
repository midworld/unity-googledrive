using System;
using System.Collections;
using UnityEngine;

public class DriveTest3 : MonoBehaviour
{
	GoogleDrive drive;

	void Start()
	{
		StartCoroutine(InitGoogleDrive());
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
			Debug.LogWarning(authorization.Current as Exception);
		else
			Debug.Log("User Account: " + drive.UserAccount);

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
