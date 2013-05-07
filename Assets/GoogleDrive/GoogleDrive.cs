using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Midworld;
using JsonFx.Json;

/// <summary>
/// Google Drive for Unity3D
/// </summary>
partial class GoogleDrive
{
	/// <summary>
	/// Google Drive Application Client ID
	/// </summary>
	public string ClientID { get; set; }

	/// <summary>
	/// Google Drive Application Secret Key
	/// </summary>
	/// <remarks>Android doesn't need this value.</remarks>
	public string ClientSecret { get; set; }

	/// <summary>
	/// Success result.
	/// </summary>
	/// <seealso cref="GetResult<T>"/>
	public class AsyncSuccess
	{
		public object Result { get; private set; }

		public AsyncSuccess() : this(null) { }
		public AsyncSuccess(object o)
		{
			Result = o;
		}
	}

	/// <summary>
	/// Get the result from AsyncSuccess.
	/// </summary>
	/// <typeparam name="T">Type of result.</typeparam>
	/// <param name="async">Async routine.</param>
	/// <returns>Result or null.</returns>
	/// <example>
	/// <code>
	/// var listFiles = drive.ListFiles(drive.AppData);
	///	yield return StartCoroutine(listFiles);
	///	var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	///	
	/// if (files != null)
	///		do something;
	/// </code>
	/// </example>
	public static T GetResult<T>(IEnumerator async)
	{
		if (async.Current is AsyncSuccess)
			return (T)(async.Current as AsyncSuccess).Result;
		else
			return default(T);
	}

	/// <summary>
	/// Check the async operation is done.
	/// </summary>
	/// <param name="async">Async operation.</param>
	/// <returns>True if the operation is done.</returns>
	public static bool IsDone(IEnumerator async)
	{
		return (async.Current is AsyncSuccess || async.Current is Exception);
	}

#if !UNITY_EDITOR && UNITY_ANDROID
	AndroidJavaClass pluginClass;
#endif

	/// <summary>
	/// Constructor.
	/// </summary>
	public GoogleDrive()
	{
#if !UNITY_EDITOR && UNITY_ANDROID
		pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");

		// Set Unity activity.
		{
			AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

			pluginClass.CallStatic("setUnityActivity", new object[] { unityActivity });

			unityActivity.Dispose();
			unityPlayerClass.Dispose();
		}
#elif !UNITY_EDITOR && UNITY_IPHONE
#endif
	}

	/*! \mainpage Google Drive for Unity3D
	 *
	 * \section intro_sec Introduction
	 *
	 * Google Drive for Unity3D plugin.
	 * 
	 * You can upload files, explore, download on the Google Drive storage.
	 * 
	 * This plugin supports PC(Windows and Mac), Android and iOS.
	 * 
	 * - Google Drive supports 'App Data' that is only accessible by your application.
	 * See https://developers.google.com/drive/appdata
	 * 
	 * \section install_sec Installation
	 *
	 * \subsection step1 Step 1: Enable the Google Drive API
	 * 
	 * - PC and iOS: https://developers.google.com/drive/quickstart-ios (Step 1)
	 * 
	 * - Android: https://developers.google.com/drive/quickstart-android (Step 1, 2)
	 *
	 * \subsection step2 Step 2: Type Your Client ID and Secret to DriveTest.cs.
	 * 
	 * In the line 40:
	 * \code
	 * drive.ClientID = "YOUR CLIENT ID";
	 * drive.ClientSecret = "YOUR CLIENT SECRET";
	 * \endcode
	 * 
	 * - PC and iOS can use same Client ID and secret.
	 * 
	 * - Android doesn't need Client ID and secret.
	 * 
	 * \subsection step3 Step 3: Run the Sample Scene 'DriveTest'.
	 * 
	 * All done!
	 *
	 * \section demo Demo Download
	 * 
	 * - Windows Binary: <a href="./unitydrivetest_win.zip">unitydrivetest_win.zip</a>
	 * 
	 * - Android APK: <a href="./unitydrivetest.apk">unitydrivetest.apk</a>
	 * 
	 * \section sample Sample Code
	 * 
	 * \code
	 * var drive = new GoogleDrive();
	 * drive.ClientID = "YOUR CLIENT ID";
	 * drive.ClientSecret = "YOUR CLIENT SECRET";
	 * 
	 * // Request authorization.
	 * var authorization = drive.Authorize();
	 * yield return StartCoroutine(authorization);
	 * 
	 * if (authorization.Current is Exception)
	 * {
	 *	Debug.LogWarning(authorization.Current as Exception);
	 *	yield break;
	 * }
	 * 
	 * // Authorization succeeded.
	 * Debug.Log("User Account: " + drive.UserAccount);
	 * 
	 * // Upload a text file.
	 * var bytes = Encoding.UTF8.GetBytes("world!");
	 * yield return StartCoroutine(drive.UploadFile("hello.txt", "text/plain", bytes));
	 * 
	 * // Get all files.
	 * var listFiles = drive.ListAllFiles();
	 * yield return StartCoroutine(listFiles);
	 * 
	 * var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	 * if (files != null)
	 * {
	 *	foreach (var file in files)
	 *	{
	 *		// Download a text file and print.
	 *		if (file.Title.EndsWith(".txt"))
	 *		{
	 *			var download = drive.DownloadFile(file);
	 *			yield return StartCoroutine(download);
	 *			
	 *			var data = GoogleDrive.GetResult<byte[]>(download);
	 *			Debug.Log(System.Text.Encoding.UTF8.GetString(data));
	 *		}
	 *	}
	 * }
	 * \endcode
	 * 
	 * Work with 'App Data'.
	 * 
	 * \code
	 * // Upload score in 'AppData'.
	 * int score = 10000;
	 * var bytes = Encoding.UTF8.GetBytes(score.ToString());
	 * 
	 * // User cannot see 'score.txt'. Only your app can see this file.
	 * StartCoroutine(drive.UploadFile("score.txt", "text/plain", drive.AppData, bytes));
	 * \endcode
	 */
}
