using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Midworld;
using JsonFx.Json;

partial class GoogleDrive
{
	/// <summary>
	/// Port number for authorization redirection server.
	/// </summary>
	const int SERVER_PORT = 9271;

	/// <summary>
	/// Redirection URI.
	/// </summary>
	string RedirectURI
	{
		get
		{
#if !UNITY_EDITOR && UNITY_IPHONE
			return "urn:ietf:wg:oauth:2.0:oob";
#else
			return "http://localhost:" + SERVER_PORT; 
#endif
		}
	}

	Uri AuthorizationURL
	{
		get
		{
			return new Uri("https://accounts.google.com/o/oauth2/auth?" +
				"scope=" +
					"https://www.googleapis.com/auth/drive.file" +
					" https://www.googleapis.com/auth/userinfo.email" +
					" https://www.googleapis.com/auth/drive.appdata" +
				"&response_type=code" +
				"&redirect_uri=" + RedirectURI +
				"&client_id=" + ClientID);
		}
	}

	bool isAuthorized = false;

	/// <summary>
	/// Is Google Drive authorized.
	/// </summary>
	public bool IsAuthorized
	{
		get { return isAuthorized; }
		private set { isAuthorized = value; }
	}

	string accessToken = null;

	/// <summary>
	/// Access token.
	/// </summary>
	string AccessToken
	{
		get
		{
			if (accessToken == null)
			{
				int key = ClientID.GetHashCode();
				accessToken = PlayerPrefs.GetString("UnityGoogleDrive_Token_" + key, "");
			}

			return accessToken;
		}
		set
		{
			if (accessToken != value)
			{
				accessToken = value;

				int key = ClientID.GetHashCode();

				if (accessToken != null)
					PlayerPrefs.SetString("UnityGoogleDrive_Token_" + key, accessToken);
				else
					PlayerPrefs.DeleteKey("UnityGoogleDrive_Token_" + key);
			}
		}
	}

	string refreshToken = null;

	/// <summary>
	/// Refresh token.
	/// </summary>
	string RefreshToken
	{
		get
		{
			if (refreshToken == null)
			{
				int key = ClientID.GetHashCode();
				refreshToken = PlayerPrefs.GetString("UnityGoogleDrive_RefreshToken_" + key, "");
			}

			return refreshToken;
		}
		set
		{
			if (refreshToken != value)
			{
				refreshToken = value;

				int key = ClientID.GetHashCode();

				if (refreshToken != null)
					PlayerPrefs.SetString("UnityGoogleDrive_RefreshToken_" + key, refreshToken);
				else
					PlayerPrefs.DeleteKey("UnityGoogleDrive_RefreshToken_" + key);
			}
		}
	}

	string userAccount = null;

	/// <summary>
	/// User's E-Mail address.
	/// </summary>
	public string UserAccount
	{
		get
		{
			if (userAccount == null)
			{
				int key = ClientID.GetHashCode();
				userAccount = PlayerPrefs.GetString("UnityGoogleDrive_UserAccount_" + key, "");
			}

			return userAccount;
		}
		private set
		{
			if (userAccount != value)
			{
				userAccount = value;

				int key = ClientID.GetHashCode();

				if (userAccount != null)
					PlayerPrefs.SetString("UnityGoogleDrive_UserAccount_" + key, userAccount);
				else
					PlayerPrefs.DeleteKey("UnityGoogleDrive_UserAccount_" + key);
			}
		}
	}

	/// <summary>
	/// Access token lifetime.
	/// </summary>
	DateTime expiresIn = DateTime.MaxValue;

	/// <summary>
	/// Start authorization.
	/// </summary>
	/// <returns>AsyncSuccess or Exception for error.</returns>
	/// <example>
	/// <code>
	/// var drive = new GoogleDrive();
	/// drive.ClientID = "YOUR CLIENT ID"; // unnecessary to Android
	/// drive.ClientSecret = "YOUR CLIENT SECRET"; // unnecessary to Android
	/// 
	/// var authorization = drive.Authorize();
	///	yield return StartCoroutine(authorization);
	///
	///	if (authorization.Current is Exception)
	///	{
	///		Debug.LogError(authorization.Current as Exception);
	///		return;
	///	}
	///	
	/// do something;
	/// </code>
	/// </example>
	public IEnumerator Authorize()
	{
		#region CHECK CLIENT ID AND SECRET
		if (Application.platform == RuntimePlatform.Android)
		{
			if (ClientID == null)
			{
				yield return new Exception(-1, "ClientID is null.");
				yield break;
			}
		}
		else
		{
			if (ClientID == null || ClientSecret == null)
			{
				yield return new Exception(-1, "ClientID or ClientSecret is null.");
				yield break;
			}
		}
		#endregion

#if !UNITY_EDITOR && UNITY_ANDROID
		pluginClass.CallStatic("auth", new object[] { 
			(AndroidJavaRunnable)AuthSuccess, (AndroidJavaRunnable)AuthFailure });

		while (!success && !failure)
			yield return null;

		if (success)
		{
			success = false;

			AccessToken = pluginClass.CallStatic<string>("getAuthToken");
			UserAccount = pluginClass.CallStatic<string>("getSelectedAccountName");

			var validate = ValidateToken(AccessToken);
			while (validate.MoveNext())
				yield return null;

			IsAuthorized = true;
		}
		else
		{
			failure = false;

			IsAuthorized = false;

			yield return new Exception(-1, "Authorization failed.");
			yield break;
		}
#else
		if (AccessToken == "")
		{
			// Open browser and authorization.
			var routine = GetAuthorizationCodeAndAccessToken();
			while (routine.MoveNext())
				yield return null;

			if (routine.Current is Exception)
			{
				yield return routine.Current;
				yield break;
			}
			else if (AccessToken == "")
			{
				yield return new Exception(-1, "Authorization failed.");
				yield break;
			}
			else
			{
				IsAuthorized = true;
			}
		}
		else
		{
			// Check the access token.
			var validate = ValidateToken(accessToken);
			{
				while (validate.MoveNext())
					yield return null;

				if (validate.Current is Exception)
				{
					yield return validate.Current;
					yield break;
				}

				var res = (TokenInfoResponse)validate.Current;

				// Require re-authorization.
				if (res.error != null)
				{
					// Remove saved access token.
					AccessToken = null;

					if (RefreshToken != "")
					{
						// Try refresh token.
						var refresh = RefreshAccessToken();
						while (refresh.MoveNext())
							yield return null;
					}

					// No refresh token or refresh failed.
					if (AccessToken == "")
					{
						// Open browser and authorization.
						var routine = GetAuthorizationCodeAndAccessToken();
						while (routine.MoveNext())
							yield return null;

						if (routine.Current is Exception)
						{
							yield return routine.Current;
							yield break;
						}
					}

					// If access token is available, authorization is succeeded.
					if (AccessToken != "")
						IsAuthorized = true;
					else
					{
						yield return new Exception(-1, "Authorization failed.");
						yield break;
					}
				}
				else
				{
					// Validating succeeded.
					IsAuthorized = true;
					UserAccount = res.email;

					expiresIn = DateTime.Now + new TimeSpan(0, 0, res.expiresIn);
				}
			}
		}
#endif
		// Get AppData folder.
		var getAppData = GetFile("appdata");
		while (getAppData.MoveNext())
			yield return null;

		if (getAppData.Current is AsyncSuccess)
		{
			AppData = (getAppData.Current as AsyncSuccess).Result as File;
		}
		else
		{
			Debug.LogWarning("Cannot get the AppData folder: " + 
				getAppData.Current);
		}

		yield return new AsyncSuccess();
	}

#if !UNITY_EDITOR && UNITY_ANDROID
	bool success = false, failure = false;

	void AuthSuccess()
	{
		success = true;
	}

	void AuthFailure()
	{
		failure = true;
	}
#endif

	/// <summary>
	/// Unauthorize and remove the saved session.
	/// </summary>
	/// <returns>AsyncSuccess or Exception for error.</returns>
	/// <example>
	/// <code>
	/// StartCoroutine(drive.Unauthorize());
	/// </code>
	/// </example>
	public IEnumerator Unauthorize()
	{
		IsAuthorized = false;

#if !UNITY_EDITOR && UNITY_ANDROID
		pluginClass.CallStatic("clearSelectedAccountName");
		
		AccessToken = null;

		yield return new AsyncSuccess();
#else
		var revoke = RevokeToken(AccessToken);
		while (revoke.MoveNext())
			yield return null;

		AccessToken = null;
		RefreshToken = null;

		if (revoke.Current is Exception)
		{
			yield return revoke.Current;
			yield break;
		}
		else
		{
			var res = (RevokeResponse)revoke.Current;

			if (res.error != null)
			{
				yield return res.error;
				yield break;
			}
		}

		yield return new AsyncSuccess();
#endif
	}

	/// <summary>
	/// Get access token through Google login in web browser.
	/// </summary>
	/// <returns></returns>
	IEnumerator GetAuthorizationCodeAndAccessToken()
	{
		// Google authorization URL
		Uri uri = AuthorizationURL;

		string authorizationCode = null;

#if !UNITY_EDITOR && UNITY_ANDROID
		Debug.LogError("Android cannot support the authorization code.");
#elif !UNITY_EDITOR && UNITY_IPHONE
		#region OPEN WEBVIEW FOR AUTHORIZATION
		var obj = new GameObject("WebViewObject");
		var webView = obj.AddComponent<WebViewObject>();
		webView.url = AuthorizationURL;

		while (webView.token == null && !webView.cancelled)
			yield return null;

		if (webView.cancelled)
		{
			GameObject.Destroy(obj);

			IsAuthorized = false;

			yield return new Exception(-1, "Authorization failed.");
			yield break;
		}

		authorizationCode = webView.token;

		GameObject.Destroy(obj);
		#endregion
#else
		#region OPEN BROWSER FOR AUTHORIZATION
		System.Diagnostics.Process browser;
		bool windows = false;

		// Open the browser.
		if (Application.platform == RuntimePlatform.WindowsPlayer ||
			Application.platform == RuntimePlatform.WindowsEditor)
		{
			windows = true;

			System.Diagnostics.ProcessStartInfo startInfo =
				new System.Diagnostics.ProcessStartInfo("IExplore.exe");
			startInfo.Arguments = uri.ToString();

			browser = new System.Diagnostics.Process();
			browser.StartInfo = startInfo;
			browser.Start();
		}
		else
		{
			browser = System.Diagnostics.Process.Start(uri.ToString());
		}

		// Authorization code will redirect to this server.
		AuthRedirectionServer server = new AuthRedirectionServer();
		server.StartServer(SERVER_PORT);

		// Wait for authorization code.
		while (!windows || !browser.HasExited)
		{
			if (server.AuthorizationCode != null)
			{
				browser.CloseMainWindow();
				browser.Close();
				break;
			}
			else
				yield return null;
		}

		server.StopServer();

		// Authorization rejected.
		if (server.AuthorizationCode == null)
		{
			yield return new Exception(-1, "Authorization rejected.");
			yield break;
		}

		authorizationCode = server.AuthorizationCode;
		#endregion
#endif

		// Get the access token by the authroization code.
		var getAccessToken = GetAccessTokenByAuthorizationCode(authorizationCode);
		{
			while (getAccessToken.MoveNext())
				yield return null;

			if (getAccessToken.Current is Exception)
			{
				yield return getAccessToken.Current;
				yield break;
			}

			var res = (TokenResponse)getAccessToken.Current;
			if (res.error != null)
			{
				yield return res.error;
				yield break;
			}

			AccessToken = res.accessToken;
			RefreshToken = res.refreshToken;
		}

		// And validate for email address.
		var validate = ValidateToken(accessToken);
		{
			while (validate.MoveNext())
				yield return null;

			if (validate.Current is Exception)
			{
				yield return validate.Current;
				yield break;
			}

			var res = (TokenInfoResponse)validate.Current;
			if (res.error != null)
			{
				yield return res.error;
				yield break;
			}

			if (res.email != null)
				UserAccount = res.email;
		}
	}

	/// <summary>
	/// Get access token by the refresh token.
	/// </summary>
	/// <returns>null or Exception for error.</returns>
	IEnumerator RefreshAccessToken()
	{
		var refresh = GetAccessTokenByRefreshToken(RefreshToken);
		{
			while (refresh.MoveNext())
				yield return null;

			if (refresh.Current is Exception)
			{
				yield return refresh.Current;
				yield break;
			}

			var res = (TokenResponse)refresh.Current;
			if (res.error != null)
			{
				yield return res.error;
				yield break;
			}

			AccessToken = res.accessToken;
		}

		// And validate for email address.
		var validate = ValidateToken(accessToken);
		{
			while (validate.MoveNext())
				yield return null;

			if (validate.Current is Exception)
			{
				yield return validate.Current;
				yield break;
			}

			var res = (TokenInfoResponse)validate.Current;
			if (res.error != null)
			{
				yield return res.error;
				yield break;
			}

			if (res.email != null)
				UserAccount = res.email;
		}
	}

	/// <summary>
	/// If the access token is expired then refresh it.
	/// </summary>
	/// <returns>nothing or Exception for error.</returns>
	IEnumerator CheckExpiration()
	{
		if (DateTime.Now >= expiresIn)
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			pluginClass.CallStatic("auth", new object[] { 
				(AndroidJavaRunnable)AuthSuccess, (AndroidJavaRunnable)AuthFailure });

			while (!success && !failure)
				yield return null;

			if (success)
			{
				success = false;

				AccessToken = pluginClass.CallStatic<string>("getAuthToken");

				var validate = ValidateToken(AccessToken);
				while (validate.MoveNext())
					yield return validate.Current;
			}
			else
			{
				failure = false;

				yield return new Exception(-1, "Authorization failed.");
				yield break;
			}
#elif !UNITY_EDITOR && UNITY_IPHONE
			// TODO: iOS

			yield return null;
#else
			var refresh = RefreshAccessToken();
			while (refresh.MoveNext())
				yield return null;

			yield return refresh.Current;
#endif
		}
	}

	/// <summary>
	/// Response of 'token'.
	/// </summary>
	struct TokenResponse
	{
		public Exception error;
		public string accessToken;
		public string refreshToken;
		public int expiresIn;
		public string tokenType;

		public TokenResponse(Dictionary<string, object> json)
		{
			error = null;
			accessToken = null;
			refreshToken = null;
			expiresIn = 0;
			tokenType = null;

			if (json.ContainsKey("error"))
			{
				error = GetError(json);
			}
			else
			{
				if (json.ContainsKey("access_token"))
					accessToken = json["access_token"] as string;
				if (json.ContainsKey("refresh_token"))
					refreshToken = json["refresh_token"] as string;
				if (json.ContainsKey("expires_in"))
					expiresIn = (int)json["expires_in"];
				if (json.ContainsKey("token_type"))
					tokenType = json["token_type"] as string;
			}
		}
	}

	/// <summary>
	/// Get the access token by the authorization code.
	/// </summary>
	/// <param name="authorizationCode">Authorization code.</param>
	/// <returns>TokenResponse or Exception for error.</returns>
	IEnumerator GetAccessTokenByAuthorizationCode(string authorizationCode)
	{
		var request = new UnityWebRequest("https://accounts.google.com/o/oauth2/token");

		request.method = "POST";
		request.headers["Content-Type"] = "application/x-www-form-urlencoded";
		request.body = Encoding.UTF8.GetBytes(string.Format(
			"code={0}&" +
			"client_id={1}&" +
			"client_secret={2}&" +
			"redirect_uri={3}&" +
			"grant_type=authorization_code",
			authorizationCode, ClientID, ClientSecret, RedirectURI));

		var response = request.GetResponse();
		while (!response.isDone)
			yield return null;

		if (response.error != null)
		{
			yield return response.error;
			yield break;
		}

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "GetAccessToken response parsing failed.");
			yield break;
		}

		yield return new TokenResponse(json);
	}

	/// <summary>
	/// Get the access code by the refresh token.
	/// </summary>
	/// <param name="refreshToken">Refresh token.</param>
	/// <returns>TokenResponse or Exception for error.</returns>
	IEnumerator GetAccessTokenByRefreshToken(string refreshToken)
	{
		var request = new UnityWebRequest("https://accounts.google.com/o/oauth2/token");

		request.method = "POST";
		request.headers["Content-Type"] = "application/x-www-form-urlencoded";
		request.body = Encoding.UTF8.GetBytes(string.Format(
			"client_id={0}&" +
			"client_secret={1}&" +
			"refresh_token={2}&" +
			"grant_type=refresh_token",
			ClientID, ClientSecret, refreshToken));

		var response = request.GetResponse();
		while (!response.isDone)
			yield return null;

		if (response.error != null)
		{
			yield return response.error;
			yield break;
		}

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "RefreshToken response parsing failed.");
			yield break;
		}

		yield return new TokenResponse(json);
	}

	/// <summary>
	/// Response of 'tokeninfo'.
	/// </summary>
	struct TokenInfoResponse
	{
		public Exception error;
		public string audience;
		public string scope;
		public string userId;
		public int expiresIn;
		public string email;

		public TokenInfoResponse(Dictionary<string, object> json)
		{
			error = null;
			audience = null;
			scope = null;
			userId = null;
			expiresIn = 0;
			email = null;

			if (json.ContainsKey("error"))
			{
				error = GetError(json);
			}
			else
			{
				if (json.ContainsKey("audience"))
					audience = json["audience"] as string;
				if (json.ContainsKey("scope"))
					scope = json["scope"] as string;
				if (json.ContainsKey("user_id"))
					userId = json["user_id"] as string;
				if (json.ContainsKey("expires_in"))
					expiresIn = (int)json["expires_in"];
				if (json.ContainsKey("email"))
					email = json["email"] as string;
			}
		}
	}

	/// <summary>
	/// Validate the token and get informations.
	/// </summary>
	/// <param name="accessToken">Access token.</param>
	/// <returns>TokenInfoResponse or Exception for error</returns>
	static IEnumerator ValidateToken(string accessToken)
	{
		var request = new UnityWebRequest(
			"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=" + accessToken);

		var response = request.GetResponse();
		while (!response.isDone)
			yield return null;

		if (response.error != null)
		{
			yield return response.error;
			yield break;
		}

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "TokenInfo response parsing failed.");
			yield break;
		}

		yield return new TokenInfoResponse(json);
	}

	/// <summary>
	/// Response of 'revoke'.
	/// </summary>
	struct RevokeResponse
	{
		public Exception error;

		public RevokeResponse(Dictionary<string, object> json)
		{
			if (json.ContainsKey("error"))
				error = GetError(json);
			else
				error = null;
		}
	}

	/// <summary>
	/// Revoke a access token.
	/// </summary>
	/// <param name="token">Access token.</param>
	/// <returns>RevokeResponse or Exception for error.</returns>
	static IEnumerator RevokeToken(string token)
	{
		var request = new UnityWebRequest(
			"https://accounts.google.com/o/oauth2/revoke?token=" + token);

		var response = request.GetResponse();
		while (!response.isDone)
			yield return null;

		if (response.error != null)
		{
			yield return response.error;
			yield break;
		}

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null) // no response is success.
			yield return new RevokeResponse(); // error is null.
		else
			yield return new RevokeResponse(json);
	}
}
