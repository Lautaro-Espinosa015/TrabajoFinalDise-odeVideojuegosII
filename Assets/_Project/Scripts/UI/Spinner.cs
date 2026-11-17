using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>Hace girar un objeto sobre el eje vertical del mundo y lo hace flotar suavemente (íconos de contenedores).</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 60f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 _basePosition;

        private void Awake()
        {
            _basePosition = transform.localPosition;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
            transform.localPosition = _basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
        }
    }
}
