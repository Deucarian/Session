using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.Session
{
    /// <summary>
    /// Stores session data in Unity PlayerPrefs.
    /// </summary>
    /// <remarks>
    /// PlayerPrefs is convenient but not secure storage. Use a custom <see cref="ISessionStore"/> for stronger token protection.
    /// </remarks>
    public sealed class PlayerPrefsSessionStore : ISessionStore
    {
        /// <summary>
        /// Default PlayerPrefs key used when no key is supplied.
        /// </summary>
        public const string DefaultKey = "com.deucarian.session.session";

        private readonly string key;
        private readonly bool saveImmediately;

        /// <summary>
        /// Creates a PlayerPrefs-backed session store.
        /// </summary>
        /// <param name="key">PlayerPrefs key used to store session data.</param>
        /// <param name="saveImmediately">Whether to call <see cref="PlayerPrefs.Save"/> after save and clear operations.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null, empty, or whitespace.</exception>
        public PlayerPrefsSessionStore(string key = DefaultKey, bool saveImmediately = true)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A PlayerPrefs key is required.", nameof(key));
            }

            this.key = key;
            this.saveImmediately = saveImmediately;
        }

        /// <inheritdoc />
        public Task<SessionData> LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PlayerPrefs.HasKey(key))
            {
                return Task.FromResult<SessionData>(null);
            }

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(json))
            {
                return Task.FromResult<SessionData>(null);
            }

            try
            {
                StoredSessionData stored = JsonUtility.FromJson<StoredSessionData>(json);
                return Task.FromResult(ToSessionData(stored));
            }
            catch (Exception)
            {
                return Task.FromResult<SessionData>(null);
            }
        }

        /// <inheritdoc />
        public Task SaveAsync(SessionData session, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (session == null)
            {
                return ClearAsync(cancellationToken);
            }

            StoredSessionData stored = FromSessionData(session);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(stored));

            if (saveImmediately)
            {
                PlayerPrefs.Save();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ClearAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            PlayerPrefs.DeleteKey(key);

            if (saveImmediately)
            {
                PlayerPrefs.Save();
            }

            return Task.CompletedTask;
        }

        private static StoredSessionData FromSessionData(SessionData session)
        {
            return new StoredSessionData
            {
                accessToken = session.AccessToken,
                refreshToken = session.RefreshToken,
                expiresAtUtc = session.ExpiresAtUtc.HasValue
                    ? session.ExpiresAtUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                    : null
            };
        }

        private static SessionData ToSessionData(StoredSessionData stored)
        {
            if (stored == null || string.IsNullOrWhiteSpace(stored.accessToken))
            {
                return null;
            }

            DateTimeOffset? expiresAtUtc = null;
            if (!string.IsNullOrEmpty(stored.expiresAtUtc))
            {
                DateTimeOffset parsed;
                if (DateTimeOffset.TryParseExact(
                    stored.expiresAtUtc,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out parsed))
                {
                    expiresAtUtc = parsed.ToUniversalTime();
                }
            }

            return new SessionData(stored.accessToken, stored.refreshToken, expiresAtUtc);
        }

        [Serializable]
        private sealed class StoredSessionData
        {
            public string accessToken;
            public string refreshToken;
            public string expiresAtUtc;
        }
    }
}
