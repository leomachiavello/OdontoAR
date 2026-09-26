using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public static class GrabadorVoz
{
    private const int FrecuenciaSolicitada = 16000;
    private const int SegundosMaximos = 20;

    private static AudioClip clip;

    public static bool Grabando { get; private set; }

    public static bool Iniciar()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            return false;
        }
#endif
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[GrabadorVoz] No hay microfono disponible.");
            return false;
        }

        clip = Microphone.Start(null, false, SegundosMaximos, FrecuenciaSolicitada);
        Grabando = clip != null;
        return Grabando;
    }

    public static byte[] Detener(out float segundos)
    {
        segundos = 0f;
        if (!Grabando)
            return null;

        Grabando = false;
        int muestras = Microphone.GetPosition(null);
        Microphone.End(null);

        if (clip == null || muestras <= 0)
            return null;

        float[] datos = new float[muestras * clip.channels];
        clip.GetData(datos, 0);
        segundos = (float)muestras / clip.frequency;

        byte[] wav = ConvertirAWav(datos, clip.channels, clip.frequency);
        Object.Destroy(clip);
        clip = null;
        return wav;
    }

    private static byte[] ConvertirAWav(float[] muestras, int canales, int frecuencia)
    {
        int bytesDatos = muestras.Length * 2;

        using (MemoryStream memoria = new MemoryStream(44 + bytesDatos))
        using (BinaryWriter escritor = new BinaryWriter(memoria))
        {
            escritor.Write(Encoding.ASCII.GetBytes("RIFF"));
            escritor.Write(36 + bytesDatos);
            escritor.Write(Encoding.ASCII.GetBytes("WAVE"));
            escritor.Write(Encoding.ASCII.GetBytes("fmt "));
            escritor.Write(16);
            escritor.Write((short)1);
            escritor.Write((short)canales);
            escritor.Write(frecuencia);
            escritor.Write(frecuencia * canales * 2);
            escritor.Write((short)(canales * 2));
            escritor.Write((short)16);
            escritor.Write(Encoding.ASCII.GetBytes("data"));
            escritor.Write(bytesDatos);

            foreach (float muestra in muestras)
                escritor.Write((short)(Mathf.Clamp(muestra, -1f, 1f) * short.MaxValue));

            return memoria.ToArray();
        }
    }
}
