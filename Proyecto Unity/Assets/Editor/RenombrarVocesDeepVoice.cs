using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// DeepVoice Pro siempre guarda "<Voz>_<Nombre>_<Toma>.wav" (ej. Archer_saludo_inicial_0.wav),
// pero la app busca cada audio por su nombre exacto (CatalogoVoces). Este script lo renombra al llegar.
public class RenombrarVocesDeepVoice : AssetPostprocessor
{
    private const string Carpeta = "Assets/Resources/Voces/";
    private const string CarpetaAnteriores = "Assets/DeepVoicePro/Voices/Anteriores";
    private static readonly Regex Toma = new Regex(@"_\d+$");

    static void OnPostprocessAllAssets(string[] importados, string[] borrados, string[] movidos, string[] origenesMovidos)
    {
        List<string> nuevos = new List<string>();
        foreach (string ruta in importados)
        {
            if (ruta.StartsWith(Carpeta) && ruta.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase)
                && Toma.IsMatch(Path.GetFileNameWithoutExtension(ruta)))
                nuevos.Add(ruta);
        }

        if (nuevos.Count > 0)
            EditorApplication.delayCall += () => Renombrar(nuevos);
    }

    private static void Renombrar(List<string> rutas)
    {
        List<string> requeridos = CatalogoVoces.NombresRequeridos();

        foreach (string ruta in rutas)
        {
            string sinToma = Toma.Replace(Path.GetFileNameWithoutExtension(ruta), "");

            string nombre = null;
            foreach (string requerido in requeridos)
            {
                if (sinToma.EndsWith("_" + requerido) && (nombre == null || requerido.Length > nombre.Length))
                    nombre = requerido;
            }

            if (nombre == null)
            {
                Debug.LogWarning($"[Voces de Tano] '{ruta}' no coincide con ningun audio de la app. Revisa el campo del medio de File Name.");
                continue;
            }

            // Si se regenera un audio, la version anterior se guarda fuera de Resources (no entra en la build).
            string destino = Carpeta + nombre + ".wav";
            if (AssetDatabase.LoadMainAssetAtPath(destino) != null)
            {
                if (!AssetDatabase.IsValidFolder(CarpetaAnteriores))
                    AssetDatabase.CreateFolder("Assets/DeepVoicePro/Voices", "Anteriores");
                AssetDatabase.MoveAsset(destino, AssetDatabase.GenerateUniqueAssetPath($"{CarpetaAnteriores}/{nombre}.wav"));
            }

            string error = AssetDatabase.RenameAsset(ruta, nombre);
            if (string.IsNullOrEmpty(error))
                Debug.Log($"[Voces de Tano] {Path.GetFileName(ruta)} -> {nombre}.wav");
            else
                Debug.LogError($"[Voces de Tano] No se pudo renombrar {ruta}: {error}");
        }
    }
}
