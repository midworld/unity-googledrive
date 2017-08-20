Google Drive for Unity3D
========================

Introduction
------------

Google Drive for Unity3D plugin.

You can upload files, explore, download on the Google Drive storage.

This plugin supports PC(Windows and Mac), Android and iOS.

* Google Drive supports 'App Data' that is only accessible by your application. 
See <https://developers.google.com/drive/appdata>

Installation
------------

### Step 1: Enable the Google Drive API.

* PC and iOS: <https://developers.google.com/drive/quickstart-ios> (Step 1)

* Android: <https://developers.google.com/drive/quickstart-android> (Step 1, 2)

### Step 2: Type Your Client ID and Secret to DriveTest.cs.

In the line 40: 

```c#
drive.ClientID = "YOUR CLIENT ID";
drive.ClientSecret = "YOUR CLIENT SECRET";
```

* PC and iOS can use same Client ID and secret.

* Android doesn't need Client ID and secret.

### Step 3: Run the Sample Scene 'DriveTest'.

All done!

Doxygen Docs
------------

[here](http://midworld.github.io/unity-googledrive/)

Demo Downloads
--------------

* Windows Binary: [unitydrivetest_win.zip](http://midworld.github.io/unity-googledrive/unitydrivetest_win.zip)

* Android APK: [unitydrivetest.apk](http://midworld.github.io/unity-googledrive/unitydrivetest.apk)

Sample Code
-----------

```c#
var drive = new GoogleDrive();
drive.ClientID = "YOUR CLIENT ID";
drive.ClientSecret = "YOUR CLIENT SECRET";

// Request authorization.
var authorization = drive.Authorize();
yield return StartCoroutine(authorization);

if (authorization.Current is Exception)
{
   Debug.LogWarning(authorization.Current as Exception);
   yield break;
}

// Authorization succeeded.
Debug.Log("User Account: " + drive.UserAccount);

// Upload a text file.
var bytes = Encoding.UTF8.GetBytes("world!");
yield return StartCoroutine(drive.UploadFile("hello.txt", "text/plain", bytes));

// Get all files.
var listFiles = drive.ListAllFiles();
yield return StartCoroutine(listFiles);

var files = GoogleDrive.GetResult<List<GoogleDrive.File>>(listFiles);
if (files != null)
{
   foreach (var file in files)
   {
       // Download a text file and print.
       if (file.Title.EndsWith(".txt"))
       {
           var download = drive.DownloadFile(file);
           yield return StartCoroutine(download);
           
           var data = GoogleDrive.GetResult<byte[]>(download);
           Debug.Log(System.Text.Encoding.UTF8.GetString(data));
       }
   }
}
```

Work with 'App Data':

```c#
// Upload score in 'AppData'.
int score = 10000;
var bytes = Encoding.UTF8.GetBytes(score.ToString());

// User cannot see 'score.txt'. Only your app can see this file.
StartCoroutine(drive.UploadFile("score.txt", "text/plain", drive.AppData, bytes));
```

License
-------

Apache License 2.0
