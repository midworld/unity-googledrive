using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

class GoogleDrive : Midworld.UnityCoroutine
{
	static bool activitySet = false;

	int id = -1;

	public GoogleDrive()
	{
#if UNITY_ANDROID
		using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin"))
		{
			if (!activitySet)
			{
				AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

				pluginClass.CallStatic("setUnityActivity", new object[] { unityActivity });

				unityActivity.Dispose();
				unityPlayerClass.Dispose();

				activitySet = true;
			}

			pluginClass.CallStatic("setAuthRejectionCallback", new object[] { (AndroidJavaRunnable)OnAuthRejection });

			pluginClass.CallStatic("auth", new object[] { 
				(AndroidJavaRunnable)OnAuthSuccess, (AndroidJavaRunnable)OnAuthFailure } );

			AndroidJavaRunnable callback = OnComplete;

			id = pluginClass.CallStatic<int>("list", new object[] { callback });

			pluginClass.Dispose();
		}
#endif
	}

	void OnComplete()
	{
#if UNITY_ANDROID
		using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin"))
		{
			string error = pluginClass.CallStatic<string>("getError", new object[] { id });

			if (error.Length == 0)
			{
				string[] ret = pluginClass.CallStatic<string[]>("getResult", new object[] { id });

				for (int i = 0; i < ret.Length; i++)
				{
					Debug.Log(ret[i]);
				}
			}
			else
			{
				Debug.LogError(error);

				string code = error.Substring(0, error.IndexOf(':'));

				switch (code)
				{
					case "UserRecoverableAuthIOException":
						// retry!
						break;
					case "Exception":
						break;
				}
			}

			pluginClass.Dispose();
		}

		isDone = true;
#endif
	}

	void OnAuthSuccess()
	{
		Debug.Log("OnAuthSuccess");
	}

	void OnAuthFailure()
	{
		Debug.Log("OnAuthFailure");
	}

	void OnAuthRejection()
	{
		Debug.LogError("OnAuthRejection");
	}
}
