using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

public class PanelFlotante : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARInteractorSpawnTrigger spawnTrigger;
    [SerializeField] private Transform camara;
    [SerializeField] private float alturaFlotante = 0.2f;
    [SerializeField] private float distanciaPared = 0.05f;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private RectTransform rectTransform;
    private bool colocado = false;
    private bool esperandoSoltarToque = false;

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

        if (spawnTrigger != null)
            spawnTrigger.enabled = false;
    }

    void Update()
    {
        if (colocado)
        {
            if (esperandoSoltarToque && spawnTrigger != null && !HayToqueActivo())
            {
                spawnTrigger.enabled = true;
                esperandoSoltarToque = false;
            }
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

            colocado = true;
            esperandoSoltarToque = true;
        }
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

    bool HayToqueActivo()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;

        return false;
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
