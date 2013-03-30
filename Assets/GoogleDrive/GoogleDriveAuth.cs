using System;
using System.Collections;
using UnityEngine;

namespace GoogleDrive
{
	class Auth
	{
		public static bool isAuthorized
		{
			get;
			private set;
		}

#if UNITY_ANDROID
		static bool activitySet = false;

		static void EnsureActivitySet()
		{
			if (!activitySet)
			{
				AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
				AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
				
				pluginClass.CallStatic("setUnityActivity", new object[] { unityActivity });
				
				unityActivity.Dispose();
				unityPlayerClass.Dispose();
				pluginClass.Dispose();

				activitySet = true;
			}
		}

		public static void Authorize()
		{
			EnsureActivitySet();

			isAuthorized = true;
		}

		public static void Unauthorize()
		{
			EnsureActivitySet();

			AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
			pluginClass.CallStatic("clearSelectedAccountName");
			pluginClass.Dispose();

			isAuthorized = false;
		}
#endif
	}
}
