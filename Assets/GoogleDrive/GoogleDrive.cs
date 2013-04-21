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
}
