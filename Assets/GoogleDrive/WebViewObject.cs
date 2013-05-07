using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Unity3D - iOS WebView Object(Only for iOS).
/// </summary>
public class WebViewObject : MonoBehaviour
{
#if !UNITY_EDITOR && UNITY_IPHONE
	/// <summary>
	/// URL to open.
	/// </summary>
	public Uri url = new Uri("about:blank");

	/// <summary>
	/// Authorization code.
	/// </summary>
	public string token = null;

	/// <summary>
	/// WebView closed by user touch.
	/// </summary>
	public bool cancelled = false;

	IntPtr webView;

	bool opened = false;

	void Start()
	{
		string escaped = Uri.EscapeUriString(url.ToString());

		webView = _WebViewPlugin_Init(gameObject.name);
		_WebViewPlugin_SetMargins(webView, 20, 20, 20, 20);
		_WebViewPlugin_LoadURL(webView, escaped);
		_WebViewPlugin_SetVisibility(webView, 1);

		opened = true;

		StartCoroutine(CheckTitle());
	}

	/// <summary>
	/// Authorization result will be shown in title.
	/// </summary>
	IEnumerator CheckTitle()
	{
		while (opened) 
		{
			_WebViewPlugin_EvaluateJS(webView, "(function () { return document.title; })()");

			yield return new WaitForSeconds(0.1f);
		}
	}

	void CallFromJS(string message)
	{
		//Debug.Log("CallFromJS: " + message);

		if (message.StartsWith("Success code=")) 
		{
			string token = message.Substring(13);

			Debug.Log("token = " + token);

			// Run on next frame.
			StartCoroutine(Close(token));
		}
		else if (message == "close")
		{
			opened = false;

			Debug.Log("webview closed.");

			// Run on next frame.
			StartCoroutine(Close(null));
		}
	}

	IEnumerator Close(string got)
	{
		yield return new WaitForSeconds(0);

		_WebViewPlugin_Destroy(webView);

		if (got == null)
			cancelled = true;
		else
			token = got;
	}

	[DllImport ("__Internal")]
	static extern IntPtr _WebViewPlugin_Init(string gameObjectName);

	[DllImport ("__Internal")]
	static extern void _WebViewPlugin_Destroy(IntPtr instance);

	[DllImport ("__Internal")]
	static extern void _WebViewPlugin_SetMargins(
		IntPtr instance, int left, int top, int right, int bottom);

	[DllImport ("__Internal")]
	static extern void _WebViewPlugin_SetVisibility(IntPtr instance, int visibility);

	[DllImport ("__Internal")]
	static extern void _WebViewPlugin_LoadURL(IntPtr instance, string url);

	[DllImport ("__Internal")]
	static extern void _WebViewPlugin_EvaluateJS(IntPtr instance, string url);
#endif
}
