using System;
using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>Zona bajo el tablero: todo IRespawnable que entra vuelve al inicio y se avisa de la caída.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KillZone : MonoBehaviour
    {
        /// <summary>Se dispara una vez por cada caída.</summary>
        public event Action<IRespawnable> Fell;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            IRespawnable respawnable = other.GetComponentInParent<IRespawnable>();
            if (respawnable != null && respawnable.RequestRespawn()) Fell?.Invoke(respawnable);
        }
    }
}
