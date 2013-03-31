using System;
using System.Collections;
using UnityEngine;

namespace GoogleDrive
{
	class Auth : Midworld.UnityCoroutine
	{
		public static bool isAuthorized { get; private set; }

		public static string selectedAccountName { get; private set; }

		public static string token { get; private set; }

#if UNITY_ANDROID
		#region Authorization Coroutine
		public Auth()
		{
			EnsureActivitySet();

			AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
			pluginClass.CallStatic("auth", new object[] { 
				(AndroidJavaRunnable)OnAuthSuccess, (AndroidJavaRunnable)OnAuthFailure });
			pluginClass.Dispose();
		}

		void OnAuthSuccess()
		{
			isAuthorized = true;

			AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
			selectedAccountName = pluginClass.CallStatic<string>("getSelectedAccountName");
			token = pluginClass.CallStatic<string>("getAuthToken");
			pluginClass.Dispose();

			isDone = true;
		}

		void OnAuthFailure()
		{
			isDone = true;
		}
		#endregion

		#region Static Methods
		/// <summary>
		/// Is Unity activity set.
		/// </summary>
		static bool isActivitySet = false;

		/// <summary>
		/// Ensure Unity activity is set.
		/// </summary>
		public static void EnsureActivitySet()
		{
			if (!isActivitySet)
			{
				AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
				AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
				
				pluginClass.CallStatic("setUnityActivity", new object[] { unityActivity });
				
				unityActivity.Dispose();
				unityPlayerClass.Dispose();
				pluginClass.Dispose();

				isActivitySet = true;
			}
		}

		static string _apiKey = null;

		public static string apiKey
		{
			get
			{
				return _apiKey;
			}
			set
			{
				_apiKey = value;

				AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
				pluginClass.CallStatic("setAPIKey", new object[] { _apiKey });
				pluginClass.Dispose();
			}
		}

		/// <summary>
		/// Request for authorization.
		/// </summary>
		public static Auth Authorize()
		{
			return new Auth();
		}

		/// <summary>
		/// Clear authorization.
		/// </summary>
		public static void Unauthorize()
		{
			EnsureActivitySet();

			AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
			pluginClass.CallStatic("clearSelectedAccountName");
			pluginClass.Dispose();

			isAuthorized = false;
			selectedAccountName = null;
			token = null;
		}
		#endregion
#endif
	}
}
