using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// Genera con DeepVoice Pro todos los audios de Tano que aun no existen en Resources/Voces,
// usando los textos de "Info util/deepvoice_textos.txt" y el Invoice Number guardado en la ventana de DeepVoice Pro.
public static class GenerarVocesTano
{
    // Misma configuracion para todos los audios, para que Tano suene siempre igual.
    private const string Voz = "Breeze";
    private const string Instrucciones =
        "Accent: Neutral Latin American Spanish, clear and natural.\n" +
        "Tone: Warm, friendly and encouraging, like a kind tutor talking to a dental student.\n" +
        "Pacing: Moderate, with natural pauses between ideas.\n" +
        "Emotion: Cheerful and patient, never monotone.\n" +
        "Personality: A friendly mascot who explains things simply and with enthusiasm.";

    private const string Url = "https://dvpro.aikodex.com/invoice";
    private const string ClaveInvoice = "DeepVoicePro_Invoice";
    private const string Carpeta = "Assets/Resources/Voces";
    private const float PausaEntrePeticiones = 2f;

    private static readonly Regex Cabecera = new Regex(@"FILE NAME:\s*(\S+)");
    private static EditorCoroutine proceso;

    [Serializable]
    private class Peticion
    {
        public string text;
        public string model;
        public string invoice;
        public string name;
        public string instructions;
    }

    [MenuItem("OdontoAR/Generar audios faltantes de Tano (DeepVoice)")]
    public static void Generar()
    {
        if (proceso != null)
        {
            Debug.LogWarning("[Voces de Tano] Ya se estan generando audios.");
            return;
        }

        string invoice = PlayerPrefs.GetString(ClaveInvoice, "");
        if (string.IsNullOrWhiteSpace(invoice))
        {
            EditorUtility.DisplayDialog("Falta el Invoice Number",
                "Abre Window > DeepVoice Pro, escribe el Invoice Number y pulsa Verify y Save.", "OK");
            return;
        }

        List<KeyValuePair<string, string>> pendientes = Pendientes(out int caracteres);
        if (pendientes == null)
            return;

        if (pendientes.Count == 0)
        {
            EditorUtility.DisplayDialog("Audios de Tano", "Todos los audios ya existen en Resources/Voces.", "OK");
            return;
        }

        bool confirmar = EditorUtility.DisplayDialog("Generar audios de Tano",
            $"Se generaran {pendientes.Count} audios ({caracteres} caracteres de la cuota de DeepVoice).\n\n" +
            $"Voz: {Voz} (modelo Instruct)\n\nTarda unos 5 a 10 segundos por audio. Se puede cancelar desde la barra de progreso.",
            "Generar", "Cancelar");

        if (confirmar)
            proceso = EditorCoroutineUtility.StartCoroutineOwnerless(GenerarTodos(pendientes, invoice.Trim()));
    }

    private static List<KeyValuePair<string, string>> Pendientes(out int caracteres)
    {
        caracteres = 0;
        string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Info útil", "deepvoice_textos.txt"));
        if (!File.Exists(ruta))
        {
            Debug.LogError("[Voces de Tano] No se encontro " + ruta);
            return null;
        }

        HashSet<string> requeridos = new HashSet<string>(CatalogoVoces.NombresRequeridos());
        List<KeyValuePair<string, string>> pendientes = new List<KeyValuePair<string, string>>();
        string[] lineas = File.ReadAllLines(ruta, Encoding.UTF8);

        for (int i = 0; i < lineas.Length; i++)
        {
            Match cabecera = Cabecera.Match(lineas[i]);
            if (!cabecera.Success)
                continue;

            string nombre = cabecera.Groups[1].Value;
            string texto = null;
            for (int j = i + 1; j < lineas.Length && texto == null; j++)
            {
                if (!string.IsNullOrWhiteSpace(lineas[j]))
                    texto = lineas[j].Trim();
            }

            if (string.IsNullOrEmpty(texto) || texto.StartsWith("---"))
            {
                Debug.LogWarning($"[Voces de Tano] '{nombre}' no tiene texto en el txt.");
                continue;
            }

            if (!requeridos.Contains(nombre))
                Debug.LogWarning($"[Voces de Tano] '{nombre}' esta en el txt pero la app no lo usa; se genera igual.");

            if (CatalogoVoces.CargarDefinitivo(nombre) != null)
                continue;

            pendientes.Add(new KeyValuePair<string, string>(nombre, texto));
            caracteres += texto.Length;
        }

        return pendientes;
    }

    private static IEnumerator GenerarTodos(List<KeyValuePair<string, string>> pendientes, string invoice)
    {
        int generados = 0;
        string motivoFin = null;

        for (int i = 0; i < pendientes.Count && motivoFin == null; i++)
        {
            string nombre = pendientes[i].Key;
            if (EditorUtility.DisplayCancelableProgressBar("Generando audios de Tano",
                    $"{i + 1}/{pendientes.Count}: {nombre}", (float)i / pendientes.Count))
            {
                motivoFin = "cancelado por el usuario";
                break;
            }

            string json = JsonUtility.ToJson(new Peticion
            {
                text = pendientes[i].Value,
                model = "instruct",
                invoice = invoice,
                name = Voz,
                instructions = Instrucciones,
            });

            using (UnityWebRequest peticion = new UnityWebRequest(Url, "POST"))
            {
                peticion.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                peticion.downloadHandler = new DownloadHandlerBuffer();
                peticion.SetRequestHeader("Content-Type", "application/json");
                peticion.timeout = 120;
                yield return peticion.SendWebRequest();

                string respuesta = peticion.downloadHandler.text;
                if (peticion.result != UnityWebRequest.Result.Success)
                    motivoFin = $"error de red en '{nombre}' ({peticion.responseCode}): {peticion.error}";
                else if (respuesta == "Invalid Response" || respuesta == "Invalid Invoice Number")
                    motivoFin = "el Invoice Number no es valido";
                else if (respuesta == "Limit Reached")
                    motivoFin = "se acabo la cuota mensual de DeepVoice";
                else
                {
                    byte[] audio;
                    try
                    {
                        audio = Convert.FromBase64String(respuesta);
                    }
                    catch (FormatException)
                    {
                        motivoFin = $"respuesta inesperada del servidor en '{nombre}': {respuesta.Substring(0, Math.Min(200, respuesta.Length))}";
                        break;
                    }

                    // DeepVoice devuelve MP3; se guarda con su extension real.
                    string destino = $"{Carpeta}/{nombre}.mp3";
                    File.WriteAllBytes(destino, audio);
                    AssetDatabase.ImportAsset(destino);
                    generados++;
                    Debug.Log($"[Voces de Tano] Generado {i + 1}/{pendientes.Count}: {nombre}");
                }
            }

            if (motivoFin == null && i < pendientes.Count - 1)
                yield return new EditorWaitForSeconds(PausaEntrePeticiones);
        }

        EditorUtility.ClearProgressBar();
        proceso = null;

        if (motivoFin == null)
            Debug.Log($"[Voces de Tano] Listo: se generaron {generados} audios.");
        else
            Debug.LogWarning($"[Voces de Tano] Se detuvo ({motivoFin}). Generados: {generados} de {pendientes.Count}. Vuelve a ejecutar el menu para continuar con los que faltan.");

        VerificarVocesTano.Verificar();
    }
}
