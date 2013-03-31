using System;
using System.Collections.Generic;

namespace GoogleDrive
{
	namespace Files
	{
		class File
		{
			public string kind { get; private set; }
			public string id { get; private set; }

			public string webContentLink { get; private set; }
			public string webViewLink { get; private set; }
			public string alternateLink { get; private set; }
			public string embedLink { get; private set; }
			public string iconLink { get; private set; }
			public string thumbnailLink { get; private set; }
			
			public string title { get; private set; }
			public string mimeType { get; private set; }
			public string description { get; private set; }

			public DateTime createdDate { get; private set; }
			public DateTime modifiedDate { get; private set; }

			// parents

			public string downloadUrl { get; private set; }

			public string originalFilename { get; private set; }
			public string fileExtension { get; private set; }
			public string md5Checksum { get; private set; }
			public long fileSize { get; private set; }
			public long quotaBytesUsed { get; private set; }
			public string[] ownerNames { get; private set; }

			T GetOrNull<T>(Dictionary<string, object> dict, string key)
			{
				object v;
				if (dict.TryGetValue(key, out v) && v is T)
					return (T)v;
				else
					return default(T);
			}

			public File(Dictionary<string, object> metadata)
			{
				kind = GetOrNull<string>(metadata, "kind");
				id = GetOrNull<string>(metadata, "id");
				
				webContentLink = GetOrNull<string>(metadata, "webContentLink");
				alternateLink = GetOrNull<string>(metadata, "alternateLink");
				embedLink = GetOrNull<string>(metadata, "embedLink");
				iconLink = GetOrNull<string>(metadata, "iconLink");
				thumbnailLink = GetOrNull<string>(metadata, "thumbnailLink");

				title = GetOrNull<string>(metadata, "title");
				mimeType = GetOrNull<string>(metadata, "mimeType");
				description = GetOrNull<string>(metadata, "description");

				try { createdDate = DateTime.Parse(GetOrNull<string>(metadata, "createdDate")); }
				catch(Exception){ }

				try { modifiedDate = DateTime.Parse(GetOrNull<string>(metadata, "modifiedDate")); }
				catch(Exception){ }

				// parents

				downloadUrl = GetOrNull<string>(metadata, "downloadUrl");

				originalFilename = GetOrNull<string>(metadata, "originalFilename");
				fileExtension = GetOrNull<string>(metadata, "fileExtension");
				md5Checksum = GetOrNull<string>(metadata, "md5Checksum");

				try { fileSize = long.Parse(GetOrNull<string>(metadata, "fileSize")); }
				catch(Exception){ }

				try { quotaBytesUsed = long.Parse(GetOrNull<string>(metadata, "quotaBytesUsed")); }
				catch(Exception){ }

				ownerNames = GetOrNull<string[]>(metadata, "ownerNames");
			}

			public override string ToString()
			{
				return string.Format("{0} {1} (mime-type:{2} id:{3})", kind, title, mimeType, id);
			}
		}
	}
}
