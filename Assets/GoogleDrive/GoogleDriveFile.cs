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
	/// <example>
	/// Make a file with data.
	/// <code>
	/// var file = new GoogleDrive.File(new Dictionary<string, object>
	///	{
	///		{ "title", "text_file.txt" },
	///		{ "mimeType", "text/plain" },
	///		{ "description", "This is a text file." }
	///	});
	/// </code>
	/// </example>
	public class File
	{
		/// <summary>
		/// The ID of the file.
		/// </summary>
		public string ID { get; private set; }

		/// <summary>
		/// File title.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// File MIME type. It must be set.
		/// </summary>
		/// <example>
		/// <para>text/plain: text.</para>
		/// <para>image/png: PNG image data(binary).</para>
		/// <para>application/json: JSON string.</para>
		/// <para>application/octet-stream: Binary data.</para>
		/// <para>and more.</para>
		/// </example>
		public string MimeType { get; set; }

		/// <summary>
		/// File description. null is default.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Created date.
		/// </summary>
		public DateTime CreatedDate { get; private set; }

		/// <summary>
		/// Last time this file was modified.
		/// </summary>
		public DateTime ModifiedDate { get; private set; }

		/// <summary>
		/// A link to the file's thumbnail. It can be null.
		/// </summary>
		public string ThumbnailLink { get; private set; }

		/// <summary>
		/// Download URL. It can be null such as folders.
		/// </summary>
		public string DownloadUrl { get; private set; }

		/// <summary>
		/// MD5 Checksum. It can be null.
		/// </summary>
		public string MD5Checksum { get; private set; }

		/// <summary>
		/// The size of the file in bytes. 0 is default(folders or file has no content).
		/// </summary>
		public long FileSize { get; private set; }

		/// <summary>
		/// Parents ID List. If it is empty then the file is in the root folder.
		/// </summary>
		public List<string> Parents { get; set; }

		/// <summary>
		/// Is this file a folder?
		/// </summary>
		public bool IsFolder
		{
			get { return MimeType == "application/vnd.google-apps.folder"; }
		}

		/// <summary>
		/// Make a file with data.
		/// </summary>
		/// <param name="metadata">JSON data.</param>
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

			string fileSize = TryGet<string>(metadata, "fileSize");
			if (fileSize != null)
				FileSize = long.Parse(fileSize);

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

		/// <summary>
		/// Generate JSON data of this file.
		/// </summary>
		/// <returns>JSON data.</returns>
		public Dictionary<string, object> ToJSON()
		{
			var json = new Dictionary<string, object>();
			
			json["title"] = Title;
			json["mimeType"] = MimeType;
			json["description"] = Description;

			var parents = new Dictionary<string, object>[Parents.Count];
			for (int i = 0; i < Parents.Count; i++)
			{
				parents[i] = new Dictionary<string, object>();
				parents[i]["id"] = Parents[i];
			}
			json["parents"] = parents;

			return json;
		}

		/// <summary>
		/// File information.
		/// </summary>
		/// <returns>Dump string.</returns>
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
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

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
	/// Get all files.
	/// </summary>
	/// <returns>AsyncSuccess with List&lt;File&gt; or Exception for error.</returns>
	/// <example>
	/// Get all files that you can see.
	/// <code>
	/// var listFiles = drive.ListAllFiles();
	/// yield return StartCoroutine(listFiles);
	/// var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	/// </code>
	/// </example>
	public IEnumerator ListAllFiles()
	{
		var listFiles = ListFilesByQuery("");
		while (listFiles.MoveNext())
			yield return listFiles.Current;
	}

	/// <summary>
	/// Get files in a folder with Folder File.
	/// </summary>
	/// <param name="parentFolder">Folder File.</param>
	/// <returns>AsyncSuccess with List&lt;File&gt; or Exception for error.</returns>
	/// <example>
	/// Get all files located directly under AppData.
	/// <code>
	/// var listFiles = drive.ListFiles(drive.AppData);
	/// yield return StartCoroutine(listFiles);
	/// var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	/// </code>
	/// </example>
	public IEnumerator ListFiles(File parentFolder)
	{
		var listFiles = ListFilesByQuery(string.Format("'{0}' in parents", parentFolder.ID));
		while (listFiles.MoveNext())
			yield return listFiles.Current;
	}

	/// <summary>
	/// <para>Get files by a searching query.</para>
	/// <para>https://developers.google.com/drive/search-parameters</para>
	/// </summary>
	/// <param name="query">Query string.</param>
	/// <returns>AsyncSuccess with List&lt;File&gt; or Exception for error.</returns>
	/// <example>
	/// Search by title.
	/// <code>
	/// var listFiles = drive.ListFilesByQuery("title = 'some_title.txt'");
	/// yield return StartCoroutine(listFiles);
	/// var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	/// 
	/// if (files != null && files.Count > 0)
	///		do something;
	/// </code>
	/// </example>
	public IEnumerator ListFilesByQuery(string query)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

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
	/// <example>
	/// <code>
	/// var insert = drive.InsertFolder("new_folder_in_root");
	/// yield return StartCoroutine(insert);
	/// </code>
	/// </example>
	public IEnumerator InsertFolder(string title)
	{
		var insertFolder = InsertFolder(title, null);
		while (insertFolder.MoveNext())
			yield return insertFolder.Current;
	}

	/// <summary>
	/// Insert a folder to otehr folder.
	/// </summary>
	/// <param name="parentFolder">Parent folder.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// <code>
	/// var insert = drive.InsertFolder("new_folder_in_appdata", drive.AppData);
	/// yield return StartCoroutine(insert);
	/// </code>
	/// </example>
	public IEnumerator InsertFolder(string title, File parentFolder)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

		var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files");
		request.method = "POST";
		request.headers["Authorization"] = "Bearer " + AccessToken;
		request.headers["Content-Type"] = "application/json";
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data["title"] = title;
		data["mimeType"] = "application/vnd.google-apps.folder";
		if (parentFolder != null)
		{
			data["parents"] = new List<Dictionary<string, string>>
			{
				new Dictionary<string, string> 
				{
					{ "id", parentFolder.ID }
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
	/// Delete a file(or folder).
	/// </summary>
	/// <param name="file">File.</param>
	/// <returns>AsyncSuccess or Exception for error.</returns>
	/// <example>
	/// Delete all files.
	/// <code>
	/// var listFiles = drive.ListAllFiles();
	/// yield return StartCoroutine(listFiles);
	/// var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	/// 
	/// if (files != null)
	/// {
	///		for (int i = 0; i < files.Count; i++)
	///			yield return StartCoroutine(drive.DeleteFile(files[i]));
	/// }
	/// </code>
	/// </example>
	public IEnumerator DeleteFile(File file)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

		var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" + file.ID);
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

	/// <summary>
	/// <para>Update file's metadata.</para>
	/// <para>You can change Title, Description, MimeType and Parents.</para>
	/// </summary>
	/// <param name="file">Updated file.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// Rename 'a.txt' to 'b.txt'.
	/// <code>
	/// var listFiles = drive.ListFilesByQuery("title = 'a.txt'");
	/// yield return StartCoroutine(listFiles);
	/// var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	/// 
	/// if (files != null && files.Count > 0)
	///	{
	///		files[0].Title = "b.txt";
	///		yield return StartCoroutine(drive.UpdateFile(files[0]));
	///	}
	/// </code>
	/// </example>
	public IEnumerator UpdateFile(File file)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

		var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" + file.ID);
		request.method = "PUT";
		request.headers["Authorization"] = "Bearer " + AccessToken;
		request.headers["Content-Type"] = "application/json";

		string metadata = JsonWriter.Serialize(file.ToJSON());
		request.body = Encoding.UTF8.GetBytes(metadata);

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "UpdateFile response parsing failed.");
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
	/// Touch a file(or folder).
	/// </summary>
	/// <param name="file">File to touch.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// <code>
	/// StartCoroutine(drive.TouchFile(someFile));
	/// </code>
	/// </example>
	public IEnumerator TouchFile(File file)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

		var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" + 
			file.ID + "/touch");
		request.method = "POST";
		request.headers["Authorization"] = "Bearer " + AccessToken;
		request.body = new byte[0]; // with no data

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "TouchFile response parsing failed.");
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
	/// Duplicate a file in the same folder.
	/// </summary>
	/// <param name="file">File to duplicate.</param>
	/// <param name="newTitle">New filename.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// Copy a file in the same folder.
	/// <code>
	/// string newTitle = someFile.Title + "(2)";
	/// StartCoroutine(drive.DuplicateFile(someFile, newTitle));
	/// </code>
	/// </example>
	public IEnumerator DuplicateFile(File file, string newTitle)
	{
		File newFile = new File(file.ToJSON());
		newFile.Title = newTitle;

		var duplicate = DuplicateFile(file, newFile);
		while (duplicate.MoveNext())
			yield return duplicate.Current;
	}

	/// <summary>
	/// Duplicate a file in a specified folder.
	/// </summary>
	/// <param name="file">File to duplicate.</param>
	/// <param name="newTitle">New filename.</param>
	/// <param name="newParentFolder">
	///	New parent folder. If it is null then the new file will place in root folder.
	/// </param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// Copy a file to the root folder.
	/// <code>
	/// StartCoroutine(drive.DuplicateFile(someFile, someFile.Title, null));
	/// </code>
	/// </example>
	public IEnumerator DuplicateFile(File file, string newTitle, File newParentFolder)
	{
		File newFile = new File(file.ToJSON());
		newFile.Title = newTitle;

		// Set the new parent id.
		if (newParentFolder != null)
			newFile.Parents = new List<string> { newParentFolder.ID };
		else
			newFile.Parents = new List<string> { };

		var duplicate = DuplicateFile(file, newFile);
		while (duplicate.MoveNext())
			yield return duplicate.Current;
	}
	
	/// <summary>
	/// Duplicate a file.
	/// </summary>
	/// <param name="file">File to duplicate.</param>
	/// <param name="newFile">New file data.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// Copy 'someFile' to 'newFile'.
	/// <code>
	/// var newFile = new GoogleDrive.File(new Dictionary<string, object>
	///	{
	///		{ "title", someFile.Title + "(2)" },
	///		{ "mimeType", someFile.MimeType },
	///	});
	///	newFile.Parents = new List<string> { newParentFolder.ID };
	///	
	/// StartCoroutine(drive.DuplicateFile(someFile, newFile));
	/// </code>
	/// </example>
	IEnumerator DuplicateFile(File file, File newFile)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

		var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" + 
			file.ID + "/copy");
		request.method = "POST";
		request.headers["Authorization"] = "Bearer " + AccessToken;
		request.headers["Content-Type"] = "application/json";

		string metadata = JsonWriter.Serialize(newFile.ToJSON());
		request.body = Encoding.UTF8.GetBytes(metadata);

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		JsonReader reader = new JsonReader(response.text);
		var json = reader.Deserialize<Dictionary<string, object>>();

		if (json == null)
		{
			yield return new Exception(-1, "DuplicateFile response parsing failed.");
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
	/// Upload a file to the root folder.
	/// </summary>
	/// <param name="title">Filename.</param>
	/// <param name="mimeType">Content type.</param>
	/// <param name="data">Data.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// <code>
	/// var bytes = Encoding.UTF8.GetBytes("world!");
	/// StartCoroutine(drive.UploadFile("hello.txt", "text/plain", bytes));
	/// </code>
	/// </example>
	public IEnumerator UploadFile(string title, string mimeType, byte[] data)
	{
		var upload = UploadFile(title, mimeType, null, data);
		while (upload.MoveNext())
			yield return upload.Current;
	}

	/// <summary>
	/// Upload a file.
	/// </summary>
	/// <param name="title">Filename.</param>
	/// <param name="mimeType">Content type.</param>
	/// <param name="parentFolder">Parent folder. null is the root folder.</param>
	/// <param name="data">Data.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// Upload a file in AppData.
	/// <code>
	/// var bytes = Encoding.UTF8.GetBytes("world!");
	/// StartCoroutine(drive.UploadFile("hello.txt", "text/plain", drive.AppData, bytes));
	/// </code>
	/// </example>
	public IEnumerator UploadFile(string title, string mimeType, File parentFolder, byte[] data)
	{
		File file = new File(new Dictionary<string, object>
		{
			{ "title", title },
			{ "mimeType", mimeType },
		});

		if (parentFolder != null)
			file.Parents = new List<string> { parentFolder.ID };

		var upload = UploadFile(file, data);
		while (upload.MoveNext())
			yield return upload.Current;
	}

	/// <summary>
	/// Upload a file.
	/// </summary>
	/// <param name="file">File metadata.</param>
	/// <param name="data">Data.</param>
	/// <returns>AsyncSuccess with File or Exception for error.</returns>
	/// <example>
	/// Upload a file to the root folder.
	/// <code>
	/// var bytes = Encoding.UTF8.GetBytes("world!");
	/// 
	/// var file = new GoogleDrive.File(new Dictionary<string, object>
	///	{
	///		{ "title", "hello.txt" },
	///		{ "mimeType", "text/plain" },
	///	});
	///	
	/// StartCoroutine(drive.UploadFile(file, bytes));
	/// </code>
	/// Update the file content.
	/// <code>
	/// var listFiles = drive.ListFilesByQuery("title = 'a.txt'");
	/// yield return StartCoroutine(listFiles);
	/// 
	/// var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
	/// if (files != null && files.Count > 0)
	/// {
	///		var bytes = Encoding.UTF8.GetBytes("new content.");
	///		StartCoroutine(drive.UploadFile(files[0], bytes));
	/// }
	/// </code>
	/// </example>
	public IEnumerator UploadFile(File file, byte[] data)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

		string uploadUrl = null;

		// Start a resumable session.
		if (file.ID == null || file.ID == string.Empty)
		{
			var request = new UnityWebRequest(
				"https://www.googleapis.com/upload/drive/v2/files?uploadType=resumable");
			request.method = "POST";
			request.headers["Authorization"] = "Bearer " + AccessToken;
			request.headers["Content-Type"] = "application/json";
			request.headers["X-Upload-Content-Type"] = file.MimeType;
			request.headers["X-Upload-Content-Length"] = data.Length;

			string metadata = JsonWriter.Serialize(file.ToJSON());
			request.body = Encoding.UTF8.GetBytes(metadata);

			var response = new UnityWebResponse(request);
			while (!response.isDone)
				yield return null;

			if (response.statusCode != 200)
			{
				JsonReader reader = new JsonReader(response.text);
				var json = reader.Deserialize<Dictionary<string, object>>();

				if (json == null)
				{
					yield return new Exception(-1, "UploadFile response parsing failed.");
					yield break;
				}
				else if (json.ContainsKey("error"))
				{
					yield return GetError(json);
					yield break;
				}
			}

			// Save the resumable session URI.
			uploadUrl = response.headers["Location"] as string;
		}
		else
		{
			uploadUrl = "https://www.googleapis.com/upload/drive/v2/files/" + file.ID;
		}

		// Upload the file.
		{
			var request = new UnityWebRequest(uploadUrl);
			request.method = "PUT";
			request.headers["Authorization"] = "Bearer " + AccessToken;
			request.headers["Content-Type"] = "application/octet-stream"; // file.MimeType;
			request.body = data;

			var response = new UnityWebResponse(request);
			while (!response.isDone)
				yield return null;

			JsonReader reader = new JsonReader(response.text);
			var json = reader.Deserialize<Dictionary<string, object>>();

			if (json == null)
			{
				yield return new Exception(-1, "UploadFile response parsing failed.");
				yield break;
			}
			else if (json.ContainsKey("error"))
			{
				yield return GetError(json);
				yield break;
			}

			yield return new AsyncSuccess(new File(json));
		}
	}

	/// <summary>
	/// Download a file content.
	/// </summary>
	/// <param name="file">File.</param>
	/// <returns>AsyncSuccess with byte[] or Exception for error.</returns>
	/// <example>
	/// Download a file and print text.
	/// <code>
	/// var download = drive.DownloadFile(file);
	/// yield return StartCoroutine(download);
	/// 
	/// var data = GoogleDrive.GetResult<byte[]>(download);
	/// if (data != null)
	///		print(System.Text.Encoding.UTF8.GetString(data));
	/// </code>
	/// </example>
	public IEnumerator DownloadFile(File file)
	{
		if (file.DownloadUrl == null || file.DownloadUrl == string.Empty)
		{
			yield return new Exception(-1, "No download URL.");
			yield break;
		}

		var download = DownloadFile(file.DownloadUrl);
		while (download.MoveNext())
			yield return download.Current;
	}

	/// <summary>
	/// Download a file content.
	/// </summary>
	/// <param name="file">Download URL.</param>
	/// <returns>AsyncSuccess with byte[] or Exception for error.</returns>
	/// <example>
	/// Download the thumbnail image.
	/// <code>
	/// if (file.ThumbnailLink != null)
	/// {
	///		var download = drive.DownloadFile(file.ThumbnailLink);
	///		yield return StartCoroutine(drive);
	///		
	///		var data = GoogleDrive.GetResult<byte[]>(download);
	///		if (data != null)
	///			someTexture.LoadImage(data);
	///	}
	/// </code>
	/// </example>
	public IEnumerator DownloadFile(string url)
	{
		#region Check the access token is expired
		var check = CheckExpiration();
		while (check.MoveNext())
			yield return null;

		if (check.Current is Exception)
		{
			yield return check.Current;
			yield break;
		}
		#endregion

		var request = new UnityWebRequest(url);
		request.headers["Authorization"] = "Bearer " + AccessToken;

		var response = new UnityWebResponse(request);
		while (!response.isDone)
			yield return null;

		yield return new AsyncSuccess(response.bytes);
	}
}
