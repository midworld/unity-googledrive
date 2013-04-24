using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JsonFx.Json;
using Midworld;

partial class GoogleDrive
{
	static T TryGet<T>(Dictionary<string, object> dict, string key)
	{
		object v;
		if (dict.TryGetValue(key, out v) && v is T)
			return (T)v;
		else
			return default(T);
	}

	static string GetErrorString(Dictionary<string, object> json)
	{
		if (json.ContainsKey("error"))
		{
			object error = json["error"];

			if (error is string)
				return error as string;
			else if (error is Dictionary<string, object>)
			{
				var errorObject = error as Dictionary<string, object>;

				int code = TryGet<int>(errorObject, "code");
				string message = TryGet<string>(errorObject, "message");

				return message + "(" + code + ")";
			}
			else
				return error.ToString();
		}
		else
			return null;
	}
}
