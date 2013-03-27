using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

class DriveTest : MonoBehaviour
{
	public Transform cube = null;

	//float elapsedTime = 0;

	void Start()
	{
		Debug.LogWarning("Start()");

		string json = @"{""hello"":""world"",""one"":1}";
		JsonFx.Json.JsonReader reader = new JsonFx.Json.JsonReader(json);
		var ret = reader.Deserialize<Dictionary<string, object>>();
		Debug.Log(ret["hello"]);
		Debug.Log(ret["one"]);
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

		//elapsedTime += Time.deltaTime;

		//if (elapsedTime >= 1)
		//{
		//    Debug.Log("elapsedTime: " + elapsedTime);
		//    elapsedTime = 0;
		//}
	}

	bool wait = false;

	IEnumerator GetList()
	{
		wait = true;

		yield return StartCoroutine(new GoogleDrive());

		wait = false;
	}

#if UNITY_IPHONE
	[DllImport ("__Internal")]
	static extern void Auth();
#endif
	
	void OnGUI()
	{
		if (!wait && GUI.Button(new Rect(10, 10, 300, 100), "Get List"))
		{
#if UNITY_ANDROID
			StartCoroutine(GetList());
#elif UNITY_IPHONE
			Auth();
#endif
		}

//        if (GUI.Button(new Rect(10, 10, 300, 100), "Auth"))
//        {
//#if UNITY_ANDROID
//            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

//            AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
//            pluginClass.CallStatic("setUnityActivity", new object[] { unityActivity });
//            pluginClass.CallStatic("auth");

//            string[] ret = pluginClass.CallStatic<string[]>("list");
//            for (int i = 0; i < ret.Length; i++)
//            {
//                Debug.Log(ret[i]);
//            }

//            pluginClass.Dispose();
//            unityActivity.Dispose();
//            unityPlayerClass.Dispose();
//#endif
//        }

//        if (GUI.Button(new Rect(10, 10, 300, 100), "hello"))
//        {
//#if UNITY_ANDROID
//            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

//            AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
//            //pluginClass.CallStatic("Test", new object[] { unityActivity, "hello world" });
//            pluginClass.CallStatic("Test2", new object[] { unityActivity });

//            pluginClass.Dispose();
//            unityActivity.Dispose();
//            unityPlayerClass.Dispose();
//#endif

//        }

//        if (GUI.Button(new Rect(10, 120, 300, 100), "world"))
//        {
//#if UNITY_ANDROID
//            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

//            AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
//            pluginClass.CallStatic("Show", new object[] { unityActivity });

//            pluginClass.Dispose();
//            unityActivity.Dispose();
//            unityPlayerClass.Dispose();
//#endif
//        }

		if (!screenshot && GUI.Button(new Rect(10, 230, 300, 100), "screenshot"))
		{
			screenshot = true;
		}
	}

	bool screenshot = false;

	void OnPostRender()
	{
		if (screenshot)
		{
			screenshot = false;

			int width = Screen.width;
			int height = Screen.height;
			Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();

			var bytes = tex.EncodeToPNG();
			Destroy(tex);

			Debug.LogWarning(Application.persistentDataPath);

			File.WriteAllBytes(Application.persistentDataPath + "/screenshot.png", bytes);
		}
	}
}
