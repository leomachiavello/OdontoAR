using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class ValorEntidad
{
    public string valor;
    public string[] sinonimos;
}

[Serializable]
public class DefinicionEntidad
{
    public string nombre;
    public string plantilla;
    public ValorEntidad[] valores;
}

[Serializable]
public class DefinicionIntencion
{
    public string nombre;
    public string accion;
    public string clip;
    public string entidad;
    public string prefijo;
    public string[] frases;
}

[Serializable]
public class DatosIntenciones
{
    public float umbral;
    public float margen;
    public float confianzaAlta;
    public DefinicionEntidad[] entidades;
    public DefinicionIntencion[] intenciones;
}

public class ResultadoIntencion
{
    public DefinicionIntencion intencion;
    public float puntaje;
    public Dictionary<string, List<string>> entidades = new Dictionary<string, List<string>>();

    public string ValorDe(string entidad)
    {
        return !string.IsNullOrEmpty(entidad) && entidades.TryGetValue(entidad, out List<string> valores) && valores.Count > 0
            ? valores[0]
            : null;
    }

    public string Clip
    {
        get
        {
            if (intencion == null)
                return null;

            if (!string.IsNullOrEmpty(intencion.clip))
                return intencion.clip;

            string valor = ValorDe(intencion.entidad);
            return valor != null && !string.IsNullOrEmpty(intencion.prefijo) ? intencion.prefijo + valor : null;
        }
    }
}

public class IntencionesLocal
{
    private class Sinonimo
    {
        public string entidad;
        public string plantilla;
        public string texto;
        public string valor;
    }

    private class Fila
    {
        public DefinicionIntencion intencion;
        public Dictionary<string, float> vector;
    }

    // Las palabras inglesas se pronuncian "onlei"/"inlei"/"overlei" y el reconocimiento de voz las escribe de mil formas.
    private static readonly string Fin = "(?:lay|lai|lei|ley|lain|lein|line|li|le)";
    private static readonly (Regex patron, string termino)[] CorreccionesTerminos =
    {
        (new Regex(@"\b(?:h?on ?" + Fin + @"|(?:un|an)(?:lay|lai|lei|ley|lain|lein))\b"), "onlay"),
        (new Regex(@"\b(?:h?in ?" + Fin + @"|enl(?:ay|ai|ei|ey))\b"), "inlay"),
        (new Regex(@"\b(?:o[uv]|ob)[ae]?r ?(?:lay|lai|lei|ley|lain|lein|le)\b"), "overlay"),
    };
    private static readonly string[] TerminosFonetica = { "onlay", "inlay", "overlay" };

    private static DatosIntenciones datosCargados;
    private static IntencionesLocal instancia;

    private readonly DatosIntenciones datos;
    private readonly HashSet<string> conocidas = new HashSet<string>();
    private readonly List<Sinonimo> sinonimos = new List<Sinonimo>();
    private readonly List<Fila> filas = new List<Fila>();
    private readonly Dictionary<string, float> idf = new Dictionary<string, float>();
    private float idfMaximo = 1f;

    public float ConfianzaAlta => datos.confianzaAlta;

    public static DatosIntenciones CargarDatos()
    {
        if (datosCargados != null)
            return datosCargados;

        TextAsset archivo = Resources.Load<TextAsset>("mascota_intenciones");
        if (archivo == null)
        {
            Debug.LogError("[Intenciones] No se encontro Assets/Resources/mascota_intenciones.json");
            return null;
        }

        datosCargados = JsonUtility.FromJson<DatosIntenciones>(archivo.text);
        return datosCargados;
    }

    public static IntencionesLocal Crear()
    {
        if (instancia != null)
            return instancia;

        DatosIntenciones datos = CargarDatos();
        if (datos == null)
            return null;

        instancia = new IntencionesLocal(datos);
        return instancia;
    }

    private IntencionesLocal(DatosIntenciones datosIntenciones)
    {
        datos = datosIntenciones;

        foreach (DefinicionEntidad entidad in datos.entidades)
        {
            foreach (ValorEntidad valor in entidad.valores)
            {
                HashSet<string> vistos = new HashSet<string>();
                foreach (string sinonimo in valor.sinonimos)
                {
                    string texto = Normalizar(sinonimo);
                    if (texto.Length > 0 && vistos.Add(texto))
                        sinonimos.Add(new Sinonimo { entidad = entidad.nombre, plantilla = entidad.plantilla, texto = texto, valor = valor.valor });
                }
            }
        }

        sinonimos.Sort((a, b) => b.texto.Length.CompareTo(a.texto.Length));

        List<KeyValuePair<DefinicionIntencion, List<string>>> documentos = new List<KeyValuePair<DefinicionIntencion, List<string>>>();
        Dictionary<string, int> frecuencia = new Dictionary<string, int>();

        foreach (DefinicionIntencion intencion in datos.intenciones)
        {
            foreach (string frase in intencion.frases)
            {
                string normalizada = CorregirTerminos(Normalizar(frase), null);
                foreach (string palabra in normalizada.Split(' '))
                    conocidas.Add(palabra);

                string sustituida = SustituirEntidades(normalizada, null);
                List<string> caracteristicas = Caracteristicas(sustituida);
                documentos.Add(new KeyValuePair<DefinicionIntencion, List<string>>(intencion, caracteristicas));

                foreach (string c in new HashSet<string>(caracteristicas))
                    frecuencia[c] = frecuencia.TryGetValue(c, out int n) ? n + 1 : 1;
            }
        }

        int total = documentos.Count;
        foreach (KeyValuePair<string, int> par in frecuencia)
        {
            float valorIdf = Mathf.Log((1f + total) / (1f + par.Value)) + 1f;
            idf[par.Key] = valorIdf;
            idfMaximo = Mathf.Max(idfMaximo, valorIdf);
        }

        foreach (KeyValuePair<DefinicionIntencion, List<string>> documento in documentos)
            filas.Add(new Fila { intencion = documento.Key, vector = Vectorizar(documento.Value) });
    }

    public ResultadoIntencion Clasificar(string texto)
    {
        ResultadoIntencion resultado = new ResultadoIntencion();
        string sustituida = SustituirEntidades(CorregirTerminos(Normalizar(texto), conocidas), resultado.entidades);
        Dictionary<string, float> consulta = Vectorizar(Caracteristicas(sustituida));

        Dictionary<DefinicionIntencion, float> mejores = new Dictionary<DefinicionIntencion, float>();
        foreach (Fila fila in filas)
        {
            float similitud = 0f;
            foreach (KeyValuePair<string, float> par in consulta)
            {
                if (fila.vector.TryGetValue(par.Key, out float peso))
                    similitud += par.Value * peso;
            }

            if (!mejores.TryGetValue(fila.intencion, out float actual) || similitud > actual)
                mejores[fila.intencion] = similitud;
        }

        List<KeyValuePair<DefinicionIntencion, float>> orden = new List<KeyValuePair<DefinicionIntencion, float>>();
        foreach (KeyValuePair<DefinicionIntencion, float> par in mejores)
        {
            bool falta = !string.IsNullOrEmpty(par.Key.entidad) && resultado.ValorDe(par.Key.entidad) == null;
            orden.Add(new KeyValuePair<DefinicionIntencion, float>(par.Key, falta ? 0f : par.Value));
        }

        orden.Sort((a, b) => b.Value.CompareTo(a.Value));

        if (orden.Count == 0)
            return resultado;

        resultado.puntaje = orden[0].Value;

        if (orden[0].Value < datos.confianzaAlta)
        {
            SoloTermino(resultado, consulta);
            if (resultado.intencion != null)
                return resultado;
        }

        if (orden[0].Value < datos.umbral)
            return resultado;

        bool ambiguo = orden.Count > 1
            && orden[0].Value - orden[1].Value < datos.margen
            && orden[0].Value < datos.confianzaAlta
            && orden[0].Key.nombre != "fuera_de_alcance";
        if (ambiguo)
            return resultado;

        resultado.intencion = orden[0].Key;
        return resultado;
    }

    // Si el alumno solo dijo el termino (o el reconocimiento se comio el resto de la frase), se asume que pide su definicion.
    private void SoloTermino(ResultadoIntencion resultado, Dictionary<string, float> consulta)
    {
        if (resultado.entidades.Count != 1)
            return;

        string entidad = null;
        List<string> valores = null;
        foreach (KeyValuePair<string, List<string>> par in resultado.entidades)
        {
            entidad = par.Key;
            valores = par.Value;
        }

        string plantilla = null;
        foreach (DefinicionEntidad definicion in datos.entidades)
        {
            if (definicion.nombre == entidad)
                plantilla = definicion.plantilla;
        }

        int palabras = 0;
        foreach (string clave in consulta.Keys)
        {
            if (clave.IndexOf('_') < 0)
                palabras++;
        }

        if (plantilla == null || valores.Count != 1 || palabras != 1 || !consulta.ContainsKey(plantilla))
            return;

        string nombre = entidad == "tipo_incrustacion" ? "definicion_tipo" : entidad == "tema_preparacion" ? "explicar_tema" : null;
        foreach (DefinicionIntencion intencion in datos.intenciones)
        {
            if (intencion.nombre == nombre)
            {
                resultado.intencion = intencion;
                resultado.puntaje = Mathf.Max(resultado.puntaje, datos.umbral + 0.05f);
                return;
            }
        }
    }

    private static string CorregirTerminos(string texto, HashSet<string> conocidas)
    {
        foreach ((Regex patron, string termino) correccion in CorreccionesTerminos)
            texto = correccion.patron.Replace(texto, correccion.termino);

        if (conocidas == null)
            return texto;

        string[] palabras = texto.Split(' ');
        for (int i = 0; i < palabras.Length; i++)
        {
            if (palabras[i].Length < 4 || conocidas.Contains(palabras[i]))
                continue;

            string clave = ClaveFonetica(palabras[i]);
            string elegido = null;
            int mejor = int.MaxValue;
            bool empate = false;

            foreach (string termino in TerminosFonetica)
            {
                int distancia = Distancia(clave, termino);
                if (distancia > (termino.Length >= 7 ? 2 : 1))
                    continue;

                if (distancia < mejor)
                {
                    elegido = termino;
                    mejor = distancia;
                    empate = false;
                }
                else if (distancia == mejor)
                {
                    empate = true;
                }
            }

            if (elegido != null && !empate)
                palabras[i] = elegido;
        }

        return string.Join(" ", palabras);
    }

    private static string ClaveFonetica(string palabra)
    {
        if (palabra.StartsWith("h", StringComparison.Ordinal))
            palabra = palabra.Substring(1);

        return palabra.Replace("ei", "ay").Replace("ey", "ay").Replace("ai", "ay");
    }

    private static int Distancia(string a, string b)
    {
        int[] anterior = new int[b.Length + 1];
        int[] actual = new int[b.Length + 1];

        for (int j = 0; j <= b.Length; j++)
            anterior[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            actual[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int costo = a[i - 1] == b[j - 1] ? 0 : 1;
                actual[j] = Math.Min(Math.Min(actual[j - 1] + 1, anterior[j] + 1), anterior[j - 1] + costo);
            }

            int[] intercambio = anterior;
            anterior = actual;
            actual = intercambio;
        }

        return anterior[b.Length];
    }

    public static string Normalizar(string texto)
    {
        StringBuilder sb = new StringBuilder(texto.Length);
        foreach (char original in texto.ToLowerInvariant())
        {
            char c = QuitarAcento(original);
            sb.Append((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ? c : ' ');
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private static char QuitarAcento(char c)
    {
        switch (c)
        {
            case 'á': case 'à': case 'â': case 'ä': return 'a';
            case 'é': case 'è': case 'ê': case 'ë': return 'e';
            case 'í': case 'ì': case 'î': case 'ï': return 'i';
            case 'ó': case 'ò': case 'ô': case 'ö': return 'o';
            case 'ú': case 'ù': case 'û': case 'ü': return 'u';
            case 'ñ': return 'n';
            case 'ç': return 'c';
            default: return c;
        }
    }

    private string SustituirEntidades(string textoNormalizado, Dictionary<string, List<string>> encontradas)
    {
        string t = " " + textoNormalizado + " ";

        foreach (Sinonimo sinonimo in sinonimos)
        {
            string patron = " " + sinonimo.texto + " ";
            int indice;
            while ((indice = t.IndexOf(patron, StringComparison.Ordinal)) >= 0)
            {
                t = t.Substring(0, indice) + " " + sinonimo.plantilla + " " + t.Substring(indice + patron.Length);

                if (encontradas != null)
                {
                    if (!encontradas.TryGetValue(sinonimo.entidad, out List<string> valores))
                    {
                        valores = new List<string>();
                        encontradas[sinonimo.entidad] = valores;
                    }
                    valores.Add(sinonimo.valor);
                }
            }
        }

        return Regex.Replace(t, @"\s+", " ").Trim();
    }

    private static List<string> Caracteristicas(string textoSustituido)
    {
        string[] palabras = textoSustituido.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> raices = new List<string>(palabras.Length);
        foreach (string palabra in palabras)
            raices.Add(palabra.Length > 5 && !palabra.EndsWith("ent", StringComparison.Ordinal) ? palabra.Substring(0, 5) : palabra);

        List<string> resultado = new List<string>(raices);
        for (int i = 0; i < raices.Count - 1; i++)
            resultado.Add(raices[i] + "_" + raices[i + 1]);

        return resultado;
    }

    private Dictionary<string, float> Vectorizar(List<string> caracteristicas)
    {
        Dictionary<string, float> vector = new Dictionary<string, float>();
        float extra = 0f;

        foreach (string caracteristica in caracteristicas)
        {
            if (idf.TryGetValue(caracteristica, out float peso))
                vector[caracteristica] = (vector.TryGetValue(caracteristica, out float acumulado) ? acumulado : 0f) + peso;
            else if (caracteristica.IndexOf('_') < 0)
                extra += idfMaximo * idfMaximo;
        }

        float suma = extra;
        foreach (float v in vector.Values)
            suma += v * v;

        float norma = Mathf.Sqrt(suma);
        if (norma <= 0f)
            norma = 1f;

        List<string> claves = new List<string>(vector.Keys);
        foreach (string clave in claves)
            vector[clave] = vector[clave] / norma;

        return vector;
    }
}
