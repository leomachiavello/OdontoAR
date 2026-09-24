using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class NavegacionPasos : MonoBehaviour
{
    public const string NombreMenu = "menu_entrenamiento";

    private static readonly Regex PatronPaso = new Regex(@"^(.+_paso)(\d+)$", RegexOptions.IgnoreCase);

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

    public bool IrAPasoPorNombre(string nombre)
    {
        for (int i = 0; i < pasos.Length; i++)
        {
            if (pasos[i] != null && string.Equals(pasos[i].name, nombre, StringComparison.OrdinalIgnoreCase))
            {
                IrAPaso(i);
                return true;
            }
        }

        return false;
    }

    public bool IrAlSiguiente()
    {
        Match paso = PatronPaso.Match(NombrePasoActual ?? "");
        return paso.Success && IrAPasoPorNombre(paso.Groups[1].Value + (int.Parse(paso.Groups[2].Value) + 1));
    }

    public bool IrAlAnterior()
    {
        Match paso = PatronPaso.Match(NombrePasoActual ?? "");
        if (!paso.Success)
            return false;

        int numero = int.Parse(paso.Groups[2].Value);
        return numero > 1
            ? IrAPasoPorNombre(paso.Groups[1].Value + (numero - 1))
            : IrAPasoPorNombre(NombreMenu);
    }
}
