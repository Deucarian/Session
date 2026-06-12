using System;

namespace Deucarian.Session
{
    /// <summary>
    /// Represents the token data for the current authenticated session.
    /// </summary>
    public sealed class SessionData : IEquatable<SessionData>
    {
        /// <summary>
        /// Creates immutable session data.
        /// </summary>
        /// <param name="accessToken">Access token returned by the application's backend.</param>
        /// <param name="refreshToken">Optional refresh token returned by the application's backend.</param>
        /// <param name="expiresAtUtc">Optional UTC expiry time for the access token.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="accessToken"/> is null, empty, or whitespace.</exception>
        public SessionData(string accessToken, string refreshToken = null, DateTimeOffset? expiresAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException("An access token is required.", nameof(accessToken));
            }

            AccessToken = accessToken;
            RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken;
            ExpiresAtUtc = expiresAtUtc.HasValue ? expiresAtUtc.Value.ToUniversalTime() : (DateTimeOffset?)null;
        }

        /// <summary>
        /// Gets the access token returned by the application's backend.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the optional refresh token returned by the application's backend.
        /// </summary>
        public string RefreshToken { get; }

        /// <summary>
        /// Gets the optional UTC expiry time for the access token.
        /// </summary>
        public DateTimeOffset? ExpiresAtUtc { get; }

        /// <summary>
        /// Gets whether this session has a non-empty refresh token.
        /// </summary>
        public bool HasRefreshToken
        {
            get { return !string.IsNullOrWhiteSpace(RefreshToken); }
        }

        /// <summary>
        /// Returns whether the access token has expired at the supplied time.
        /// </summary>
        /// <param name="utcNow">The current UTC time.</param>
        /// <returns>True when an expiry time exists and has passed; otherwise false.</returns>
        public bool IsExpired(DateTimeOffset utcNow)
        {
            return ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= utcNow.ToUniversalTime();
        }

        /// <summary>
        /// Returns whether the access token is expired or will expire within the supplied leeway.
        /// </summary>
        /// <param name="utcNow">The current UTC time.</param>
        /// <param name="leeway">How far ahead to consider the token close to expiry.</param>
        /// <returns>True when an expiry time exists and is within the leeway window; otherwise false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="leeway"/> is negative.</exception>
        public bool IsExpiredOrExpiringWithin(DateTimeOffset utcNow, TimeSpan leeway)
        {
            if (leeway < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(leeway), "Expiry leeway cannot be negative.");
            }

            return ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= utcNow.ToUniversalTime().Add(leeway);
        }

        /// <summary>
        /// Determines whether this session has the same token values as another session.
        /// </summary>
        /// <param name="other">The session to compare with this session.</param>
        /// <returns>True when both sessions contain the same values; otherwise false.</returns>
        public bool Equals(SessionData other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(AccessToken, other.AccessToken, StringComparison.Ordinal)
                   && string.Equals(RefreshToken, other.RefreshToken, StringComparison.Ordinal)
                   && Nullable.Equals(ExpiresAtUtc, other.ExpiresAtUtc);
        }

        /// <summary>
        /// Determines whether this session has the same token values as another object.
        /// </summary>
        /// <param name="obj">The object to compare with this session.</param>
        /// <returns>True when the object is an equivalent session; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as SessionData);
        }

        /// <summary>
        /// Returns a hash code for this session.
        /// </summary>
        /// <returns>A hash code for this session.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 17;
                hashCode = (hashCode * 31) + AccessToken.GetHashCode();
                hashCode = (hashCode * 31) + (RefreshToken == null ? 0 : RefreshToken.GetHashCode());
                hashCode = (hashCode * 31) + (ExpiresAtUtc.HasValue ? ExpiresAtUtc.Value.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
