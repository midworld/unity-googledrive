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
	class AsyncSuccess { }

	/// <summary>
	/// Check the async operation is done.
	/// </summary>
	/// <param name="async">Async operation.</param>
	/// <returns>True if the operation is done.</returns>
	public static bool IsDone(IEnumerator async)
	{
		return (async.Current is AsyncSuccess || async.Current is Exception);
	}
}
