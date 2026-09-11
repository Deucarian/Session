using UnityEngine;
namespace Deucarian.Session.Samples.DefinitionWorkflow
{
    [DefaultExecutionOrder(-2000)]
    public sealed class SampleSessionSetup : MonoBehaviour
    {
        [SerializeField] private SessionHost host;
        private void Awake() => host.Configure(new SessionService(new InMemorySessionStore()));
    }
}
