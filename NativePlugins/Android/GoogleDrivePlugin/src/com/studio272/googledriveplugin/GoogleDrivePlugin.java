package com.studio272.googledriveplugin;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.Iterator;
import java.util.List;
import java.util.Set;
import java.util.concurrent.atomic.AtomicInteger;

import com.google.android.gms.auth.UserRecoverableAuthException;
import com.google.api.client.extensions.android.http.AndroidHttp;
import com.google.api.client.googleapis.extensions.android.gms.auth.GoogleAccountCredential;
import com.google.api.client.googleapis.extensions.android.gms.auth.UserRecoverableAuthIOException;
import com.google.api.client.googleapis.json.GoogleJsonResponseException;
import com.google.api.client.http.HttpRequest;
import com.google.api.client.json.gson.GsonFactory;
import com.google.api.services.drive.Drive;
import com.google.api.services.drive.DriveScopes;
import com.google.api.services.drive.Drive.Files;
import com.google.api.services.drive.model.FileList;

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
	
	static String apiKey = null;
	
	public static void setAPIKey(String key) {
		Log.d(TAG, "setAPIKey: " + key);
		
		apiKey = key;
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
	
	static String token = null;
	
	public static String getAuthToken() {
		return token;
	}
	
	public static String getSelectedAccountName() {
		SharedPreferences prefs = activity.getSharedPreferences(PREFS, Activity.MODE_PRIVATE);
		String accountName = prefs.getString("accountName", null);
		
		return accountName;
	}
	
	public static void clearSelectedAccountName() {
		SharedPreferences prefs = activity.getSharedPreferences(PREFS, Activity.MODE_PRIVATE);
		if (prefs.contains("accountName")) {
			prefs.edit().remove("accountName").commit();
		}
	}
	
	static SparseArray<ArrayList<String>> results = new SparseArray<ArrayList<String>>();
	static SparseArray<String> errors = new SparseArray<String>();
	static HashSet<Integer> finished = new HashSet<Integer>(); 
	static AtomicInteger resultIndex = new AtomicInteger();
	
	public static String[] getResult(int index) {
		ArrayList<String> result = results.get(index);
		String[] resultArray;
		
		if (result == null)
			return null;
		
		synchronized (result) {
			resultArray = result.toArray(new String[]{});
			result.clear();
		}
		
		synchronized (finished) {
			if (finished.contains(index)) {
				result = null;
				results.delete(index);
			}
		}
		
		return resultArray;
	}
	
	public static String getError(int index) {
		synchronized (errors) {
			String error = errors.get(index);
			
			if (error != null) {
				errors.delete(index);
			}
			
			return error;
		}
	}
	
	public static int list(final int maxResults, final Runnable progress, final Runnable complete) {
		final int currentId = resultIndex.getAndIncrement();
		final ArrayList<String> result = new ArrayList<String>();
		results.put(currentId, result);
		
		new Thread(new Runnable() {
			@Override
			public void run() {
				try {
					Files.List request = service.files().list();
					if (maxResults > 0)
						request.setMaxResults(maxResults);
					
					do {
						try {
							FileList files = request.execute();
							
							synchronized (result) {
								result.add(files.toString());
							}
							
							activity.runOnUiThread(progress);
							
							request.setPageToken(files.getNextPageToken());
						} catch (UserRecoverableAuthIOException e) {
							throw e;
						} catch (GoogleJsonResponseException e) {
							// retry after 100msec
							Thread.sleep(100);
						}
					} while (request.getPageToken() != null &&
							request.getPageToken().length() > 0);
				} catch (UserRecoverableAuthIOException e) {
					//authorization(e.getIntent());
					Log.w(TAG, e.getMessage(), e);
					
					synchronized (errors) {
						errors.put(currentId, e.getClass().getName());
					}
				} catch (Exception e) {
					Log.e(TAG, e.getMessage(), e);
					
					synchronized (errors) {
						errors.put(currentId, e.getClass().getName());
					}
				} finally {
					activity.runOnUiThread(complete);
					
					int remainingResult;
					
					synchronized (result) {
						remainingResult = result.size();
					}
					
					if (remainingResult > 0) {
						synchronized (finished) {
							finished.add(currentId);
						}
					} else {
						activity.runOnUiThread(new Runnable() {
							@Override
							public void run() {
								results.delete(currentId);
							}
						});
					}
				}
			}
		}).start();
		
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
					prefs.edit().putString("accountName", accountName).commit();
					
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
					token = credential.getToken();
					
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
