using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class WitClient
{
    private const string UrlVoz = "https://api.wit.ai/speech?v=20240304";

    [Serializable]
    public class MensajeWit
    {
        public string type;
        public string text;
        public bool is_final;
    }

    public static IEnumerator Transcribir(byte[] wav, Action<string> alTerminar)
    {
        IEnumerator carga = ConfigApi.Cargar();
        while (carga.MoveNext())
            yield return carga.Current;

        DatosApi datos = ConfigApi.Datos;
        if (datos == null || string.IsNullOrEmpty(datos.witAiServerToken))
        {
            Debug.LogError("[Wit] Falta witAiServerToken en api_config.json");
            alTerminar(null);
            yield break;
        }

        string respuesta = null;
        IEnumerator envio = Enviar(() =>
        {
            UnityWebRequest peticion = new UnityWebRequest(UrlVoz, "POST");
            peticion.uploadHandler = new UploadHandlerRaw(wav);
            peticion.downloadHandler = new DownloadHandlerBuffer();
            peticion.SetRequestHeader("Authorization", "Bearer " + datos.witAiServerToken);
            peticion.SetRequestHeader("Content-Type", "audio/wav");
            return peticion;
        }, r => respuesta = r);
        while (envio.MoveNext())
            yield return envio.Current;

        alTerminar(respuesta == null ? null : ExtraerTexto(respuesta));
    }

    public static string ExtraerTexto(string cuerpo)
    {
        string parcial = null;
        string final = null;

        foreach (string objeto in SepararObjetos(cuerpo))
        {
            MensajeWit mensaje = JsonUtility.FromJson<MensajeWit>(objeto);
            if (mensaje == null || string.IsNullOrEmpty(mensaje.text))
                continue;

            if (mensaje.type == "FINAL_TRANSCRIPTION" || mensaje.is_final)
                final = mensaje.text;
            else
                parcial = mensaje.text;
        }

        string texto = string.IsNullOrWhiteSpace(final) ? parcial : final;
        return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }

    public static List<string> SepararObjetos(string cuerpo)
    {
        List<string> objetos = new List<string>();
        int profundidad = 0;
        int inicio = -1;
        bool enTexto = false;
        bool escape = false;

        for (int i = 0; i < cuerpo.Length; i++)
        {
            char c = cuerpo[i];

            if (enTexto)
            {
                if (escape)
                    escape = false;
                else if (c == '\\')
                    escape = true;
                else if (c == '"')
                    enTexto = false;
                continue;
            }

            if (c == '"')
            {
                enTexto = true;
            }
            else if (c == '{')
            {
                if (profundidad == 0)
                    inicio = i;
                profundidad++;
            }
            else if (c == '}')
            {
                profundidad--;
                if (profundidad == 0 && inicio >= 0)
                {
                    objetos.Add(cuerpo.Substring(inicio, i - inicio + 1));
                    inicio = -1;
                }
            }
        }

        return objetos;
    }

    private static IEnumerator Enviar(Func<UnityWebRequest> crearPeticion, Action<string> alTerminar)
    {
        for (int intento = 0; intento < 2; intento++)
        {
            using (UnityWebRequest peticion = crearPeticion())
            {
                peticion.timeout = 25;
                yield return peticion.SendWebRequest();

                if (peticion.result == UnityWebRequest.Result.Success)
                {
                    alTerminar(peticion.downloadHandler.text);
                    yield break;
                }

                Debug.LogWarning($"[Wit] Intento {intento + 1} fallo ({peticion.responseCode}): {peticion.error} {peticion.downloadHandler.text}");

                bool errorDelCliente = peticion.responseCode >= 400 && peticion.responseCode < 500 && peticion.responseCode != 429;
                if (errorDelCliente)
                    break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        alTerminar(null);
    }
}
