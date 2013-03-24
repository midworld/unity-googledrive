package com.studio272.googledriveplugin;

import java.io.IOException;

import com.google.api.client.extensions.android.http.AndroidHttp;
import com.google.api.client.googleapis.extensions.android.gms.auth.GoogleAccountCredential;
import com.google.api.client.googleapis.extensions.android.gms.auth.UserRecoverableAuthIOException;
import com.google.api.client.http.FileContent;
import com.google.api.client.json.gson.GsonFactory;
import com.google.api.services.drive.Drive;
import com.google.api.services.drive.DriveScopes;
import com.google.api.services.drive.model.File;

import android.accounts.AccountManager;
import android.app.Activity;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Bundle;
import android.util.Log;
import android.widget.TextView;
import android.widget.Toast;

public class GoogleDrivePluginActivity extends Activity {
	static final String TAG = "Unity-GoogleDrivePlugin";
	static final String PREFS = "unitygoogledrive";
	
	static final int REQUEST_ACCOUNT_PICKER = 1;
	static final int REQUEST_AUTHORIZATION = 2;
	
	private static Drive service;
	private GoogleAccountCredential credential;
	
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		
		Intent intent = getIntent();
		int requestCode = intent.getIntExtra("requestCode", -1);
		
		Intent newIntent = GoogleDrivePlugin.requests.get(requestCode);
		if (newIntent != null) {
			GoogleDrivePlugin.requests.remove(requestCode);
			startActivityForResult(newIntent, requestCode);
		}
		
		Log.d(TAG, "requestCode: " + requestCode + " intent: " + newIntent);
	}
	
	@Override
	protected void onActivityResult(final int requestCode, final int resultCode, final Intent data) {
		Log.d(TAG, "onActivityResult: " + requestCode + " resultCode: " + resultCode);
		
		GoogleDrivePlugin.onActivityResult(requestCode, resultCode, data, this);
		
		/*switch (requestCode) {
		case REQUEST_ACCOUNT_PICKER:
			if (resultCode == RESULT_OK && data != null && data.getExtras() != null) {
				String accountName = data.getStringExtra(AccountManager.KEY_ACCOUNT_NAME);
				if (accountName != null) {
					credential.setSelectedAccountName(accountName);
					service = getDriveService(credential);
					Log.d(TAG, "REQUEST_ACCOUNT_PICKER");
					
					//finish();
					saveFileToDrive(Uri.fromFile(
							new java.io.File("/data/data/com.studio272.unitydrivetest/files/screenshot.png")));
				}
			} else {
				setResult(RESULT_CANCELED);
				finish();
			}
			break;
		case REQUEST_AUTHORIZATION:
			if (resultCode == Activity.RESULT_OK) {
				Log.d(TAG, "REQUEST_AUTHORIZATION");
			} else {
				startActivityForResult(credential.newChooseAccountIntent(), REQUEST_ACCOUNT_PICKER);
			}
			break;
		}*/
	}
		
	private Drive getDriveService(GoogleAccountCredential credential) {
		return new Drive.Builder(AndroidHttp.newCompatibleTransport(), new GsonFactory(), credential)
				.build();
	}
	
	private void saveFileToDrive(final Uri fileUri) {
		Thread t = new Thread(new Runnable() {
			@Override
			public void run() {
				try {
					// File's binary content
					java.io.File fileContent = new java.io.File(fileUri.getPath());
					FileContent mediaContent = new FileContent("image/png", fileContent);

					// File's metadata.
					File body = new File();
					body.setTitle(fileContent.getName());
					body.setMimeType("image/png");

					File file = service.files().insert(body, mediaContent).execute();
					if (file != null) {
						showToast("Photo uploaded: " + file.getTitle());
						finish();
					}
				} catch (UserRecoverableAuthIOException e) {
					startActivityForResult(e.getIntent(), REQUEST_AUTHORIZATION);
				} catch (IOException e) {
					e.printStackTrace();
				}
			}
		});
		t.start();
	}
	
	public void showToast(final String toast) {
		runOnUiThread(new Runnable() {
			@Override
			public void run() {
				Toast.makeText(getApplicationContext(), toast, Toast.LENGTH_SHORT).show();
			}
		});
	}
}
