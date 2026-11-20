using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sonido de arrastre de cada residuo, sincronizado con su movimiento real:
/// - Solo suena mientras toca una superficie y se mueve (no en el aire, ni en el salto, ni al caer al vacío).
/// - El volumen y el tono siguen la velocidad, con un fundido corto (sin cortes ni "clics").
/// - El clip original es un golpe corto con silencio al principio y al final; se recorta y se convierte en un
///   loop continuo con fundido cruzado, para que el sonido empiece en el mismo instante que el movimiento.
/// </summary>
[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
[DisallowMultipleComponent]
public class SonidoArrastre : MonoBehaviour
{
    [UnityEngine.Serialization.FormerlySerializedAs("sonidoArrastre")]
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 0.10f;
    [Tooltip("Velocidad mínima del objeto para sonar. Ignora pequeñas vibraciones cuando está quieto.")]
    [Min(0.01f)] public float velocidadMinima = 0.1f;
    [Tooltip("Velocidad (m/s) a la que el sonido llega a su volumen máximo.")]
    [Min(0.1f)] public float velocidadPlena = 3f;
    [Tooltip("Tono a baja y a alta velocidad.")]
    public Vector2 tono = new Vector2(0.85f, 1.2f);
    [Tooltip("Segundos de fundido al empezar o dejar de moverse.")]
    [Min(0.01f)] public float fundido = 0.08f;

    private const float ContactoGracia = 0.08f; // segundos sin contacto antes de considerarlo "en el aire"
    private static readonly Dictionary<AudioClip, AudioClip> Loops = new Dictionary<AudioClip, AudioClip>();

    private AudioSource audioSource;
    private Rigidbody rb;
    private float ultimoContacto = -1f;
    private float nivel; // 0..1, volumen actual antes del volumen general

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        audioSource.playOnAwake = false;
        audioSource.Stop();
        audioSource.clip = sonido != null ? LoopContinuo(sonido) : null;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
    }

    void OnCollisionStay(Collision collision) => ultimoContacto = Time.time;
    void OnCollisionEnter(Collision collision) => ultimoContacto = Time.time;

    void Update()
    {
        if (audioSource.clip == null) return;

        float velocidad = rb.isKinematic ? 0f : rb.linearVelocity.magnitude;
        bool tocando = Time.time - ultimoContacto <= ContactoGracia;
        bool suena = Time.timeScale > 0f && !rb.isKinematic && tocando && velocidad > velocidadMinima;

        float objetivo = suena ? Mathf.Clamp01((velocidad - velocidadMinima) / Mathf.Max(0.01f, velocidadPlena - velocidadMinima)) : 0f;
        nivel = Mathf.MoveTowards(nivel, objetivo, Time.unscaledDeltaTime / fundido);

        audioSource.volume = nivel * volumen * EcoTilt.Audio.AudioPreferences.Effects;
        audioSource.pitch = Mathf.Lerp(tono.x, tono.y, Mathf.Clamp01(velocidad / velocidadPlena));

        if (nivel > 0.001f)
        {
            if (!audioSource.isPlaying) audioSource.Play();
        }
        else if (audioSource.isPlaying)
        {
            audioSource.Pause(); // ya está en silencio: pausar no se oye y ahorra mezcla
        }
    }

    void OnDisable()
    {
        nivel = 0f;
        if (audioSource != null)
        {
            audioSource.volume = 0f;
            if (audioSource.isPlaying) audioSource.Pause();
        }
    }

    /// <summary>
    /// Recorta el silencio del clip y une el final con el principio con un fundido cruzado.
    /// Se hace una sola vez por clip y se comparte entre todos los residuos que lo usan.
    /// </summary>
    private static AudioClip LoopContinuo(AudioClip original)
    {
        if (Loops.TryGetValue(original, out AudioClip cached) && cached != null) return cached;
        AudioClip resultado = original;
        try
        {
            if (original.loadState != AudioDataLoadState.Loaded) original.LoadAudioData();
            int canales = original.channels;
            var datos = new float[original.samples * canales];
            if (original.loadState == AudioDataLoadState.Loaded && original.GetData(datos, 0))
            {
                // Zona con sonido: desde el primer hasta el último instante por encima del 10 % del pico.
                float pico = 0f;
                foreach (float s in datos) pico = Mathf.Max(pico, Mathf.Abs(s));
                float umbral = pico * 0.1f;
                int primero = 0, ultimo = original.samples - 1;
                while (primero < ultimo && Mathf.Abs(datos[primero * canales]) < umbral) primero++;
                while (ultimo > primero && Mathf.Abs(datos[ultimo * canales]) < umbral) ultimo--;
                int largo = ultimo - primero + 1;
                int cruce = Mathf.Min(Mathf.RoundToInt(original.frequency * 0.06f), largo / 4);
                int final = largo - cruce;
                if (pico > 0f && final > original.frequency / 20) // al menos 50 ms de sonido
                {
                    var loop = new float[final * canales];
                    for (int i = 0; i < final; i++)
                    {
                        float t = i < cruce ? (float)i / cruce : 1f;
                        for (int c = 0; c < canales; c++)
                        {
                            float actual = datos[(primero + i) * canales + c];
                            float cola = i < cruce ? datos[(primero + final + i) * canales + c] : 0f;
                            loop[i * canales + c] = actual * t + cola * (1f - t);
                        }
                    }
                    resultado = AudioClip.Create(original.name + "_loop", final, canales, original.frequency, false);
                    resultado.SetData(loop, 0);
                }
            }
        }
        catch (System.Exception)
        {
            resultado = original; // si el clip no permite leer sus datos, se usa tal cual
        }
        Loops[original] = resultado;
        return resultado;
    }
}
