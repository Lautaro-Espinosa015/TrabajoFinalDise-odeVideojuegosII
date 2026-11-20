using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
[DisallowMultipleComponent]
public class SonidoArrastre : MonoBehaviour
{
    [UnityEngine.Serialization.FormerlySerializedAs("sonidoArrastre")]
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 0.10f;
    [Tooltip("Velocidad mínima del objeto para sonar. Ignora pequeñas vibraciones cuando está quieto.")]
    [Min(0.01f)] public float velocidadMinima = 0.1f;
    private AudioSource audioSource;
    private Rigidbody rb;
    private bool audioPausado;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        audioSource.playOnAwake = false;
        audioSource.Stop();
        if (sonido != null) audioSource.clip = sonido;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volumen * EcoTilt.Audio.AudioPreferences.Effects;
    }

    void Update()
    {
        audioSource.volume = volumen * EcoTilt.Audio.AudioPreferences.Effects;
        // Cada residuo suena solamente por su propio movimiento, también en celular.
        float umbral = Mathf.Max(0.01f, velocidadMinima);
        bool seMueve = Time.timeScale > 0f && !rb.isKinematic && !rb.IsSleeping() &&
            rb.linearVelocity.sqrMagnitude > umbral * umbral;

        if (seMueve && audioSource.clip != null)
        {
            if (!audioSource.isPlaying)
            {
                if (audioPausado) audioSource.UnPause();
                else audioSource.Play();
                audioPausado = false;
            }
        }
        else if (audioSource.isPlaying)
        {
            // Conservar la posición evita repetir el inicio del clip en cada movimiento.
            audioSource.Pause();
            audioPausado = true;
        }
    }

    void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            audioPausado = true;
        }
    }
}
