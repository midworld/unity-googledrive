package com.studio272.googledriveplugin;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import com.google.api.client.extensions.android.http.AndroidHttp;
import com.google.api.client.googleapis.extensions.android.gms.auth.GoogleAccountCredential;
import com.google.api.client.googleapis.extensions.android.gms.auth.UserRecoverableAuthIOException;
import com.google.api.client.json.gson.GsonFactory;
import com.google.api.services.drive.Drive;
import com.google.api.services.drive.DriveScopes;
import com.google.api.services.drive.Drive.Files;
import com.google.api.services.drive.model.FileList;

import android.R.bool;
import android.accounts.AccountManager;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.content.SharedPreferences;
import android.util.Log;
import android.util.SparseArray;

public class GoogleDrivePlugin {
	static final String TAG = "Unity-GoogleDrivePlugin";
	static final String PREFS = "unitygoogledrive";
	
	static final int REQUEST_ACCOUNT_PICKER = 1;
	static final int REQUEST_AUTHORIZATION = 2;
	
	static Drive service;
	static GoogleAccountCredential credential;
	
	static Activity activity = null;
	
	public static void setUnityActivity(Activity unityActivity) {
		Log.d(TAG, "setUnityActivity: " + unityActivity);
		
		activity = unityActivity;
	}
	
	static Runnable authRejectionCallback = null;
	
	public static void setAuthRejectionCallback(Runnable callback) {
		Log.d(TAG, "setAuthRejectionCallback: " + callback);
		
		authRejectionCallback = callback;
	}
	
	static Runnable authSuccessCallback = null;
	static Runnable authFailureCallback = null;
	
	public static void auth(Runnable success, Runnable failure) {
		SharedPreferences prefs = activity.getSharedPreferences(PREFS, Activity.MODE_PRIVATE);
		String accountName = prefs.getString("accountName", null);
		
		Log.d(TAG, "auth with accountName: " + accountName);
		
		credential = GoogleAccountCredential.usingOAuth2(activity, DriveScopes.DRIVE);
		
		try {
			if (accountName == null)
				throw new NullPointerException();
			
			credential.setSelectedAccountName(accountName);
			service = getDriveService(credential);
			
			Log.d(TAG, "google drive service: " + service);
			
			activity.runOnUiThread(success);
		} catch (Exception e) {
			authSuccessCallback = success;
			authFailureCallback = failure;
			
			pickAccount();
		}
	}
	
	public static void clearSelectedAccountName() {
		SharedPreferences prefs = activity.getSharedPreferences(PREFS, Activity.MODE_PRIVATE);
		if (prefs.contains("accountName")) {
			prefs.edit().remove("accountName").commit();
		}
	}
	
	static SparseArray<Object> results = new SparseArray<Object>();
	static SparseArray<String> errors = new SparseArray<String>();
	static int resultIndex = 0;
	
	public static Object getResult(int index) {
		Object result = results.get(index);
		
		Log.d(TAG, "getResult index: " + index + " result: " + result);
		
		if (result != null) {
			results.remove(index);
		}
		
		return result;
	}
	
	public static String getError(int index) {
		String error = errors.get(index);
		
		Log.d(TAG, "getError index: " + index + " error: " + error);
		
		if (error != null) {
			errors.remove(index);
			return error;
		} else {
			return "";
		}
	}
	
	public static int list(final Runnable callback) {
		Log.d(TAG, "list() started...");
		
		final int currentId = resultIndex++;
		
		Thread thread = new Thread(new Runnable() {
			@Override
			public void run() {
				List<com.google.api.services.drive.model.File> result = 
						new ArrayList<com.google.api.services.drive.model.File>();
				
				try {
					Files.List request = service.files().list();
					
					do {
						FileList files = request.execute();
							
						result.addAll(files.getItems());
						request.setPageToken(files.getNextPageToken());
					} while (request.getPageToken() != null &&
							request.getPageToken().length() > 0);
					
					String[] array = new String[result.size()];
					
					for (int i = 0; i < result.size(); i++) {
						com.google.api.services.drive.model.File file = result.get(i);
						array[i] = file.getTitle();
					}
					
					Log.d(TAG, "list() length: " + array.length);
					
					results.append(currentId, array);
				} catch (UserRecoverableAuthIOException e) {
					Log.d(TAG, "UserRecoverableAuthIOException: " + e);
					
					authorization(e.getIntent());
					
					errors.append(currentId, "UserRecoverableAuthIOException: " + e);
				} catch (Exception e) {
					Log.d(TAG, "Exception: " + e);
					e.printStackTrace();
					
					errors.append(currentId, "Exception: " + e);
				}
				
				activity.runOnUiThread(callback);
			}
		});
		thread.start();
		
		return currentId;
	}
	
	// -------------------------------
	
	static SparseArray<Intent> requests = new SparseArray<Intent>();
	
	static void onActivityResult(final int requestCode, final int resultCode, final Intent data, final Activity holder) {
		switch (requestCode) {
		case REQUEST_ACCOUNT_PICKER:
			boolean success = false;
			
			if (resultCode == Activity.RESULT_OK && data != null && data.getExtras() != null) {
				String accountName = data.getStringExtra(AccountManager.KEY_ACCOUNT_NAME);
				if (accountName != null) {
					SharedPreferences prefs = activity.getSharedPreferences(PREFS, Activity.MODE_PRIVATE);
					prefs.edit().putString("accountName", accountName).commit();
					
					Log.d(TAG, "selected accountName: " + accountName);
					
					credential.setSelectedAccountName(accountName);
					service = getDriveService(credential);
					
					Log.d(TAG, "google drive service: " + service);
					
					success = true;
					
					if (authSuccessCallback != null) {
						authSuccessCallback.run();
					}
				}
			}
			
			if (!success && authFailureCallback != null) {
				authFailureCallback.run();
			}
			
			authSuccessCallback = null;
			authFailureCallback = null;
			break;
		case REQUEST_AUTHORIZATION:
			if (resultCode == Activity.RESULT_OK) {
				// do something such as uploading
				//list();
			} else {
				// 권환 요청에서 취소한 경우임! 고치자
				//pickAccount();
			}
			break;
		}
		
		holder.finish();
	}
	
	private static void pickAccount() {
		requests.append(REQUEST_ACCOUNT_PICKER, credential.newChooseAccountIntent());
		
		Intent intent = new Intent(activity, GoogleDrivePluginActivity.class);
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		intent.putExtra("requestCode", REQUEST_ACCOUNT_PICKER);
		activity.startActivity(intent);
	}
	
	private static void authorization(Intent newIntent) {
		requests.append(REQUEST_AUTHORIZATION, newIntent);
		
		Intent intent = new Intent(activity, GoogleDrivePluginActivity.class);
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		intent.putExtra("requestCode", REQUEST_AUTHORIZATION);
		activity.startActivity(intent);
	}
	
	// -------------------------------
	
	private static Drive getDriveService(GoogleAccountCredential credential) {
		return new Drive.Builder(AndroidHttp.newCompatibleTransport(), new GsonFactory(), credential)
				.build();
	}
	
	// -------------------------------
	
	public static void Show(Activity activity) {
		Log.d(TAG, "@Show activity: " + activity);
		
		Intent intent = new Intent(activity, GoogleDrivePluginActivity.class);
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		//activity.startActivity(intent);
		activity.startActivityForResult(intent, -1);
	}
	
	public static void Test(Activity activity, String message) {
		Log.d(TAG, "@Test message: " + message);
		Log.d(TAG, "@Test activity: " + activity);
		
		new AlertDialog.Builder(activity)
			.setTitle("Test")
			.setMessage(message)
			.show();
	}
	
	public static void Test2(Activity activity) {
		Log.d(TAG, "@Test2 activity: " + activity);
		
		credential = GoogleAccountCredential.usingOAuth2(activity, DriveScopes.DRIVE);
		activity.startActivityForResult(credential.newChooseAccountIntent(), -1);
	}
}
