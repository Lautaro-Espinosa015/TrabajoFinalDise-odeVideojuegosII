using System;
using TMPro;
using UnityEngine;

/// <summary>Demostración visual aislada: no usa sensores, físicas ni reglas de la partida.</summary>
public sealed class MenuBoardDemo : MonoBehaviour
{
    [Serializable]
    public sealed class Track
    {
        public Transform item;
        public Vector3[] points;
        public string category;
        public bool jump;
    }

    public Transform board;
    public Track[] tracks;
    public TMP_Text caption;
    public float travelSeconds = 22f;
    private float clock;
    private int current;
    private Vector3 previous;
    private Quaternion rest;

    private void Awake()
    {
        rest = board.localRotation;
        foreach (var track in tracks) track.item.gameObject.SetActive(false);
        previous = tracks[0].points[0];
    }

    private void Update()
    {
        if (tracks == null || tracks.Length == 0) return;
        clock += Time.unscaledDeltaTime;
        float cycle = travelSeconds + 3f;
        if (clock >= cycle)
        {
            tracks[current].item.gameObject.SetActive(false);
            clock %= cycle;
            current = (current + 1) % tracks.Length;
            previous = tracks[current].points[0];
        }
        Track track = tracks[current];
        track.item.gameObject.SetActive(clock < travelSeconds + 2f);
        float total = 0f;
        for (int i = 1; i < track.points.Length; i++) total += Vector3.Distance(track.points[i - 1], track.points[i]);
        float remaining = Mathf.Clamp01(clock / travelSeconds) * total;
        int segment = 1;
        for (; segment < track.points.Length - 1; segment++)
        {
            float distance = Vector3.Distance(track.points[segment - 1], track.points[segment]);
            if (remaining <= distance) break;
            remaining -= distance;
        }
        Vector3 from = track.points[segment - 1], to = track.points[segment];
        float t = Mathf.Clamp01(remaining / Mathf.Max(0.001f, Vector3.Distance(from, to)));
        Vector3 point = Vector3.Lerp(from, to, t);
        bool jumping = track.jump && segment == track.points.Length - 1 && clock < travelSeconds;
        if (jumping) point.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
        track.item.localPosition = point;
        Vector3 motion = point - previous;
        if (motion.sqrMagnitude > 0.00001f)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, motion.normalized);
            track.item.Rotate(axis, motion.magnitude * 180f, Space.Self);
        }
        Vector3 velocity = motion / Mathf.Max(0.001f, Time.unscaledDeltaTime);
        Quaternion tilt = Quaternion.Euler(Mathf.Clamp(velocity.z * 1.3f, -5f, 5f), 0f,
            Mathf.Clamp(-velocity.x * 1.3f, -5f, 5f));
        board.localRotation = Quaternion.Slerp(board.localRotation, rest * tilt, Time.unscaledDeltaTime * 2.5f);
        previous = point;
        if (caption != null)
            caption.text = clock >= travelSeconds ? "¡Bien separado!" : jumping ? "Saltá hasta el contenedor" : "Incliná y guiá · " + track.category;
    }
}
