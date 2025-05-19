using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MusicZone
{
    public string zoneName; // Nombre de la zona
    public AudioSource audioSource; // AudioSource para el loop
    public AudioClip audioClip; // Clip que se reproducirá en esta zona
}

public class MusicManager : MonoBehaviour
{
    [Header("Loop Zones")]
    public AudioSource mainLoop; // AudioSource del loop principal
    public List<MusicZone> musicZones = new List<MusicZone>(); // Lista de zonas de música

    [Header("Volume Parameters")]
    public float fadeDuration = 1.0f; // Duración del fade in/out
    public float maxVolume = 0.0f;
    public float minVolume = 0.0f;
    public float mainLoop_volume = 1.0f;
    public string silenceZoneName = "Silence"; // Nombre especial para la zona de silencio
    public bool multipleLoops = false; // Permitir múltiples loops activos simultáneamente

    [Header("Zones Parameters")]
    public List<string> activeZones = new List<string>(); // Zonas actualmente activas
    private List<string> savedZones = new List<string>(); // Zonas activas antes del silencio
    private bool isSilent = false; // Indica si estamos en la Silence Zone

    void Start()
    {
        // Iniciar el loop principal
        mainLoop.volume = mainLoop_volume;
        mainLoop.Play();

        // Iniciar todos los loops con volumen 0
        foreach (MusicZone zone in musicZones)
        {
            zone.audioSource.clip = zone.audioClip;
            zone.audioSource.volume = 0;
            zone.audioSource.Play();
        }
    }

    public void InMusic(string zoneName)
    {
        if (zoneName == silenceZoneName)
        {
            Silence();
            return;
        }

        if (isSilent) return; // No activar música si estamos en modo silencio

        MusicZone zone = musicZones.Find(z => z.zoneName == zoneName);
        if (zone != null)
        {
            if (multipleLoops)
            {
                // Agregar esta zona a la lista activa si no está ya activa
                if (!activeZones.Contains(zoneName))
                {
                    activeZones.Add(zoneName);
                    StartCoroutine(FadeIn(zone.audioSource));
                }
            }
            else
            {
                // Si no permitimos múltiples loops, desactivar todos menos esta zona
                StopAllZones();
                StartCoroutine(FadeIn(zone.audioSource));
                activeZones.Clear();
                activeZones.Add(zoneName);
            }
        }
    }

    public void OutMusic(string zoneName)
    {
        if (zoneName == silenceZoneName)
        {
            RestoreMusic();
            return;
        }
        
        if (activeZones.Contains(zoneName))
        {

            MusicZone zone = musicZones.Find(z => z.zoneName == zoneName);
            if (zone != null)
            {
                StartCoroutine(FadeOut(zone.audioSource));
                activeZones.Remove(zoneName);
            }
        }
    }

    public void Silence()
    {
        if (isSilent) return; // Evitar doble silenciamiento
        isSilent = true;

        // Guardar las zonas actualmente activas y silenciarlas
        savedZones = new List<string>(activeZones);
        StopAllZones();
        StartCoroutine(FadeOut(mainLoop));
    }

    public void RestoreMusic()
    {
        if (!isSilent) return; // Evitar restaurar si no está en modo silencio
        isSilent = false;

        // Restaurar las zonas activas y el loop principal
        StartCoroutine(FadeIn(mainLoop));
        foreach (string zoneName in savedZones)
        {
            InMusic(zoneName); // Reactivar cada zona que estaba activa
        }
        savedZones.Clear();
    }

    private void StopAllZones()
    {
        foreach (MusicZone zone in musicZones)
        {
            StartCoroutine(FadeOut(zone.audioSource));
        }
        activeZones.Clear();
    }
    
    public void MuteZone(bool isEnteringMuteZone)
{
    if (isEnteringMuteZone)
    {
        if (isSilent) return; // Evitar silenciar si ya estamos en silencio
        isSilent = true;

        // Guardar las zonas activas actuales y silenciar todo
        savedZones = new List<string>(activeZones);
        StopAllZones();
        StartCoroutine(FadeOut(mainLoop));
    }
    else
    {
        if (!isSilent) return; // Evitar restaurar si no estamos en silencio
        isSilent = false;

        // Restaurar el mainLoop y las zonas activas guardadas
        StartCoroutine(FadeIn(mainLoop));
        foreach (string zoneName in savedZones)
        {
            InMusic(zoneName); // Restaurar cada zona activa previamente
        }
        savedZones.Clear();
    }
}


    private IEnumerator FadeIn(AudioSource audioSource)
    {
        float startVolume = audioSource.volume;
        float targetVolume = maxVolume;
        float elapsedTime = 0.0f;

        while (elapsedTime < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOut(AudioSource audioSource)
    {
        float startVolume = audioSource.volume;
        float targetVolume = minVolume;
        float elapsedTime = 0.0f;

        while (elapsedTime < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}