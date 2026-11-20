using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Ajustes globales de física: más pasos por segundo y más iteraciones del solver (para que los giros
    /// bruscos no hagan atravesar el piso) y "peso" de los residuos. El tablero mide 16 m y los residuos 30 cm:
    /// con la gravedad real todo parece flotar, por eso se multiplica la gravedad y se agrega algo de rozamiento.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PhysicsQuality : MonoBehaviour
    {
        [Tooltip("Pasos de física por segundo (Unity usa 50 por defecto).")]
        [SerializeField, Range(50, 200)] private int stepsPerSecond = 100;
        [SerializeField, Range(6, 30)] private int solverIterations = 12;
        [SerializeField, Range(1, 15)] private int solverVelocityIterations = 4;
        [Tooltip("Velocidad de giro máxima de los residuos (rad/s). Unity limita a 7 por defecto, lo que frena las latas.")]
        [SerializeField, Min(7f)] private float maxAngularSpeed = 50f;

        [Header("Peso")]
        [Tooltip("Multiplicador de la gravedad. 1 = real; 2-2.5 hace que los residuos se sientan pesados a esta escala.")]
        [SerializeField, Range(1f, 4f)] private float gravityMultiplier = 2.2f;
        [Tooltip("Velocidad máxima con la que la física separa objetos encimados. Bajo = sin rebotes explosivos.")]
        [SerializeField, Range(1f, 10f)] private float maxDepenetrationVelocity = 3f;
        [SerializeField, Range(0f, 2f)] private float linearDamping = 0.15f;
        [SerializeField, Range(0f, 2f)] private float angularDamping = 0.3f;

        [Header("Rendimiento")]
        [Tooltip("Atraso máximo (segundos) que la física recupera en un cuadro. Unity usa 0,33: si el teléfono se " +
                 "calienta y un cuadro tarda, se acumulan decenas de pasos de física, el siguiente cuadro tarda más " +
                 "y el juego entra en cámara lenta. Con 0,05 se descarta ese atraso y el control sigue al día.")]
        [SerializeField, Range(0.02f, 0.33f)] private float maximumFrameCatchUp = 0.05f;

        private void Awake()
        {
            Time.fixedDeltaTime = 1f / stepsPerSecond;
            Time.maximumDeltaTime = Mathf.Max(maximumFrameCatchUp, Time.fixedDeltaTime);
            Physics.defaultSolverIterations = solverIterations;
            Physics.defaultSolverVelocityIterations = solverVelocityIterations;
            Physics.defaultMaxAngularSpeed = maxAngularSpeed;
            Physics.gravity = new Vector3(0f, -9.81f * gravityMultiplier, 0f);
            Physics.defaultMaxDepenetrationVelocity = maxDepenetrationVelocity;

            // Los cuerpos que ya existen en la escena se crearon con los valores anteriores.
            foreach (Rigidbody body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                body.solverIterations = solverIterations;
                body.solverVelocityIterations = solverVelocityIterations;
                body.maxAngularVelocity = maxAngularSpeed;
                body.maxDepenetrationVelocity = maxDepenetrationVelocity;
                if (body.isKinematic) continue;
                body.linearDamping = Mathf.Max(body.linearDamping, linearDamping);
                body.angularDamping = Mathf.Max(body.angularDamping, angularDamping);
            }
        }
    }
}
