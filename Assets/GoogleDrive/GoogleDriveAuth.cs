using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Midworld;

namespace GoogleDrive
{
	class Auth : UnityCoroutine
	{
		public static string clientID { get; set; }

		public static string clientSecret { get; set; }

		public static string redirectURI { get; set; }

		// ----

		public static bool isAuthorized { get; private set; }

		public static string selectedAccountName { get; private set; }

		public static string token { get; private set; }

		public static int tokenExpiresIn { get; private set; }

		public static string refreshToken { get; private set; }

		// ----

		public static IEnumerator GetTokenByAuthorizationCode(string code)
		{
			token = null;

			var request = new UnityWebRequest("https://accounts.google.com/o/oauth2/token");

			request.method = "POST";
			request.headers["Content-Type"] = "application/x-www-form-urlencoded";
			request.postData = Encoding.UTF8.GetBytes(string.Format(
				"code={0}&" +
				"client_id={1}&" +
				"client_secret={2}&" + 
				"redirect_uri={3}&" +
				"grant_type=authorization_code",
				code, clientID, clientSecret, redirectURI));

			//Debug.Log(Encoding.UTF8.GetString(request.postData));

			var response = request.GetResponse();

			while (!response.isDone)
				yield return null;

			if (response.error != null)
			{
				Debug.LogWarning("GetTokenByAuthorizationCode:\n" + response.error);
				yield break;
			}

			//Debug.Log(response.text);

			JsonFx.Json.JsonReader reader = new JsonFx.Json.JsonReader(response.text);
			var json = reader.Deserialize<Dictionary<string, object>>();

			if (json == null)
			{
				Debug.LogError("GetTokenByAuthorizationCode:\n" + response.text);
				yield break;
			}
			else if (json.ContainsKey("error"))
			{
				Debug.LogWarning("GetTokenByAuthorizationCode:\n" + json["error"]);
				yield break;
			}

			token = json["access_token"] as string;
			tokenExpiresIn = int.Parse(json["expires_in"] as string);
			if (json.ContainsKey("refresh_token"))
				refreshToken = json["refresh_token"] as string;
			if (json.ContainsKey("email"))
				selectedAccountName = json["email"] as string;

			PlayerPrefs.SetString("UnityGoogleDrive_AccessToken", token);
			PlayerPrefs.SetInt("UnityGoogleDrive_ExpiresIn", tokenExpiresIn);
			PlayerPrefs.SetString("UnityGoogleDrive_TokenGetAt", DateTime.Now.ToString());
			if (refreshToken != null)
				PlayerPrefs.SetString("UnityGoogleDrive_RefreshToken", refreshToken);
			if (selectedAccountName != null)
				PlayerPrefs.SetString("UnityGoogleDrive_Email", selectedAccountName);

			isAuthorized = true;
		}

		public static bool HasAccessToken()
		{
			token = PlayerPrefs.GetString("UnityGoogleDrive_AccessToken", null);
			return (token != null);
		}

		public static bool CanRefreshToken()
		{
			refreshToken = PlayerPrefs.GetString("UnityGoogleDrive_RefreshToken", null);
			return (refreshToken != null);
		}

		public static bool IsTokenExpired()
		{
			token = PlayerPrefs.GetString("UnityGoogleDrive_AccessToken", null);
			tokenExpiresIn = PlayerPrefs.GetInt("UnityGoogleDrive_ExpiresIn", 0);
			string tokenGetAt = PlayerPrefs.GetString("UnityGoogleDrive_TokenGetAt", null);

			if (token == null || tokenExpiresIn == 0 || tokenGetAt == null)
				return true;

			DateTime d;
			if (!DateTime.TryParse(tokenGetAt, out d))
				return true;

			Debug.Log("IsTokenExpired:\nToken will expire after " + 
				(int)(tokenExpiresIn - (DateTime.Now - d).TotalSeconds) + " sec");

			return ((DateTime.Now - d).TotalSeconds >= tokenExpiresIn);
		}

		public static IEnumerator ValidateToken(string _token = null)
		{
			if (_token == null)
				_token = token;
			if (_token == null)
				_token = PlayerPrefs.GetString("UnityGoogleDrive_AccessToken", null);
			if (_token == null)
			{
				Debug.LogError("ValidateToken:\nToken is null");
				yield break;
			}

			var request = new UnityWebRequest(
				"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=" + _token);
			var response = request.GetResponse();

			while (!response.isDone)
				yield return null;

			if (response.error != null)
			{
				Debug.LogWarning("ValidateToken:\n" + response.error);
				yield break;
			}

			//Debug.Log(response.text);

			JsonFx.Json.JsonReader reader = new JsonFx.Json.JsonReader(response.text);
			var json = reader.Deserialize<Dictionary<string, object>>();

			if (json == null)
			{
				Debug.LogError("ValidateToken:\n" + response.text);
				yield break;
			}
			else if (json.ContainsKey("error"))
			{
				Debug.LogWarning("ValidateToken:\n" + json["error"]);
				yield break;
			}

			token = _token;
			PlayerPrefs.SetString("UnityGoogleDrive_AccessToken", token);

			if (json.ContainsKey("email"))
				selectedAccountName = json["email"] as string;

			isAuthorized = true;
		}

		public static IEnumerator RefreshToken(string _refreshToken = null)
		{
			if (_refreshToken == null)
				_refreshToken = GoogleDrive.Auth.refreshToken;
			if (_refreshToken == null)
				_refreshToken = PlayerPrefs.GetString("UnityGoogleDrive_RefreshToken", null);
			if (_refreshToken == null)
			{
				Debug.LogError("RefreshToken:\nRefresh token is null.");
				yield break;
			}

			token = null;

			var request = new UnityWebRequest("https://accounts.google.com/o/oauth2/token");

			request.method = "POST";
			request.headers["Content-Type"] = "application/x-www-form-urlencoded";
			request.postData = Encoding.UTF8.GetBytes(string.Format(
				"client_id={0}&" +
				"client_secret={1}&" +
				"refresh_token={2}&" +
				"grant_type=refresh_token",
				clientID, clientSecret, _refreshToken));

			//Debug.Log(Encoding.UTF8.GetString(request.postData));

			var response = request.GetResponse();

			while (!response.isDone)
				yield return null;

			if (response.error != null)
			{
				Debug.LogWarning("RefreshToken:\n" + response.error);
				yield break;
			}

			//Debug.Log(response.text);

			JsonFx.Json.JsonReader reader = new JsonFx.Json.JsonReader(response.text);
			var json = reader.Deserialize<Dictionary<string, object>>();

			if (json == null)
			{
				Debug.LogError("GetTokenByAuthorizationCode:\n" + response.text);
				yield break;
			}
			else if (json.ContainsKey("error"))
			{
				Debug.LogWarning("RefreshToken:\n" + json["error"]);
				yield break;
			}

			token = json["access_token"] as string;
			tokenExpiresIn = int.Parse(json["expires_in"] as string);
			if (json.ContainsKey("refresh_token"))
				refreshToken = json["refresh_token"] as string;
			if (json.ContainsKey("email"))
				selectedAccountName = json["email"] as string;

			PlayerPrefs.SetString("UnityGoogleDrive_AccessToken", token);
			PlayerPrefs.SetInt("UnityGoogleDrive_ExpiresIn", tokenExpiresIn);
			PlayerPrefs.SetString("UnityGoogleDrive_TokenGetAt", DateTime.Now.ToString());
			if (refreshToken != null)
				PlayerPrefs.SetString("UnityGoogleDrive_RefreshToken", refreshToken);
			if (selectedAccountName != null)
				PlayerPrefs.SetString("UnityGoogleDrive_Email", selectedAccountName);

			isAuthorized = true;
		}

		public static IEnumerator RevokeToken(string _token = null) 
		{
			if (_token == null)
				_token = token;
			if (_token == null)
				_token = PlayerPrefs.GetString("UnityGoogleDrive_AccessToken", null);
			if (_token == null)
			{
				Debug.LogError("RevokeToken:\nToken is null");
				yield break;
			}

			var request = new UnityWebRequest("https://accounts.google.com/o/oauth2/revoke?token=" + _token);
			var response = request.GetResponse();

			while (!response.isDone)
				yield return null;

			if (response.error != null)
			{
				Debug.LogWarning("RevokeToken:\n" + response.error);
				yield break;
			}

			//Debug.Log(response.text);

			JsonFx.Json.JsonReader reader = new JsonFx.Json.JsonReader(response.text);
			var json = reader.Deserialize<Dictionary<string, object>>();

			if (json != null && json.ContainsKey("error"))
			{
				Debug.LogWarning("RevokeToken:\n" + json["error"]);
				yield break;
			}

			token = null;
			refreshToken = null;
			PlayerPrefs.DeleteKey("UnityGoogleDrive_AccessToken");
			PlayerPrefs.DeleteKey("UnityGoogleDrive_RefreshToken");

			isAuthorized = false;
		}

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
