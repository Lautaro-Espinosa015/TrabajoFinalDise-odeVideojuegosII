using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>Mantiene el objeto mirando a la cámara (carteles que se leen aunque el tablero se incline).</summary>
    public sealed class FaceCamera : MonoBehaviour
    {
        private Camera _camera;

        private void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) transform.rotation = _camera.transform.rotation;
        }
    }
}
