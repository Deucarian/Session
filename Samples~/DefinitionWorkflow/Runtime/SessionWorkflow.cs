using System;
using UnityEngine;

namespace Deucarian.Session.Samples.DefinitionWorkflow
{
    /// <summary>Small caller example. The configured scene hosts own services and resource lifetimes.</summary>
    public sealed class SessionWorkflow : MonoBehaviour
    {
        [SerializeField] private SessionHost host;
        [SerializeField] private SessionTrigger trigger;
        private string status = "Ready. Choose an action below.";
        public string Status => status;
        public async void Restore() { var result = await host.RestoreAsync(); status = result.Succeeded ? "Session restored: " + host.State : "Restore failed."; }
        public async void Logout() { var result = await host.LogoutAsync(); status = result.Succeeded ? "Signed out." : "Logout failed."; }
        public void RestoreComponent() { trigger.Restore(); status = "Restore requested through SessionTrigger."; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, Math.Min(540, Screen.width - 48), Screen.height - 48), GUI.skin.box);
            GUILayout.Label("Session — definition workflow");
            GUILayout.Label("Session state belongs to one SessionService. Both access paths use that service; this scene uses an empty memory store.");
            GUILayout.Space(12);
            if (GUILayout.Button("Restore with C#", GUILayout.Height(32))) { try { Restore(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Logout with C#", GUILayout.Height(32))) { try { Logout(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Restore from component", GUILayout.Height(32))) { try { RestoreComponent(); } catch (Exception error) { status = error.Message; } }
            GUILayout.Space(12);
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}
