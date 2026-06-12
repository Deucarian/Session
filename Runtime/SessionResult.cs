using System;

namespace Deucarian.Session
{
    /// <summary>
    /// Represents the result of a session operation.
    /// </summary>
    public sealed class SessionResult
    {
        private SessionResult(bool succeeded, SessionData session, SessionError error)
        {
            Succeeded = succeeded;
            Session = session;
            Error = error;
        }

        /// <summary>
        /// Gets whether the operation completed successfully.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets whether the operation failed.
        /// </summary>
        public bool IsFailure
        {
            get { return !Succeeded; }
        }

        /// <summary>
        /// Gets the session returned by the operation, or null when no session is available.
        /// </summary>
        public SessionData Session { get; }

        /// <summary>
        /// Gets the failure details, or null when the operation succeeded.
        /// </summary>
        public SessionError Error { get; }

        /// <summary>
        /// Creates a successful result with no session data.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static SessionResult Success()
        {
            return new SessionResult(true, null, null);
        }

        /// <summary>
        /// Creates a successful result with session data.
        /// </summary>
        /// <param name="session">The session returned by the operation.</param>
        /// <returns>A successful result.</returns>
        public static SessionResult Success(SessionData session)
        {
            return new SessionResult(true, session, null);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="error">The failure details.</param>
        /// <returns>A failed result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
        public static SessionResult Failed(SessionError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return new SessionResult(false, null, error);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="code">Stable machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <returns>A failed result.</returns>
        public static SessionResult Failed(string code, string message, Exception exception = null)
        {
            return Failed(SessionError.Create(code, message, exception));
        }
    }
}
