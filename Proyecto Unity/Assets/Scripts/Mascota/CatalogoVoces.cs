using System.Collections.Generic;
using UnityEngine;

public static class CatalogoVoces
{
    public const string Saludo = "saludo_inicial";
    public const string NoSeguro = "respuesta_no_seguro";
    public const string NoEscuche = "respuesta_no_escuche";
    public const string UltimoPaso = "respuesta_ultimo_paso";
    public const string EligeRuta = "respuesta_elige_ruta";

    private const string Carpeta = "Voces";
    private const string CarpetaProvisional = "VocesProvisionales";

    public static AudioClip CargarDefinitivo(string nombre)
    {
        return string.IsNullOrEmpty(nombre) ? null : Resources.Load<AudioClip>(Carpeta + "/" + nombre);
    }

    public static AudioClip Cargar(string nombre)
    {
        AudioClip clip = CargarDefinitivo(nombre);
        if (clip != null || string.IsNullOrEmpty(nombre))
            return clip;

        return Resources.Load<AudioClip>(CarpetaProvisional + "/" + nombre);
    }

    public static List<string> NombresRequeridos()
    {
        List<string> nombres = new List<string> { Saludo, NoSeguro, NoEscuche, UltimoPaso, EligeRuta };

        ContenidoMascota contenido = ContenidoMascota.Cargar();
        if (contenido != null && contenido.pasos != null)
        {
            foreach (PasoContenido paso in contenido.pasos)
            {
                if (!string.IsNullOrWhiteSpace(paso.explicacion))
                    Agregar(nombres, paso.panel);
            }
        }

        DatosIntenciones datos = IntencionesLocal.CargarDatos();
        if (datos != null)
        {
            foreach (DefinicionIntencion intencion in datos.intenciones)
            {
                if (!string.IsNullOrEmpty(intencion.clip))
                {
                    Agregar(nombres, intencion.clip);
                }
                else if (!string.IsNullOrEmpty(intencion.prefijo))
                {
                    foreach (DefinicionEntidad entidad in datos.entidades)
                    {
                        if (entidad.nombre != intencion.entidad)
                            continue;

                        foreach (ValorEntidad valor in entidad.valores)
                            Agregar(nombres, intencion.prefijo + valor.valor);
                    }
                }
            }
        }

        return nombres;
    }

    private static void Agregar(List<string> lista, string nombre)
    {
        if (!lista.Contains(nombre))
            lista.Add(nombre);
    }
}
