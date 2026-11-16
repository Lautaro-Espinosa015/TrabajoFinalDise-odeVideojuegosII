using EcoTilt.Controls;
using UnityEngine;

namespace EcoTilt.Board
{
    /// <summary>
    /// Inclina el tablero en X y Z según un ITiltInput.
    /// El tablero es un Rigidbody cinemático movido con MoveRotation en FixedUpdate,
    /// así los residuos (cuerpos dinámicos) reciben el movimiento de forma físicamente correcta.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BoardTiltController : MonoBehaviour
    {
        [Tooltip("Componente que implemente ITiltInput (por ejemplo, TiltInputProvider).")]
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField, Range(1f, 30f)] private float maxTiltAngle = 12f;
        [SerializeField, Range(0.01f, 1f)] private float smoothTime = 0.12f;
        [Tooltip("Velocidad angular máxima en grados por segundo. Evita que un giro brusco lance los residuos.")]
        [SerializeField, Min(1f)] private float maxAngularSpeed = 60f;

        private Rigidbody _body;
        private ITiltInput _input;
        private Quaternion _restRotation;
        private Vector2 _currentAngles;
        private Vector2 _angularVelocity;

        /// <summary>Inclinación actual en grados (x: lateral, y: adelante/atrás).</summary>
        public Vector2 CurrentAngles => _currentAngles;

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
            Vector2 target = _input != null
                ? Vector2.ClampMagnitude(_input.ReadTilt(), 1f) * maxTiltAngle
                : Vector2.zero;

            _currentAngles = Vector2.SmoothDamp(
                _currentAngles, target, ref _angularVelocity, smoothTime, maxAngularSpeed, Time.fixedDeltaTime);

            // Eje X positivo baja el extremo lejano (+Z); eje Z negativo baja el lado derecho (+X).
            Quaternion tilt = Quaternion.Euler(_currentAngles.y, 0f, -_currentAngles.x);
            _body.MoveRotation(_restRotation * tilt);
        }
    }
}
