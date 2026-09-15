using UnityEngine;

namespace Deucarian.Session.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        [SerializeField] private SessionHost session;
        public System.Threading.Tasks.Task Restore() => session.RestoreAsync();
        public System.Threading.Tasks.Task Logout() => session.LogoutAsync();
    }
}
