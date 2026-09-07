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
        if (colocado || raycastManager == null)
            return;

        Vector2? posicionToque = LeerToque();
        if (posicionToque == null)
            return;

        if (raycastManager.Raycast(posicionToque.Value, hits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit hit = hits[0];
            bool esPared = false;

            if (planeManager != null)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null)
                    esPared = plane.alignment == PlaneAlignment.Vertical;
            }

            if (esPared)
                ColocarEnPared(hit.pose);
            else
                ColocarFlotandoSobrePiso(hit.pose);

            colocado = true;

            if (spawnTrigger != null)
                spawnTrigger.enabled = true;
        }
    }

    void ColocarFlotandoSobrePiso(Pose pose)
    {
        float alturaCanvas = ObtenerAlturaMundo();
        transform.position = pose.position + Vector3.up * (alturaCanvas * 0.5f + alturaFlotante);
        MirarACamara();
    }

    void ColocarEnPared(Pose pose)
    {
        transform.position = pose.position + pose.up * distanciaPared;
        transform.rotation = Quaternion.LookRotation(pose.up, Vector3.up);
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
