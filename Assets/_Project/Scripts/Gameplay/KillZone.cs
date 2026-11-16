using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>Zona bajo el tablero: todo IRespawnable que entra vuelve al inicio.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KillZone : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            IRespawnable respawnable = other.GetComponentInParent<IRespawnable>();
            if (respawnable != null) respawnable.RequestRespawn();
        }
    }
}
