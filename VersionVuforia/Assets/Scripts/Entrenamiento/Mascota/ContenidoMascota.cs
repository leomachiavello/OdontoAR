using System;
using System.Text;
using UnityEngine;

[Serializable]
public class PasoContenido
{
    public string panel;
    public int numero;
    public string titulo;
    public string explicacion;
    public string estado;
}

[Serializable]
public class ContenidoMascota
{
    public string persona;
    public string saludo;
    public string general;
    public PasoContenido[] pasos;

    private static ContenidoMascota instancia;
    private static readonly string[] numerosEnPalabras =
        { "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve", "diez" };

    public static ContenidoMascota Cargar()
    {
        if (instancia != null)
            return instancia;

        TextAsset archivo = Resources.Load<TextAsset>("mascota_contenido");
        if (archivo == null)
        {
            Debug.LogError("[ContenidoMascota] No se encontro Assets/Resources/mascota_contenido.json");
            return null;
        }

        instancia = JsonUtility.FromJson<ContenidoMascota>(archivo.text);
        return instancia;
    }

    public PasoContenido BuscarPaso(string panel)
    {
        if (pasos == null || string.IsNullOrEmpty(panel))
            return null;

        foreach (PasoContenido paso in pasos)
        {
            if (paso.panel == panel)
                return paso;
        }

        return null;
    }

    public string ConstruirGuion(PasoContenido paso)
    {
        if (paso == null || string.IsNullOrWhiteSpace(paso.explicacion))
            return null;

        StringBuilder guion = new StringBuilder();

        if (paso.numero > 0)
        {
            string numero = paso.numero < numerosEnPalabras.Length ? numerosEnPalabras[paso.numero] : paso.numero.ToString();
            guion.Append("Paso ").Append(numero).Append(". ");
        }

        if (!string.IsNullOrWhiteSpace(paso.titulo))
            guion.Append(paso.titulo.Trim().TrimEnd('.')).Append(". ");

        guion.Append(paso.explicacion.Trim());
        return guion.ToString();
    }
}
