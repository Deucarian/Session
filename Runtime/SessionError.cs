using System;

namespace Deucarian.Session
{
    /// <summary>
    /// Describes why a session operation failed.
    /// </summary>
    public sealed class SessionError
    {
        /// <summary>
        /// Creates a session error.
        /// </summary>
        /// <param name="code">Stable machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is null, empty, or whitespace.</exception>
        public SessionError(string code, string message, Exception exception = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("An error code is required.", nameof(code));
            }

            Code = code;
            Message = string.IsNullOrEmpty(message) ? code : message;
            Exception = exception;
        }

        /// <summary>
        /// Gets a stable machine-readable error code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the optional exception that caused the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Creates a session error.
        /// </summary>
        /// <param name="code">Stable machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <returns>A new session error.</returns>
        public static SessionError Create(string code, string message, Exception exception = null)
        {
            return new SessionError(code, message, exception);
        }
    }
}
