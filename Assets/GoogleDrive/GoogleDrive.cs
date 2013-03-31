using System;

namespace GoogleDrive
{
	class AuthException : Exception 
	{
		public AuthException() : this(string.Empty) { }
		public AuthException(string message) : base(message) { }
	}
}
