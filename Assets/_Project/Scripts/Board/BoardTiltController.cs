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
        [Tooltip("Tope de seguridad de la velocidad de giro (grados por segundo). 0 = sin tope. " +
                 "El límite que se siente al jugar depende de la respuesta elegida (Fiel / Suave) en el input.")]
        [SerializeField, Min(0f)] private float angularSpeedCap = 720f;
        [Tooltip("Al empezar, el tablero pasa de nivelado al control del jugador en este tiempo (segundos).")]
        [SerializeField, Min(0f)] private float startBlendTime = 1.5f;

        private Rigidbody _body;
        private ITiltInput _input;
        private Quaternion _restRotation;
        private Quaternion _current = Quaternion.identity;
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
            if (angularSpeedCap > 0f)
                target = Quaternion.RotateTowards(_current, target, angularSpeedCap * Time.fixedDeltaTime);

            _current = target;
            _body.MoveRotation(_restRotation * _current);
        }
    }
}
