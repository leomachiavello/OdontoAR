using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class VerificarVocesTano
{
    [MenuItem("OdontoAR/Verificar audios de Tano")]
    public static void Verificar()
    {
        AssetDatabase.Refresh();

        List<string> requeridos = CatalogoVoces.NombresRequeridos();
        List<string> faltan = new List<string>();
        int definitivos = 0;
        int provisionales = 0;

        foreach (string nombre in requeridos)
        {
            if (CatalogoVoces.CargarDefinitivo(nombre) != null)
                definitivos++;
            else if (CatalogoVoces.Cargar(nombre) != null)
                provisionales++;
            else
                faltan.Add(nombre);
        }

        Debug.Log($"[Voces de Tano] Audios definitivos (Resources/Voces): {definitivos} de {requeridos.Count}. Provisionales: {provisionales}. Sin audio: {faltan.Count}.");

        if (faltan.Count > 0)
            Debug.LogWarning("[Voces de Tano] Sin audio: " + string.Join(", ", faltan));
    }
}
