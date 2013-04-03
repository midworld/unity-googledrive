using System;
using System.Collections;
using System.Collections.Generic;

namespace Midworld
{
	/// <summary>
	/// Unity3D coroutine object.
	/// </summary>
	/// <example>
	/// Usage with Unity3D coroutine.
	/// <code>
	/// IEnumerator SomeCoroutine()
	/// {
	///		// Wait for the coroutine to finish.
	///		yield return StartCoroutine(new SomeUnityCoroutine1());
	///		
	///		// Do something after 'SomeUnityCoroutine1' finished.
	///		// ...
	/// 
	///		// Run this coroutine and 'SomeUnityCorutine2' at same time.
	///		SomeUnityCoroutine2 routine2 = new SomeUnityCoroutine2();
	///		StartCoroutine(routine2);
	///		
	///		// Do parallel works.
	///		// ...
	///		
	///		// And wait for 'SomeUnityCoroutine2'.
	///		while (!routine2.isDone)
	///			yield return null;
	/// }
	/// </code>
	/// 
	/// Usage with the done callback.
	/// <code>
	/// StartCoroutine(new SomeUnityCoroutine((coroutine) =>
	/// {
	///		// Done!
	///		
	///		SomeUnityCoroutine r = coroutine as SomeUnityCoroutine;
	///		
	///		print(r.someResult);
	///		
	///		// ...
	/// }));
	/// </code>
	/// </example>
	class UnityCoroutine : IEnumerator
	{
		/// <summary>
		/// Done flag.
		/// </summary>
		private bool _isDone = false;

		/// <summary>
		/// All processing is done.
		/// </summary>
		/// <remarks>
		/// Callback the done function only once when the flag set true.
		/// If this UnityCorouine started by StartCoroutine() then 
		/// done function will run in main thread. If it is not then the
		/// function will run in the working thread.
		/// </remarks>
		public bool isDone
		{
			get
			{
				return _isDone;
			}

			protected set
			{
				_isDone = value;

				if (_isDone && !isCoroutine && done != null)
				{
					done(this);
					done = null;
				}
			}
		}

		/// <summary>
		/// Done callback function.
		/// </summary>
		private Action<UnityCoroutine> _done = null;

		/// <summary>
		/// Done callback function getter/setter.
		/// </summary>
		/// <remarks>
		/// Done callback run immediately if the coroutine is already done.
		/// </remarks>
		public Action<UnityCoroutine> done 
		{
			get
			{
				return _done;
			}
			set
			{
				_done = value;

				if (isDone)
				{
					_done(this);
					_done = null;
				}
			}
		}

		/// <summary>
		/// Create a Unity coroutine without the done callback.
		/// </summary>
		protected UnityCoroutine() : this(null) { }

		/// <summary>
		/// Create a Unity coroutine with the done callback.
		/// </summary>
		/// <param name="doneCallback">Done callback function.</param>
		public UnityCoroutine(Action<UnityCoroutine> doneCallback)
		{
			done = doneCallback;
		}

		/// <summary>
		/// Run all remaining routines at this time.
		/// </summary>
		public void DoSync()
		{
			while (MoveNext()) ;
		}

		/// <summary>
		/// Run the routines one at a time(each routine at each update).
		/// </summary>
		/// <remarks>
		/// Do not dequeue manually.
		/// </remarks>
		protected Queue<Action> routines = new Queue<Action>();

		/// <summary>
		/// Is this UnityCoroutine started by StartCoroutine().
		/// </summary>
		private bool isCoroutine = false;

		/// <summary>
		/// Run the routines.
		/// </summary>
		/// <returns>Is there remaining work.</returns>
		public bool MoveNext()
		{
			isCoroutine = true;

			if (!isDone && routines.Count > 0)
			{
				Action routine = routines.Dequeue();
				routine();
			}

			if (isDone && done != null)
			{
				done(this);
				done = null;
			}

			return !isDone;
		}

		/// <summary>
		/// Clear queued routines.
		/// </summary>
		public void Reset()
		{
			routines.Clear();
		}

		/// <summary>
		/// Remaining routines count.
		/// </summary>
		public object Current
		{
			get
			{
				return routines.Count;
			}
		}
	}
}
