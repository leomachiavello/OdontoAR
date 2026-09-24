using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarradorMascota : MonoBehaviour
{
    private NavegacionPasos navegacion;
    private AudioSource fuenteAudio;
    private AnimacionHablar animacion;
    private Coroutine narracionActual;
    private readonly HashSet<string> avisados = new HashSet<string>();
    private bool activo;
    private bool saludoDicho;

    public event Action AudioIniciado;

    public void Configurar(Transform mascota, NavegacionPasos navegacionPasos)
    {
        navegacion = navegacionPasos;

        fuenteAudio = mascota.GetComponent<AudioSource>();
        if (fuenteAudio == null)
            fuenteAudio = mascota.gameObject.AddComponent<AudioSource>();

        fuenteAudio.playOnAwake = false;
        fuenteAudio.spatialBlend = 0f;

        animacion = mascota.GetComponent<AnimacionHablar>();
        if (animacion == null)
            animacion = mascota.gameObject.AddComponent<AnimacionHablar>();
        animacion.Configurar(fuenteAudio);

        if (navegacion != null)
            navegacion.PasoCambiado += AlCambiarPaso;
    }

    void OnDestroy()
    {
        if (navegacion != null)
            navegacion.PasoCambiado -= AlCambiarPaso;
    }

    public void IniciarNarracion()
    {
        activo = true;

        List<string> clips = new List<string>();

        if (!saludoDicho)
        {
            saludoDicho = true;
            if (Cargar(CatalogoVoces.Saludo) != null)
                clips.Add(CatalogoVoces.Saludo);
        }

        string paso = NombrePaso();
        if (Cargar(paso) != null)
            clips.Add(paso);

        Iniciar(clips);
    }

    public bool RepetirPaso()
    {
        string paso = NombrePaso();
        if (Cargar(paso) == null)
            return false;

        Iniciar(new List<string> { paso });
        return true;
    }

    public bool Decir(string clip)
    {
        if (Cargar(clip) == null)
            return false;

        Iniciar(new List<string> { clip });
        return true;
    }

    public void Detener()
    {
        if (narracionActual != null)
        {
            StopCoroutine(narracionActual);
            narracionActual = null;
        }

        if (fuenteAudio != null)
            fuenteAudio.Stop();
    }

    private void AlCambiarPaso(int indice)
    {
        if (!activo)
            return;

        string paso = NombrePaso();
        if (Cargar(paso) != null)
            Iniciar(new List<string> { paso });
        else
            Detener();
    }

    private string NombrePaso()
    {
        return navegacion != null ? navegacion.NombrePasoActual : null;
    }

    private void Iniciar(List<string> clips)
    {
        Detener();
        if (clips.Count > 0)
            narracionActual = StartCoroutine(Reproducir(clips));
    }

    private IEnumerator Reproducir(List<string> clips)
    {
        foreach (string nombre in clips)
        {
            AudioClip clip = Cargar(nombre);
            if (clip == null)
                continue;

            fuenteAudio.clip = clip;
            fuenteAudio.Play();
            AudioIniciado?.Invoke();

            if (animacion != null && nombre == CatalogoVoces.Saludo)
                animacion.Saludar();

            while (fuenteAudio.isPlaying)
                yield return null;

            yield return new WaitForSeconds(0.3f);
        }

        narracionActual = null;
    }

    private AudioClip Cargar(string nombre)
    {
        if (string.IsNullOrEmpty(nombre))
            return null;

        AudioClip clip = CatalogoVoces.Cargar(nombre);
        if (clip == null && avisados.Add(nombre))
            Debug.LogWarning($"[Narrador] Todavia no hay audio '{nombre}' en Assets/Resources/Voces.");

        return clip;
    }
}
