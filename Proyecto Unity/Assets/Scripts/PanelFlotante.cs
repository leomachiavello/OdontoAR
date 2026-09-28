using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class PanelFlotante : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARInteractorSpawnTrigger spawnTrigger;
    [SerializeField] private Transform camara;
    [SerializeField] private float alturaFlotante = 0.2f;
    [SerializeField] private float distanciaPared = 0.05f;
    [SerializeField] private Transform mascota;
    [SerializeField] private string nombreMascota = "tano4";
    [SerializeField] private Vector3 offsetMascota = new Vector3(0.85f, -0.32f, -0.1f);
    [SerializeField] private Vector3 offsetMascotaRutas = new Vector3(0.66f, -0.32f, -0.1f);
    [SerializeField] private float rotacionYMascota = -130f;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private RectTransform rectTransform;
    private bool colocado = false;
    private NavegacionPasos navegacion;
    private NarradorMascota narrador;
    private ConversacionMascota conversacion;
    private Vector3 posicionObjetivoMascota;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;

        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();

        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();

        if (spawnTrigger == null)
            spawnTrigger = FindObjectOfType<ARInteractorSpawnTrigger>();

        DesactivarPlantillaAR();

        if (mascota == null)
        {
            GameObject go = GameObject.Find(nombreMascota);
            if (go != null)
                mascota = go.transform;
        }

        if (mascota != null)
            mascota.gameObject.SetActive(false);

        navegacion = GetComponent<NavegacionPasos>();
        if (navegacion != null)
            navegacion.PasoCambiado += AlCambiarPaso;

        if (mascota != null)
        {
            narrador = GetComponent<NarradorMascota>();
            if (narrador == null)
                narrador = gameObject.AddComponent<NarradorMascota>();
            narrador.Configurar(mascota, navegacion);

            conversacion = GetComponent<ConversacionMascota>();
            if (conversacion == null)
                conversacion = gameObject.AddComponent<ConversacionMascota>();
            conversacion.Configurar(mascota, narrador, navegacion);
        }
    }

    void OnDestroy()
    {
        if (navegacion != null)
            navegacion.PasoCambiado -= AlCambiarPaso;
    }

    void Update()
    {
        if (colocado)
        {
            SeguirPosicionMascota();
            return;
        }

        if (raycastManager == null)
            return;

        Vector2? posicionToque = LeerToque();
        if (posicionToque == null)
            return;

        if (raycastManager.Raycast(posicionToque.Value, hits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit hit = hits[0];
            ARPlane plane = planeManager != null ? planeManager.GetPlane(hit.trackableId) : null;
            bool esPared = plane != null && plane.alignment == PlaneAlignment.Vertical;

            if (esPared)
                ColocarEnPared(hit.pose, plane);
            else
                ColocarFlotandoSobrePiso(hit.pose);

            ColocarMascota();

            if (narrador != null)
                narrador.IniciarNarracion();

            if (conversacion != null)
                conversacion.Activar();

            DetenerDeteccionPlanos();
            colocado = true;
        }
    }

    // La app no usa la creacion de objetos del template de AR (cubos, piramides, etc.) ni su menu de opciones.
    void DesactivarPlantillaAR()
    {
        if (spawnTrigger != null)
            spawnTrigger.enabled = false;

        ObjectSpawner spawner = FindObjectOfType<ObjectSpawner>();
        if (spawner != null)
            spawner.gameObject.SetActive(false);

        ARTemplateMenuManager menu = FindObjectOfType<ARTemplateMenuManager>();
        if (menu == null)
            return;

        // El menu del template es quien asigna el visual de los planos; se conserva para poder ubicar el canvas.
        if (planeManager != null && menu.debugPlane != null)
            planeManager.planePrefab = menu.debugPlane;

        // Solo se apaga lo del template: en el mismo objeto UI viven botones propios (OcultarMostrarButton, AtrasBoton).
        menu.enabled = false;
        OcultarUI(menu.createButton);
        OcultarUI(menu.deleteButton);
        OcultarUI(menu.transform.Find("Options Button"));
        OcultarUI(menu.modalMenu);
        OcultarUI(menu.objectMenuAnimator);
        OcultarUI(menu.debugMenu);
    }

    static void OcultarUI(Component componente)
    {
        if (componente != null)
            componente.gameObject.SetActive(false);
    }

    static void OcultarUI(GameObject objeto)
    {
        if (objeto != null)
            objeto.SetActive(false);
    }

    void DetenerDeteccionPlanos()
    {
        if (planeManager == null)
            return;

        planeManager.requestedDetectionMode = PlaneDetectionMode.None;
        foreach (ARPlane plano in planeManager.trackables)
            plano.gameObject.SetActive(false);
        planeManager.enabled = false;
    }

    void ColocarFlotandoSobrePiso(Pose pose)
    {
        float alturaCanvas = ObtenerAlturaMundo();
        transform.position = pose.position + Vector3.up * (alturaCanvas * 0.5f + alturaFlotante);
        MirarACamara();
    }

    void ColocarEnPared(Pose pose, ARPlane plane)
    {
        Vector3 normal = plane != null ? plane.normal : pose.up;
        transform.position = pose.position + normal * distanciaPared;
        MirarACamara();
    }

    void ColocarMascota()
    {
        if (mascota == null)
            return;

        posicionObjetivoMascota = CalcularPosicionMascota();
        mascota.position = posicionObjetivoMascota;
        mascota.rotation = transform.rotation * Quaternion.Euler(0f, rotacionYMascota, 0f);
        mascota.gameObject.SetActive(true);
    }

    Vector3 CalcularPosicionMascota()
    {
        bool enMenu = navegacion == null || navegacion.PasoActual == 0;
        Vector3 offset = enMenu ? offsetMascota : offsetMascotaRutas;

        return transform.position
            + transform.right * offset.x
            + transform.up * offset.y
            + transform.forward * offset.z;
    }

    void AlCambiarPaso(int indice)
    {
        if (colocado && mascota != null)
            posicionObjetivoMascota = CalcularPosicionMascota();
    }

    void SeguirPosicionMascota()
    {
        if (mascota == null || !mascota.gameObject.activeSelf)
            return;

        mascota.position = Vector3.Lerp(mascota.position, posicionObjetivoMascota, 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    float ObtenerAlturaMundo()
    {
        if (rectTransform == null)
            return 0f;

        return rectTransform.rect.height * transform.lossyScale.y;
    }

    void MirarACamara()
    {
        if (camara == null)
            return;

        Vector3 direccion = transform.position - camara.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direccion);
    }

    Vector2? LeerToque()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return Mouse.current.position.ReadValue();

        return null;
    }
}
