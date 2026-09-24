using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

public class ConversacionMascota : MonoBehaviour
{
    [SerializeField] private float duracionMinima = 0.5f;

    private Transform mascota;
    private NarradorMascota narrador;
    private NavegacionPasos navegacion;
    private AnimacionHablar animacion;
    private ARInteractorSpawnTrigger spawnTrigger;
    private IntencionesLocal intenciones;
    private BoxCollider colisionMascota;
    private Coroutine procesoActual;
    private bool activo;
    private bool grabando;
    private bool presionadoAntes;
    private bool esperandoSoltar;

    public void Configurar(Transform mascotaTano, NarradorMascota narradorMascota, NavegacionPasos navegacionPasos, ARInteractorSpawnTrigger disparador)
    {
        mascota = mascotaTano;
        narrador = narradorMascota;
        navegacion = navegacionPasos;
        spawnTrigger = disparador;
        intenciones = IntencionesLocal.Crear();
        animacion = mascota.GetComponent<AnimacionHablar>();

        CrearColision();

        narrador.AudioIniciado += AlIniciarAudio;
        if (navegacion != null)
            navegacion.PasoCambiado += AlCambiarPaso;
    }

    public void Activar()
    {
        activo = true;
    }

    void OnDestroy()
    {
        if (narrador != null)
            narrador.AudioIniciado -= AlIniciarAudio;
        if (navegacion != null)
            navegacion.PasoCambiado -= AlCambiarPaso;
    }

    void Update()
    {
        if (!activo)
            return;

        bool presionado = LeerPuntero(out Vector2 posicion);

        if (esperandoSoltar && !presionado)
        {
            esperandoSoltar = false;
            if (spawnTrigger != null)
                spawnTrigger.enabled = true;
        }

        if (presionado && !presionadoAntes && !grabando && PuntoSobreMascota(posicion))
            IniciarEscucha();
        else if (!presionado && presionadoAntes && grabando)
            TerminarEscucha();

        presionadoAntes = presionado;
    }

    private void IniciarEscucha()
    {
        if (!GrabadorVoz.Iniciar())
            return;

        grabando = true;
        CancelarProceso();
        narrador.Detener();

        if (spawnTrigger != null)
        {
            spawnTrigger.enabled = false;
            esperandoSoltar = true;
        }

        if (animacion != null)
            animacion.Escuchando(true);
    }

    private void TerminarEscucha()
    {
        grabando = false;
        byte[] wav = GrabadorVoz.Detener(out float segundos);

        if (wav == null || segundos < duracionMinima)
        {
            DejarDeEscuchar();
            return;
        }

        procesoActual = StartCoroutine(ProcesarPregunta(wav));
    }

    private IEnumerator ProcesarPregunta(byte[] wav)
    {
        string pregunta = null;
        IEnumerator transcripcion = WitClient.Transcribir(wav, r => pregunta = r);
        while (transcripcion.MoveNext())
            yield return transcripcion.Current;

        ResultadoIntencion resultado = Clasificar(pregunta);
        string origen = "Wit";
        string textoWit = pregunta;
        string textoGroq = null;

        if (NecesitaRespaldo(resultado) && GroqDisponible())
        {
            string alternativa = null;
            IEnumerator respaldo = GroqClient.Transcribir(wav, r => alternativa = r);
            while (respaldo.MoveNext())
                yield return respaldo.Current;

            textoGroq = alternativa;
            ResultadoIntencion otro = Clasificar(alternativa);
            if (otro != null && (resultado == null || otro.puntaje > resultado.puntaje))
            {
                pregunta = alternativa;
                resultado = otro;
                origen = "Groq (respaldo)";
            }
        }

        procesoActual = null;

        if (string.IsNullOrWhiteSpace(pregunta))
        {
            Debug.Log($"[Conversacion] No se entendio nada | Wit: '{textoWit}' | Groq: '{textoGroq}'");
            Responder(CatalogoVoces.NoEscuche);
            yield break;
        }

        string nombre = resultado != null && resultado.intencion != null ? resultado.intencion.nombre : "(ninguna)";
        float puntaje = resultado != null ? resultado.puntaje : 0f;
        Debug.Log($"[Conversacion] ({origen}) Alumno: {pregunta} | Intencion: {nombre} ({puntaje:0.00}) | Wit: '{textoWit}' | Groq: '{textoGroq}'");

        Ejecutar(resultado);
    }

    private ResultadoIntencion Clasificar(string texto)
    {
        return intenciones == null || string.IsNullOrWhiteSpace(texto) ? null : intenciones.Clasificar(texto);
    }

    private bool NecesitaRespaldo(ResultadoIntencion resultado)
    {
        return resultado == null || resultado.intencion == null || resultado.puntaje < intenciones.ConfianzaAlta;
    }

    private static bool GroqDisponible()
    {
        DatosApi datos = ConfigApi.Datos;
        return datos != null && datos.groqRespaldo && !string.IsNullOrEmpty(datos.groqApiKey);
    }

    private void Ejecutar(ResultadoIntencion resultado)
    {
        if (resultado == null || resultado.intencion == null)
        {
            Responder(CatalogoVoces.NoSeguro);
            return;
        }

        switch (resultado.intencion.accion)
        {
            case "repetir":
                if (!narrador.RepetirPaso())
                    DejarDeEscuchar();
                break;

            case "siguiente":
                if (navegacion == null)
                    DejarDeEscuchar();
                else if (navegacion.NombrePasoActual == NavegacionPasos.NombreMenu)
                    Responder(CatalogoVoces.EligeRuta);
                else if (!navegacion.IrAlSiguiente())
                    Responder(CatalogoVoces.UltimoPaso);
                else
                    DejarDeEscuchar();
                break;

            case "anterior":
                if (navegacion != null && navegacion.NombrePasoActual != NavegacionPasos.NombreMenu)
                    navegacion.IrAlAnterior();
                DejarDeEscuchar();
                break;

            case "menu":
                if (navegacion != null)
                    navegacion.IrAPasoPorNombre(NavegacionPasos.NombreMenu);
                DejarDeEscuchar();
                break;

            case "ruta":
                IrARuta(resultado.ValorDe(resultado.intencion.entidad));
                break;

            case "silenciar":
                narrador.Detener();
                DejarDeEscuchar();
                break;

            default:
                string clip = resultado.Clip;
                Responder(clip != null ? clip : CatalogoVoces.NoSeguro);
                break;
        }
    }

    private void IrARuta(string tipo)
    {
        if (string.IsNullOrEmpty(tipo) || navegacion == null)
        {
            Responder(CatalogoVoces.NoSeguro);
            return;
        }

        string paso = char.ToUpperInvariant(tipo[0]) + tipo.Substring(1) + "_paso1";
        if (navegacion.IrAPasoPorNombre(paso))
            DejarDeEscuchar();
        else
            Responder(CatalogoVoces.NoSeguro);
    }

    private void Responder(string clip)
    {
        if (!narrador.Decir(clip))
            DejarDeEscuchar();
    }

    private void DejarDeEscuchar()
    {
        if (animacion != null)
            animacion.Escuchando(false);
    }

    private void AlIniciarAudio()
    {
        DejarDeEscuchar();
    }

    private void AlCambiarPaso(int indice)
    {
        CancelarProceso();
    }

    private void CancelarProceso()
    {
        if (procesoActual != null)
        {
            StopCoroutine(procesoActual);
            procesoActual = null;
        }

        if (!grabando)
            DejarDeEscuchar();
    }

    private bool LeerPuntero(out Vector2 posicion)
    {
        posicion = default;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            posicion = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            posicion = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    private bool PuntoSobreMascota(Vector2 posicion)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
            return false;

        Camera camara = Camera.main;
        if (camara == null || colisionMascota == null || !mascota.gameObject.activeInHierarchy)
            return false;

        Physics.SyncTransforms();
        return colisionMascota.Raycast(camara.ScreenPointToRay(posicion), out _, 50f);
    }

    private void CrearColision()
    {
        bool hayLimites = false;
        Bounds limites = new Bounds();

        foreach (SkinnedMeshRenderer render in mascota.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (render.sharedMesh == null)
                continue;

            Bounds malla = render.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = malla.center + Vector3.Scale(malla.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f));

                Vector3 local = mascota.InverseTransformPoint(render.transform.TransformPoint(esquina));

                if (!hayLimites)
                {
                    limites = new Bounds(local, Vector3.zero);
                    hayLimites = true;
                }
                else
                {
                    limites.Encapsulate(local);
                }
            }
        }

        if (!hayLimites)
        {
            Debug.LogWarning("[Conversacion] No se pudo calcular el tamano de tano; no se podra hablarle presionandolo.");
            return;
        }

        colisionMascota = mascota.gameObject.AddComponent<BoxCollider>();
        colisionMascota.center = limites.center;
        colisionMascota.size = limites.size;
        colisionMascota.isTrigger = true;
    }
}
