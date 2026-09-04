using UnityEngine;

public class PanelFlotante : MonoBehaviour
{
    [SerializeField] private Transform camara;
    [SerializeField] private float distancia = 1.2f;
    [SerializeField] private float alturaOffset = 0f;
    [SerializeField] private float velocidadSeguimiento = 5f;
    [SerializeField] private bool mirarSiempreACamara = true;

    void Start()
    {
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (camara == null) return;

        Vector3 posicionObjetivo = camara.position + camara.forward * distancia + Vector3.up * alturaOffset;
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, Time.deltaTime * velocidadSeguimiento);

        if (mirarSiempreACamara)
        {
            Vector3 direccion = transform.position - camara.position;
            transform.rotation = Quaternion.LookRotation(direccion);
        }
    }
}
