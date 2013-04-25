using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JsonFx.Json;
using Midworld;

partial class GoogleDrive
{
	class Exception : System.Exception
	{
		public int Code { get; private set; }

		public Exception(int code, string message)
			: base(message)
		{
			Code = code;
		}
	}

	static Exception GetError(Dictionary<string, object> json)
	{
		if (json.ContainsKey("error"))
		{
			object error = json["error"];

			if (error is string)
				return new Exception(-1, error as string);
			else if (error is Dictionary<string, object>)
			{
				var errorObject = error as Dictionary<string, object>;

				int code = TryGet<int>(errorObject, "code");
				string message = TryGet<string>(errorObject, "message");

				return new Exception(code, message);
			}
			else
				return new Exception(-1, error.ToString());
		}
		else
			return null;
	}
}
