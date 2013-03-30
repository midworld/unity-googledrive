using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class DriveTest2 : MonoBehaviour
{
	public Transform cube = null;

	void Start()
	{	
	}

	bool list = false;

	IEnumerator List(int maxResults = -1)
	{
		GoogleDrive.Files.List ticket = 
			new GoogleDrive.Files.List(maxResults);

		StartCoroutine(ticket);

		while (!ticket.isDone)
		{
			yield return null;
		}
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}

		if (cube != null)
		{
			cube.RotateAround(Vector3.up, Time.deltaTime);
		}
	}

	void OnGUI()
	{
		int y = 0;

		if (!list && GUI.Button(new Rect(10, 10 + 110 * y++, 300, 100), "List"))
		{
			StartCoroutine(List());
		}

		if (!list && GUI.Button(new Rect(10, 10 + 110 * y++, 300, 100), "List (20)"))
		{
			StartCoroutine(List(20));
		}
	}
}
