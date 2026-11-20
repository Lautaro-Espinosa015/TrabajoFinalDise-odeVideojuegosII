using EcoTilt.Controls;
using UnityEngine;

namespace EcoTilt.Board
{
    /// <summary>
    /// Rota el tablero según un ITiltInput. El tablero es un Rigidbody cinemático movido con MoveRotation
    /// en FixedUpdate: así los residuos (cuerpos dinámicos) reciben el movimiento de forma físicamente
    /// correcta, y un giro brusco los lanza por el aire.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BoardTiltController : MonoBehaviour
    {
        [Tooltip("Componente que implemente ITiltInput (por ejemplo, TiltInputProvider).")]
        [SerializeField] private MonoBehaviour inputSource;
        [Tooltip("Suavizado extra del tablero. 0 = sigue al instante (control total).")]
        [SerializeField, Range(0f, 30f)] private float smoothing = 0f;
        [Tooltip("Qué tan rápido alcanza el tablero la inclinación pedida (Hz). Funciona como un resorte amortiguado: " +
                 "acelera y frena de a poco, así las paredes no \"golpean\" a los residuos. 6 Hz ≈ 0,05 s de demora.")]
        [SerializeField, Range(1f, 15f)] private float responseFrequency = 6f;
        [Tooltip("Velocidad de giro máxima del tablero (grados por segundo). El tablero mide 16 m: a 360°/s el borde " +
                 "ya se mueve a 50 m/s, por eso hace falta un tope.")]
        [SerializeField, Min(30f)] private float speedLimit = 360f;
        [Tooltip("Al empezar, el tablero pasa de nivelado al control del jugador en este tiempo (segundos).")]
        [SerializeField, Min(0f)] private float startBlendTime = 1.5f;

        private Rigidbody _body;
        private ITiltInput _input;
        private Quaternion _restRotation;
        private Quaternion _current = Quaternion.identity;
        private Vector3 _angularVelocity; // rad/s, en ejes del tablero en reposo
        private float _elapsed;

        /// <summary>Inclinación actual respecto de la posición nivelada.</summary>
        public Quaternion CurrentTilt => _current;

        /// <summary>Permite inyectar el input por código (tests, otra escena).</summary>
        public void SetInput(ITiltInput input)
        {
            _input = input;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _restRotation = transform.rotation;

            if (_input == null) _input = inputSource as ITiltInput;
            if (_input == null)
                Debug.LogError($"{name}: falta asignar un input que implemente ITiltInput.", this);
        }

        private void OnValidate()
        {
            if (inputSource != null && !(inputSource is ITiltInput))
            {
                Debug.LogWarning($"{inputSource.name} no implementa ITiltInput.", this);
                inputSource = null;
            }
        }

        private void FixedUpdate()
        {
            Quaternion target = _input != null ? _input.ReadTilt() : Quaternion.identity;

            // Arranque suave: los residuos se asientan antes de que el tablero responda del todo.
            _elapsed += Time.fixedDeltaTime;
            if (startBlendTime > 0f && _elapsed < startBlendTime)
                target = Quaternion.Slerp(Quaternion.identity, target, _elapsed / startBlendTime);

            if (smoothing > 0f)
                target = Quaternion.Slerp(_current, target, 1f - Mathf.Exp(-smoothing * Time.fixedDeltaTime));
            _current = FollowTarget(target, Time.fixedDeltaTime);
            _body.MoveRotation(_restRotation * _current);
        }

        /// <summary>
        /// Resorte críticamente amortiguado hacia la inclinación pedida: sin rebote y sin saltos de velocidad.
        /// Un salto brusco de la lectura (temblor, cambio de sensor) se convierte en un movimiento suave.
        /// </summary>
        private Quaternion FollowTarget(Quaternion target, float dt)
        {
            Quaternion error = target * Quaternion.Inverse(_current);
            if (error.w < 0f) error = new Quaternion(-error.x, -error.y, -error.z, -error.w); // camino corto
            error.ToAngleAxis(out float angle, out Vector3 axis);
            Vector3 offset = float.IsFinite(axis.x) && angle > 0.0001f ? axis.normalized * (angle * Mathf.Deg2Rad) : Vector3.zero;

            float w = 2f * Mathf.PI * responseFrequency;
            _angularVelocity += (w * w * offset - 2f * w * _angularVelocity) * dt;
            _angularVelocity = Vector3.ClampMagnitude(_angularVelocity, speedLimit * Mathf.Deg2Rad);

            float step = _angularVelocity.magnitude * dt;
            if (step < 1e-6f) return _current;
            return Quaternion.AngleAxis(step * Mathf.Rad2Deg, _angularVelocity / _angularVelocity.magnitude) * _current;
        }
    }
}
