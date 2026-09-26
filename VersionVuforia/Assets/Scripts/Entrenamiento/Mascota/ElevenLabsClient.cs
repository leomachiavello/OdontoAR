using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class ElevenLabsClient
{
    private const string ModeloId = "eleven_multilingual_v2";

    [Serializable]
    public class Peticion
    {
        public string text;
        public string model_id;
    }

    public static IEnumerator ObtenerAudio(string texto, Action<AudioClip> alTerminar)
    {
        IEnumerator carga = ConfigApi.Cargar();
        while (carga.MoveNext())
            yield return carga.Current;

        DatosApi datos = ConfigApi.Datos;
        if (datos == null || string.IsNullOrEmpty(datos.elevenLabsApiKey) || string.IsNullOrEmpty(datos.elevenLabsVoiceId))
        {
            Debug.LogError("[ElevenLabs] Faltan elevenLabsApiKey o elevenLabsVoiceId en api_config.json");
            alTerminar(null);
            yield break;
        }

        string carpeta = Path.Combine(Application.persistentDataPath, "tts_cache");
        Directory.CreateDirectory(carpeta);
        string ruta = Path.Combine(carpeta, Hash(datos.elevenLabsVoiceId + "|" + ModeloId + "|" + texto) + ".mp3");

        if (!File.Exists(ruta))
        {
            string url = $"https://api.elevenlabs.io/v1/text-to-speech/{datos.elevenLabsVoiceId}?output_format=mp3_44100_128";
            string cuerpo = JsonUtility.ToJson(new Peticion { text = texto, model_id = ModeloId });

            using (UnityWebRequest peticion = new UnityWebRequest(url, "POST"))
            {
                peticion.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(cuerpo));
                peticion.downloadHandler = new DownloadHandlerBuffer();
                peticion.SetRequestHeader("xi-api-key", datos.elevenLabsApiKey);
                peticion.SetRequestHeader("Content-Type", "application/json");
                peticion.SetRequestHeader("Accept", "audio/mpeg");

                yield return peticion.SendWebRequest();

                if (peticion.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ElevenLabs] Error {peticion.responseCode}: {peticion.error} {peticion.downloadHandler.text}");
                    alTerminar(null);
                    yield break;
                }

                File.WriteAllBytes(ruta, peticion.downloadHandler.data);
            }
        }

        using (UnityWebRequest lectura = UnityWebRequestMultimedia.GetAudioClip(new Uri(ruta).AbsoluteUri, AudioType.MPEG))
        {
            yield return lectura.SendWebRequest();

            if (lectura.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ElevenLabs] No se pudo cargar el audio guardado: {lectura.error}");
                alTerminar(null);
                yield break;
            }

            alTerminar(DownloadHandlerAudioClip.GetContent(lectura));
        }
    }

    private static string Hash(string entrada)
    {
        using (SHA1 sha = SHA1.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(entrada));
            StringBuilder resultado = new StringBuilder();
            foreach (byte b in bytes)
                resultado.Append(b.ToString("x2"));
            return resultado.ToString();
        }
    }
}
