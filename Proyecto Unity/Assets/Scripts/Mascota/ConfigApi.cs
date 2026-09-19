using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class DatosApi
{
    public string witAiServerToken;
    public string groqApiKey;
    public string elevenLabsApiKey;
    public string elevenLabsVoiceId;
}

public static class ConfigApi
{
    public static DatosApi Datos { get; private set; }

    public static IEnumerator Cargar()
    {
        if (Datos != null)
            yield break;

        string ruta = Path.Combine(Application.streamingAssetsPath, "api_config.json");
        string json = null;

        if (ruta.Contains("://"))
        {
            using (UnityWebRequest peticion = UnityWebRequest.Get(ruta))
            {
                yield return peticion.SendWebRequest();

                if (peticion.result == UnityWebRequest.Result.Success)
                    json = peticion.downloadHandler.text;
                else
                    Debug.LogError($"[ConfigApi] No se pudo leer api_config.json: {peticion.error}");
            }
        }
        else if (File.Exists(ruta))
        {
            json = File.ReadAllText(ruta);
        }
        else
        {
            Debug.LogError($"[ConfigApi] No existe {ruta}");
        }

        if (!string.IsNullOrEmpty(json))
            Datos = JsonUtility.FromJson<DatosApi>(json);
    }
}
