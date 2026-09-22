using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public static class GroqClient
{
    private const string UrlBase = "https://api.groq.com/openai/v1/";
    private const string ModeloVoz = "whisper-large-v3-turbo";
    private const string ModeloChat = "openai/gpt-oss-120b";
    private const string Vocabulario =
        "Explícame el proceso de la preparación onlay. También la inlay y la overlay, la caja oclusal, la caja proximal, las cúspides, el punto de contacto, el esmalte, los socavados y la matriz.";

    [Serializable]
    public class Mensaje
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class RespuestaVoz
    {
        public string text;
    }

    [Serializable]
    public class PeticionChat
    {
        public string model;
        public Mensaje[] messages;
        public float temperature;
        public int max_completion_tokens;
        public string reasoning_effort;
    }

    [Serializable]
    public class Eleccion
    {
        public Mensaje message;
    }

    [Serializable]
    public class RespuestaChat
    {
        public Eleccion[] choices;
    }

    public static IEnumerator Transcribir(byte[] wav, Action<string> alTerminar)
    {
        string clave = null;
        IEnumerator carga = ObtenerClave(c => clave = c);
        while (carga.MoveNext())
            yield return carga.Current;

        if (clave == null)
        {
            alTerminar(null);
            yield break;
        }

        WWWForm formulario = new WWWForm();
        formulario.AddBinaryData("file", wav, "voz.wav", "audio/wav");
        formulario.AddField("model", ModeloVoz);
        formulario.AddField("language", "es");
        formulario.AddField("temperature", "0");
        formulario.AddField("response_format", "json");
        formulario.AddField("prompt", Vocabulario);

        string respuesta = null;
        IEnumerator envio = Enviar(() =>
        {
            UnityWebRequest peticion = UnityWebRequest.Post(UrlBase + "audio/transcriptions", formulario);
            peticion.SetRequestHeader("Authorization", "Bearer " + clave);
            return peticion;
        }, r => respuesta = r);
        while (envio.MoveNext())
            yield return envio.Current;

        string texto = respuesta == null ? null : Limpiar(JsonUtility.FromJson<RespuestaVoz>(respuesta).text);
        alTerminar(texto == null ? null : CorregirTerminos(texto));
    }

    public static IEnumerator Preguntar(List<Mensaje> conversacion, Action<string> alTerminar)
    {
        string clave = null;
        IEnumerator carga = ObtenerClave(c => clave = c);
        while (carga.MoveNext())
            yield return carga.Current;

        if (clave == null)
        {
            alTerminar(null);
            yield break;
        }

        PeticionChat cuerpo = new PeticionChat
        {
            model = ModeloChat,
            messages = conversacion.ToArray(),
            temperature = 0.3f,
            max_completion_tokens = 400,
            reasoning_effort = "low"
        };
        byte[] datos = Encoding.UTF8.GetBytes(JsonUtility.ToJson(cuerpo));

        string respuesta = null;
        IEnumerator envio = Enviar(() =>
        {
            UnityWebRequest peticion = new UnityWebRequest(UrlBase + "chat/completions", "POST");
            peticion.uploadHandler = new UploadHandlerRaw(datos);
            peticion.downloadHandler = new DownloadHandlerBuffer();
            peticion.SetRequestHeader("Content-Type", "application/json");
            peticion.SetRequestHeader("Authorization", "Bearer " + clave);
            return peticion;
        }, r => respuesta = r);
        while (envio.MoveNext())
            yield return envio.Current;

        if (respuesta == null)
        {
            alTerminar(null);
            yield break;
        }

        RespuestaChat resultado = JsonUtility.FromJson<RespuestaChat>(respuesta);
        bool hayTexto = resultado != null && resultado.choices != null && resultado.choices.Length > 0 && resultado.choices[0].message != null;
        alTerminar(hayTexto ? Limpiar(resultado.choices[0].message.content) : null);
    }

    private static IEnumerator ObtenerClave(Action<string> alTerminar)
    {
        IEnumerator carga = ConfigApi.Cargar();
        while (carga.MoveNext())
            yield return carga.Current;

        DatosApi datos = ConfigApi.Datos;
        if (datos == null || string.IsNullOrEmpty(datos.groqApiKey))
        {
            Debug.LogError("[Groq] Falta groqApiKey en api_config.json");
            alTerminar(null);
            yield break;
        }

        alTerminar(datos.groqApiKey);
    }

    private static IEnumerator Enviar(Func<UnityWebRequest> crearPeticion, Action<string> alTerminar)
    {
        for (int intento = 0; intento < 2; intento++)
        {
            using (UnityWebRequest peticion = crearPeticion())
            {
                peticion.timeout = 20;
                yield return peticion.SendWebRequest();

                if (peticion.result == UnityWebRequest.Result.Success)
                {
                    alTerminar(peticion.downloadHandler.text);
                    yield break;
                }

                Debug.LogWarning($"[Groq] Intento {intento + 1} fallo ({peticion.responseCode}): {peticion.error} {peticion.downloadHandler.text}");

                bool errorDelCliente = peticion.responseCode >= 400 && peticion.responseCode < 500 && peticion.responseCode != 429;
                if (errorDelCliente)
                    break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        alTerminar(null);
    }

    private static string CorregirTerminos(string texto)
    {
        texto = Regex.Replace(texto, @"\bon[\s-]?l(?:ay|ai|ei|ey|ine)\b", "onlay", RegexOptions.IgnoreCase);
        texto = Regex.Replace(texto, @"\bin[\s-]?l(?:ay|ai|ei|ey|ine)\b", "inlay", RegexOptions.IgnoreCase);
        texto = Regex.Replace(texto, @"\bover[\s-]?l(?:ay|ai|ei|ey)\b", "overlay", RegexOptions.IgnoreCase);
        return texto;
    }

    private static string Limpiar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return null;

        texto = texto.Replace("*", "").Replace("#", "").Replace("`", "");
        texto = Regex.Replace(texto, @"\s+", " ").Trim();
        return texto.Length > 0 ? texto : null;
    }
}
