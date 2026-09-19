using System;
using UnityEngine;

public class NavegacionPasos : MonoBehaviour
{
    [SerializeField] private GameObject[] pasos;

    public int PasoActual { get; private set; }
    public event Action<int> PasoCambiado;

    public string NombrePasoActual =>
        pasos != null && PasoActual >= 0 && PasoActual < pasos.Length ? pasos[PasoActual].name : null;

    void Start() => IrAPaso(0);

    public void IrAPaso(int indice)
    {
        for (int i = 0; i < pasos.Length; i++)
            pasos[i].SetActive(i == indice);

        PasoActual = indice;
        PasoCambiado?.Invoke(indice);
    }
}
