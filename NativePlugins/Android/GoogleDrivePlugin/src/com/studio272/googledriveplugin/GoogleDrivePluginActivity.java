package com.studio272.googledriveplugin;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.widget.Toast;

public class GoogleDrivePluginActivity extends Activity {
	static final String TAG = "Unity-GoogleDrivePlugin";
	static final String PREFS = "unitygoogledrive";
	
	static final int REQUEST_ACCOUNT_PICKER = 1;
	static final int REQUEST_AUTHORIZATION = 2;
	
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
	}
	
	@Override
	protected void onActivityResult(final int requestCode, final int resultCode, final Intent data) {
		GoogleDrivePlugin.onActivityResult(requestCode, resultCode, data, this);
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
