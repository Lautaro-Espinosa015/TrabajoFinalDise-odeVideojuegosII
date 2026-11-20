using UnityEngine;

namespace EcoTilt.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SonidoVictoria : MonoBehaviour
    {
        [SerializeField] private LevelProgress progress;
        [SerializeField] private AudioClip sonido;
        [SerializeField, Range(0f, 1f)] private float volumen = 0.7f;
        private AudioSource source;
        private AudioClip generated;
        private int lastLevel = -1;

        private void Awake()
        {
            // Fuente exclusiva: nunca reutilizar la de la música.
            var child = new GameObject("AudioVictoria");
            child.transform.SetParent(transform, false);
            source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (progress == null) progress = GetComponent<LevelProgress>();
            if (progress != null) progress.LevelCompleted += OnCompleted;
        }

        private void OnDisable()
        {
            if (progress != null) progress.LevelCompleted -= OnCompleted;
            if (source != null) source.Stop();
        }

        private void OnCompleted(int level, int correct, int total)
        {
            if (lastLevel == level) return;
            lastLevel = level;
            if (sonido == null && generated == null) generated = CreateMelody();
            source.PlayOneShot(sonido != null ? sonido : generated, volumen * PlayerPrefs.GetFloat("VolumenEfectos", 0.85f));
        }

        private static AudioClip CreateMelody()
        {
            const int rate = 44100;
            const float duration = 0.22f;
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int length = Mathf.RoundToInt(rate * duration);
            float[] data = new float[length * notes.Length];
            for (int n = 0; n < notes.Length; n++)
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)rate;
                    float envelope = Mathf.Min(t / 0.012f, 1f) * Mathf.Clamp01((duration - t) / 0.09f);
                    data[n * length + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * envelope * 0.35f;
                }
            AudioClip clip = AudioClip.Create("VictoriaGenerada", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (generated != null) Destroy(generated);
            if (source != null) Destroy(source.gameObject);
        }
    }
}
