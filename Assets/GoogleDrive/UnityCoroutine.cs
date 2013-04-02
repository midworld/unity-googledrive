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

				if (_isDone && done != null)
				{
					done(this);
					done = null;
				}
			}
		}

		/// <summary>
		/// Done callback function.
		/// </summary>
		/// <remarks>
		/// It must be set before StartCoroutine() if you want to use the done callback.
		/// </remarks>
		public Action<UnityCoroutine> done { get; set; }

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
		protected Queue<Action> routines = new Queue<Action>();

		/// <summary>
		/// Run the routines.
		/// </summary>
		/// <returns>Is there remaining work.</returns>
		public bool MoveNext()
		{
			if (!isDone && routines.Count > 0)
			{
				Action routine = routines.Dequeue();
				routine();
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
