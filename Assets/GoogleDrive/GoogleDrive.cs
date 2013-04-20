using System;
using System.Collections;
using UnityEngine;

partial class GoogleDrive
{
	/// <summary>
	/// Google Drive Application Client ID
	/// </summary>
	public static string clientID { get; set; }

	/// <summary>
	/// Google Drive Application Secret Key
	/// </summary>
	/// <remarks>Android doesn't need this value.</remarks>
	public static string clientSecret { get; set; }

	/// <summary>
	/// Google Drive Application Redirect URI
	/// </summary>
	/// <remarks>Android/iOS doesn't need this value.</remarks>
	public static string redirectURI { get; set; }

	/// <summary>
	/// Start authorization.
	/// </summary>
	IEnumerator Authorize()
	{
		int key = clientID.GetHashCode();

		string token = 
			PlayerPrefs.GetString("UnityGoogleDrive_Token_" + key, "");
		string refreshToken = 
			PlayerPrefs.GetString("UnityGoogleDrive_RefreshToken_" + key, "");

#if !UNITY_EDITOR && UNITY_ANDROID
		// TODO: Android authorization
#elif !UNITY_EDITOR && UNITY_IPHONE
		// TODO: iOS authorization
#else
		
#endif

		yield break;
	}
}
