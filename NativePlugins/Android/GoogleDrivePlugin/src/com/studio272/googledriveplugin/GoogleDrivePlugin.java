package com.studio272.googledriveplugin;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import com.google.android.gms.auth.GoogleAuthException;
import com.google.android.gms.auth.UserRecoverableAuthException;
import com.google.api.client.extensions.android.http.AndroidHttp;
import com.google.api.client.googleapis.extensions.android.gms.auth.GoogleAccountCredential;
import com.google.api.client.googleapis.extensions.android.gms.auth.UserRecoverableAuthIOException;
import com.google.api.client.googleapis.json.GoogleJsonResponseException;
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
	
	static Runnable authSuccessCallback = null;
	static Runnable authFailureCallback = null;
	
	public static void auth(Runnable success, Runnable failure) {
		authSuccessCallback = success;
		authFailureCallback = failure;

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
			
			checkAuthorized();
		} catch (Exception e) {
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
					request.setMaxResults(50);
					
					StringBuilder sb = new StringBuilder();
					
					do {
						try {
							FileList files = request.execute();
								
							result.addAll(files.getItems());
							request.setPageToken(files.getNextPageToken());
							
							Log.d(TAG, "got results! " + files.toString());
							
							sb.append(files.toString());
							sb.append('\n');
						} catch (GoogleJsonResponseException e) {
							Log.d(TAG, "error: " + e);
						}
					} while (request.getPageToken() != null &&
							request.getPageToken().length() > 0);
					
					String[] array = new String[result.size()];
					
					/*for (int i = 0; i < result.size(); i++) {
						com.google.api.services.drive.model.File file = result.get(i);
						array[i] = file.getTitle();
					}*/
					
					array = new String[1];
					array[0] = sb.toString();
					
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
					Log.d(TAG, "selected accountName: " + accountName);
					
					SharedPreferences prefs = activity.getSharedPreferences(PREFS, Activity.MODE_PRIVATE);
					prefs.edit().putString("accountName", credential.getSelectedAccountName()).commit();
					
					credential.setSelectedAccountName(accountName);
					service = getDriveService(credential);
					
					checkAuthorized();
					
					success = true;
				}
			}
			
			if (!success && authFailureCallback != null) {
				authFailureCallback.run();
				
				authSuccessCallback = null;
				authFailureCallback = null;
			}
			break;
		case REQUEST_AUTHORIZATION:
			if (resultCode == Activity.RESULT_OK) {
				// accepted
				Log.d(TAG, "Authorization accepted with " + credential.getSelectedAccountName());
				
				if (authSuccessCallback != null) {
					authSuccessCallback.run();
					authSuccessCallback = null;
					authFailureCallback = null;
				}
			} else {
				// rejected
				clearSelectedAccountName();
				
				if (authFailureCallback != null) {
					authFailureCallback.run();
					authSuccessCallback = null;
					authFailureCallback = null;
				}
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
		Log.d(TAG, newIntent.toString());
		
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
	
	private static void checkAuthorized() {
		new Thread(new Runnable() {
			@Override
			public void run() {
				try {
					// authorization test
					credential.getToken();
					
					Log.d(TAG, "Authorization success!");
				
					if (authSuccessCallback != null) {
						activity.runOnUiThread(authSuccessCallback);
						authSuccessCallback = null;
						authFailureCallback = null;
					}
				} catch (final UserRecoverableAuthException e) {
					activity.runOnUiThread(new Runnable() {
						@Override
						public void run() {
							authorization(e.getIntent());
						}
					});
				} catch (final UserRecoverableAuthIOException e) {
					activity.runOnUiThread(new Runnable() {
						@Override
						public void run() {
							authorization(e.getIntent());
						}
					});
				} catch (Exception e) {
					Log.e(TAG, "checkAuthorized: " + e.toString());
					
					if (authFailureCallback != null) {
						activity.runOnUiThread(authFailureCallback);
						authSuccessCallback = null;
						authFailureCallback = null;
					}
				} 
			}
		}).start();
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
