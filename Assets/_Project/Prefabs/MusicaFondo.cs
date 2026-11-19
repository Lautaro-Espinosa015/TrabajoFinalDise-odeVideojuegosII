using UnityEngine;

/// <summary>Agregar a un objeto dedicado y asignar la musica en el Inspector.</summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class MusicaFondo : MonoBehaviour
{
    public AudioClip musica;
    [Range(0f, 1f)] public float volumen = 0.4f;
    private AudioSource fuente;

    private void Awake()
    {
        // Prepara la musica para que se repita y se escuche en toda la escena.
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.volume = volumen;
        fuente.clip = musica;
    }

    private void OnEnable()
    {
        // Empieza al activar el objeto, si hay una cancion asignada.
        if (fuente.clip != null)
            fuente.Play();
    }

    private void OnDisable()
    {
        if (fuente != null)
            fuente.Stop();
    }

    private void Update()
    {
        // Permite ajustar el volumen durante Play desde el Inspector.
        fuente.volume = volumen;
    }
}
