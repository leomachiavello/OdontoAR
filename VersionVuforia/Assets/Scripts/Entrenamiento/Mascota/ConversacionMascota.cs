using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ConversacionMascota : MonoBehaviour
{
    private const string FraseNoEntendi = "No te escuché bien. ¿Puedes repetirlo?";
    private const string FraseError = "Tuve un problema para responderte. Inténtalo de nuevo en un momento.";

    [SerializeField] private float duracionMinima = 0.5f;
    [SerializeField] private int mensajesDeMemoria = 6;

    private Transform mascota;
    private NarradorMascota narrador;
    private GestorSecuenciaPasos gestor;
    private AnimacionHablar animacion;
    private ContenidoMascota contenido;
    private BoxCollider colisionMascota;
    private Coroutine procesoActual;
    private readonly List<GroqClient.Mensaje> historial = new List<GroqClient.Mensaje>();
    private bool activo;
    private bool grabando;
    private bool presionadoAntes;

    public void Configurar(Transform mascotaTano, NarradorMascota narradorMascota, GestorSecuenciaPasos gestorPasos)
    {
        mascota = mascotaTano;
        narrador = narradorMascota;
        gestor = gestorPasos;
        contenido = ContenidoMascota.Cargar();
        animacion = mascota.GetComponent<AnimacionHablar>();

        CrearColision();

        narrador.AudioIniciado += AlIniciarAudio;
        if (gestor != null)
            gestor.PasoCambiado += AlCambiarPaso;
    }

    public void Activar()
    {
        activo = true;
    }

    void OnDestroy()
    {
        if (narrador != null)
            narrador.AudioIniciado -= AlIniciarAudio;
        if (gestor != null)
            gestor.PasoCambiado -= AlCambiarPaso;
    }

    void Update()
    {
        if (!activo)
            return;

        bool presionado = LeerPuntero(out Vector2 posicion);

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

        if (animacion != null)
            animacion.Escuchando(true);
    }

    private void TerminarEscucha()
    {
        grabando = false;
        byte[] wav = GrabadorVoz.Detener(out float segundos);

        if (wav == null || segundos < duracionMinima)
        {
            if (animacion != null)
                animacion.Escuchando(false);
            return;
        }

        procesoActual = StartCoroutine(ProcesarPregunta(wav));
    }

    private IEnumerator ProcesarPregunta(byte[] wav)
    {
        string pregunta = null;
        IEnumerator transcripcion = GroqClient.Transcribir(wav, r => pregunta = r);
        while (transcripcion.MoveNext())
            yield return transcripcion.Current;

        if (string.IsNullOrWhiteSpace(pregunta))
        {
            procesoActual = null;
            narrador.Decir(FraseNoEntendi);
            yield break;
        }

        Debug.Log($"[Conversacion] Alumno: {pregunta}");

        string respuesta = null;
        IEnumerator consulta = GroqClient.Preguntar(ConstruirMensajes(pregunta), r => respuesta = r);
        while (consulta.MoveNext())
            yield return consulta.Current;

        procesoActual = null;

        if (string.IsNullOrWhiteSpace(respuesta))
        {
            narrador.Decir(FraseError);
            yield break;
        }

        Debug.Log($"[Conversacion] Tano: {respuesta}");
        Recordar(pregunta, respuesta);
        narrador.Decir(respuesta);
    }

    private List<GroqClient.Mensaje> ConstruirMensajes(string pregunta)
    {
        List<GroqClient.Mensaje> mensajes = new List<GroqClient.Mensaje>
        {
            new GroqClient.Mensaje { role = "system", content = ConstruirContexto() }
        };

        mensajes.AddRange(historial);
        mensajes.Add(new GroqClient.Mensaje { role = "user", content = pregunta });
        return mensajes;
    }

    private string ConstruirContexto()
    {
        StringBuilder contexto = new StringBuilder();

        if (contenido == null)
            return "Eres Tano, un asistente amable. Responde en espanol en maximo tres oraciones.";

        contexto.Append(contenido.persona).Append("\n\nCONTENIDO DE LA APLICACION:\n").Append(contenido.general).Append("\n\n");

        foreach (PasoContenido paso in contenido.pasos)
        {
            if (string.IsNullOrWhiteSpace(paso.explicacion))
                continue;

            contexto.Append("- ").Append(paso.panel);
            if (paso.numero > 0)
                contexto.Append(" (paso ").Append(paso.numero).Append(")");
            contexto.Append(' ').Append(paso.titulo).Append(": ").Append(paso.explicacion).Append('\n');
        }

        contexto.Append("\nNOTA: la pregunta del alumno viene de un reconocimiento de voz que puede escribir mal los terminos. Si una palabra parece una mala transcripcion de inlay, onlay u overlay, interpretala como ese termino.\n");

        PasoContenido actual = contenido.BuscarPaso(gestor != null ? gestor.NombrePasoActual : null);
        if (actual != null)
            contexto.Append("\nEl alumno esta viendo ahora: ").Append(actual.panel).Append(" (").Append(actual.titulo).Append(").");

        return contexto.ToString();
    }

    private void Recordar(string pregunta, string respuesta)
    {
        historial.Add(new GroqClient.Mensaje { role = "user", content = pregunta });
        historial.Add(new GroqClient.Mensaje { role = "assistant", content = respuesta });

        while (historial.Count > mensajesDeMemoria)
            historial.RemoveAt(0);
    }

    private void AlIniciarAudio()
    {
        if (animacion != null)
            animacion.Escuchando(false);
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

        if (animacion != null && !grabando)
            animacion.Escuchando(false);
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

    // Con touch, IsPointerOverGameObject(-1) consulta el mouse y puede fallar;
    // hay que pasarle el id del dedo.
    private bool PunteroSobreUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());

        return EventSystem.current.IsPointerOverGameObject(-1);
    }

    private bool PuntoSobreMascota(Vector2 posicion)
    {
        if (PunteroSobreUI())
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