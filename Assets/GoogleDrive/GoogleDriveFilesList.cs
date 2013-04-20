using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GoogleDriveOld
{
	namespace Files
	{
		class List : Ticket
		{
			int id = -1;

			public List<File> items { get; private set; }

			public Exception error { get; private set; }

			public List() : this(-1) { }

			public List(int maxResults)
			{
				items = new List<File>(); 
				
#if UNITY_ANDROID
				Auth.EnsureActivitySet();
#endif

				AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
				id = pluginClass.CallStatic<int>("list", new object[] { 
					maxResults, (AndroidJavaRunnable)OnProgress, (AndroidJavaRunnable)OnComplete });
				pluginClass.Dispose();
			}

			void OnProgress()
			{
				AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
				string[] results = pluginClass.CallStatic<string[]>("getResult", new object[] { id });
				pluginClass.Dispose();

				if (results != null)
				{
					for (int i = 0; i < results.Length; i++)
					{
						try
						{
							JsonFx.Json.JsonReader reader = new JsonFx.Json.JsonReader(results[i]);
							var o = reader.Deserialize<Dictionary<string, object>>();
							var items = o["items"] as object[];

							this.items.Capacity += items.Length;

							foreach (object item in items) 
							{
								this.items.Add(new File(item as Dictionary<string, object>));
							}
						}
						catch (Exception e)
						{
							Debug.LogError(e);
						}
					}
				}
			}

			void OnComplete()
			{
				AndroidJavaClass pluginClass = new AndroidJavaClass("com.studio272.googledriveplugin.GoogleDrivePlugin");
				string errorSting = pluginClass.CallStatic<string>("getError", new object[] { id });
				pluginClass.Dispose();

				if (errorSting != null)
				{
					if (errorSting == "UserRecoverableAuthIOException")
					{
						error = new AuthException(errorSting);
					}
					else
					{
						error = new Exception(errorSting);
					}
				}

				//for (int i = 0; i < items.Count; i++)
				//{
				//    Debug.Log(string.Format("[{0}] {1}", i, items[i]));
				//}

				isDone = true;
			}
		}
	}
}
