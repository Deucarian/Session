using System;

namespace JorisHoef.SessionHelper
{
    /// <summary>
    /// Provides details for <see cref="ISessionService.SessionChanged"/>.
    /// </summary>
    public sealed class SessionChangedEventArgs : EventArgs
    {
        internal SessionChangedEventArgs(
            SessionData previousSession,
            SessionData currentSession,
            SessionState previousState,
            SessionState currentState,
            SessionChangeReason reason)
        {
            PreviousSession = previousSession;
            CurrentSession = currentSession;
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason;
        }

        /// <summary>
        /// Gets the session before the change.
        /// </summary>
        public SessionData PreviousSession { get; }

        /// <summary>
        /// Gets the session after the change.
        /// </summary>
        public SessionData CurrentSession { get; }

        /// <summary>
        /// Gets the state before the change.
        /// </summary>
        public SessionState PreviousState { get; }

        /// <summary>
        /// Gets the state after the change.
        /// </summary>
        public SessionState CurrentState { get; }

        /// <summary>
        /// Gets the operation that caused the change.
        /// </summary>
        public SessionChangeReason Reason { get; }
    }
}
