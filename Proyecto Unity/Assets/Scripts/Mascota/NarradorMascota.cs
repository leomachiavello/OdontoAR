using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarradorMascota : MonoBehaviour
{
    private NavegacionPasos navegacion;
    private ContenidoMascota contenido;
    private AudioSource fuenteAudio;
    private AnimacionHablar animacion;
    private Coroutine narracionActual;
    private AudioClip clipActual;
    private bool activo;
    private bool saludoDicho;

    public event Action AudioIniciado;

    public void Configurar(Transform mascota, NavegacionPasos navegacionPasos)
    {
        navegacion = navegacionPasos;
        contenido = ContenidoMascota.Cargar();

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
        if (contenido == null)
            return;

        List<string> textos = new List<string>();

        if (!saludoDicho)
        {
            saludoDicho = true;
            if (!string.IsNullOrWhiteSpace(contenido.saludo))
                textos.Add(contenido.saludo);
        }

        string guion = GuionDelPasoActual();
        if (guion != null)
            textos.Add(guion);

        Narrar(textos);
    }

    private void AlCambiarPaso(int indice)
    {
        if (!activo || contenido == null)
            return;

        string guion = GuionDelPasoActual();
        if (guion != null)
            Narrar(new List<string> { guion });
        else
            Detener();
    }

    private string GuionDelPasoActual()
    {
        string panel = navegacion != null ? navegacion.NombrePasoActual : null;
        return contenido.ConstruirGuion(contenido.BuscarPaso(panel));
    }

    private void Narrar(List<string> textos)
    {
        Detener();
        if (textos.Count > 0)
            narracionActual = StartCoroutine(ReproducirSecuencia(textos));
    }

    public void Decir(string texto)
    {
        Narrar(new List<string> { texto });
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

    private IEnumerator ReproducirSecuencia(List<string> textos)
    {
        foreach (string texto in textos)
        {
            AudioClip clip = null;
            IEnumerator obtener = ElevenLabsClient.ObtenerAudio(texto, resultado => clip = resultado);
            while (obtener.MoveNext())
                yield return obtener.Current;

            if (clip == null)
                continue;

            if (clipActual != null)
                Destroy(clipActual);

            clipActual = clip;
            fuenteAudio.clip = clip;
            fuenteAudio.Play();

            AudioIniciado?.Invoke();

            if (animacion != null && contenido != null && texto == contenido.saludo)
                animacion.Saludar();

            while (fuenteAudio.isPlaying)
                yield return null;

            yield return new WaitForSeconds(0.3f);
        }

        narracionActual = null;
    }
}
