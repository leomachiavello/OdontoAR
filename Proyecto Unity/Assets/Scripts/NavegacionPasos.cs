using UnityEngine;

public class NavegacionPasos : MonoBehaviour
{
    [SerializeField] private GameObject[] pasos;
    private int pasoActual = 0;

    void Start() => MostrarPaso(0);

    public void Siguiente()
    {
        if (pasoActual < pasos.Length - 1)
            MostrarPaso(++pasoActual);
    }

    public void Anterior()
    {
        if (pasoActual > 0)
            MostrarPaso(--pasoActual);
    }

    private void MostrarPaso(int indice)
    {
        for (int i = 0; i < pasos.Length; i++)
            pasos[i].SetActive(i == indice);
    }
}