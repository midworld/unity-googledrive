using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JsonFx.Json;
using Midworld;

partial class GoogleDrive
{
	/// <summary>
	/// File class.
	/// </summary>
	public class File
	{
		public string ID { get; private set; }

		public string Title { get; private set; }
		public string MimeType { get; private set; }
		public string Description { get; private set; }

		public DateTime CreatedDate { get; private set; }
		public DateTime ModifiedDate { get; private set; }

		public string ThumbnailLink { get; private set; }
		public string DownloadUrl { get; private set; }

		public string MD5Checksum { get; private set; }
		public long FileSize { get; private set; }

		public List<string> Parents { get; private set; }

		public bool IsFolder
		{
			get { return MimeType == "application/vnd.google-apps.folder"; }
		}

		public File(Dictionary<string, object> metadata)
		{
			ID = TryGet<string>(metadata, "id");

			Title = TryGet<string>(metadata, "title");
			MimeType = TryGet<string>(metadata, "mimeType");
			Description = TryGet<string>(metadata, "description");

			DateTime createdDate;
			DateTime.TryParse(TryGet<string>(metadata, "createdDate"), out createdDate);
			CreatedDate = createdDate;

			DateTime modifiedDate;
			DateTime.TryParse(TryGet<string>(metadata, "modifiedDate"), out modifiedDate);
			ModifiedDate = modifiedDate;

			ThumbnailLink = TryGet<string>(metadata, "thumbnailLink");
			DownloadUrl = TryGet<string>(metadata, "downloadUrl");

			MD5Checksum = TryGet<string>(metadata, "md5Checksum");
			FileSize = TryGet<long>(metadata, "fileSize");

			// Get parent folders.
			Parents = new List<string>();

			if (metadata.ContainsKey("parents") &&
				metadata["parents"] is Dictionary<string, object>[])
			{
				var parents = metadata["parents"] as Dictionary<string, object>[];
				foreach (var parent in parents)
				{
					if (parent.ContainsKey("id"))
						Parents.Add(parent["id"] as string);
				}
			}
		}

		public override string ToString()
		{
			return string.Format("{{ title: {0}, mimeType: {1}, fileSize: {2}, id: {3}, parents: [{4}] }}",
				Title, MimeType, FileSize, ID, string.Join(", ", Parents.ToArray()));
		}
	}

	/// <summary>
	/// <para>AppData folder.</para>
	/// <para>https://developers.google.com/drive/appdata</para>
	/// </summary>
	public File AppData { get; private set; }

	/// <summary>
	/// Get a file metadata.
	/// </summary>
	/// <param name="id">File ID.</param>
	/// <returns>AsyncSuccess with a File or Exception for error.</returns>
	public IEnumerator GetFile(string id)
	{
		var request = new UnityWebRequest(
			new Uri("https://www.googleapis.com/drive/v2/files/" + id));
		request.headers["Authorization"] = "Bearer " + AccessToken;

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "GetFile response parsing failed.");
			yield break;
		}
		else if (json.ContainsKey("error"))
		{
			yield return GetError(json);
			yield break;
		}

		yield return new AsyncSuccess(new File(json));
	}

	/// <summary>
	/// Get files in a folder with ID.
	/// </summary>
	/// <param name="parentId">Folder ID.</param>
	/// <returns>AsyncSuccess with List&lt;File&gt; or Exception for error.</returns>
	public IEnumerator ListFiles(string parentId)
	{
		var listFiles = ListFilesByQueary(string.Format("'{0}' in parents", parentId));
		while (listFiles.MoveNext())
			yield return listFiles.Current;
	}

	/// <summary>
	/// <para>Get files by a searching query.</para>
	/// <para>https://developers.google.com/drive/search-parameters</para>
	/// </summary>
	/// <param name="query">Query string.</param>
	/// <returns>AsyncSuccess with List&lt;File&gt; or Exception for error.</returns>
	public IEnumerator ListFilesByQueary(string query)
	{
		var request = new UnityWebRequest(
			new Uri("https://www.googleapis.com/drive/v2/files?q=" + query));
		request.headers["Authorization"] = "Bearer " + AccessToken;

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "ListFiles response parsing failed.");
			yield break;
		}
		else if (json.ContainsKey("error"))
		{
			yield return GetError(json);
			yield break;
		}

		// parsing
		var results = new List<File>();

		if (json.ContainsKey("items") &&
			json["items"] is Dictionary<string, object>[])
		{
			var items = json["items"] as Dictionary<string, object>[];
			foreach (var item in items)
			{
				results.Add(new File(item));
			}
		}

		yield return new AsyncSuccess(results);
	}

	/// <summary>
	/// Insert a folder to the root folder.
	/// </summary>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	public IEnumerator InsertFolder(string title)
	{
		var insertFolder = InsertFolder(title, null);
		while (insertFolder.MoveNext())
			yield return insertFolder.Current;
	}

	/// <summary>
	/// Insert a folder.
	/// </summary>
	/// <param name="parentId">Parent folder ID.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	public IEnumerator InsertFolder(string title, string parentId)
	{
		var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files");
		request.method = "POST";
		request.headers["Authorization"] = "Bearer " + AccessToken;
		request.headers["Content-Type"] = "application/json";
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data["title"] = title;
		data["mimeType"] = "application/vnd.google-apps.folder";
		if (parentId != null)
		{
			data["parents"] = new List<Dictionary<string, string>>
			{
				new Dictionary<string, string> 
				{
					{ "id", parentId }
				},
			};
		}
		request.body = Encoding.UTF8.GetBytes(JsonWriter.Serialize(data));

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "InsertFolder response parsing failed.");
			yield break;
		}
		else if (json.ContainsKey("error"))
		{
			yield return GetError(json);
			yield break;
		}

		yield return new AsyncSuccess(new File(json));
	}

	/// <summary>
	/// Delete a file(folder) with ID.
	/// </summary>
	/// <param name="id">File's ID.</param>
	/// <returns>AsyncSuccess or Exception for error.</returns>
	public IEnumerator DeleteFile(string id)
	{
		var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" + id);
		request.method = "DELETE";
		request.headers["Authorization"] = "Bearer " + AccessToken;

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		// If successful, empty response.
		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();
		
		if (json != null && json.ContainsKey("error"))
		{
			yield return GetError(json);
			yield break;
		}

		yield return new AsyncSuccess(); 
	}
}
