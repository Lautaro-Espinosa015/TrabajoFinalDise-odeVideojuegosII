using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Red de seguridad de la física de un residuo: limita su velocidad para que un golpe del tablero
    /// (una pared que lo barre en un giro brusco) no lo lance por el aire hasta el techo invisible.
    /// No toca los cuerpos cinemáticos (residuo guardado en un contenedor, volando al correcto, etc.).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class VelocityLimiter : MonoBehaviour
    {
        [Tooltip("Velocidad máxima en metros por segundo.")]
        [SerializeField, Min(1f)] private float maxSpeed = 12f;
        [Tooltip("Velocidad máxima hacia arriba (m/s). Con la gravedad del juego, 5 m/s ≈ medio metro de altura.")]
        [SerializeField, Min(0.5f)] private float maxRiseSpeed = 5f;

        private Rigidbody _body;

        private void Awake() => _body = GetComponent<Rigidbody>();

        private void FixedUpdate()
        {
            if (_body.isKinematic) return;
            Vector3 v = _body.linearVelocity;
            if (v.y <= maxRiseSpeed && v.sqrMagnitude <= maxSpeed * maxSpeed) return; // sin tocar: deja dormir al cuerpo
            if (v.y > maxRiseSpeed) v.y = maxRiseSpeed;
            if (v.sqrMagnitude > maxSpeed * maxSpeed) v = v.normalized * maxSpeed;
            _body.linearVelocity = v;
        }
    }
}
