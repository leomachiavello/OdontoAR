using UnityEngine;

/// <summary>
/// Maneja secuencias lineales de pasos (Inlay, Onlay, Overlay, etc.)
/// - Back en el primer paso: no hace nada.
/// - Next en el último paso: vuelve al panel principal (maqueta).
/// 
/// Un solo componente maneja las 3 secuencias (o las que agregues),
/// para no repetir lógica ni tener 3 scripts distintos.
/// </summary>
public class GestorSecuenciaPasos : MonoBehaviour
{
    [System.Serializable]
    public class Secuencia
    {
        public string nombre; // solo para identificar en el Inspector (Inlay, Onlay, Overlay)
        public GameObject[] pasos; // pasos en orden: [Paso1, Paso2, ..., PasoN]
    }

    [Header("Panel principal (la maqueta con Inlay/Onlay/Overlay Explorar)")]
    [SerializeField] private GameObject panelPrincipal;

    [Header("Secuencias disponibles")]
    [SerializeField] private Secuencia[] secuencias;

    private Secuencia secuenciaActual;
    private int indiceActual;

    private void Start()
    {
        // Apaga todos los pasos de todas las secuencias al arrancar,
        // por si en el editor quedó alguno prendido de pruebas anteriores.
        foreach (var secuencia in secuencias)
        {
            foreach (var paso in secuencia.pasos)
            {
                if (paso != null)
                    paso.SetActive(false);
            }
        }

        panelPrincipal.SetActive(true);
    }

    /// <summary>
    /// Llamar desde el botón "Explorar" de cada opción (Inlay/Onlay/Overlay).
    /// indiceSecuencia = posición del array 'secuencias' (0 = Inlay, 1 = Onlay, 2 = Overlay, etc).
    /// </summary>
    public void IniciarSecuencia(int indiceSecuencia)
    {
        if (indiceSecuencia < 0 || indiceSecuencia >= secuencias.Length)
        {
            Debug.LogWarning($"Índice de secuencia inválido: {indiceSecuencia}");
            return;
        }

        secuenciaActual = secuencias[indiceSecuencia];
        indiceActual = 0;

        panelPrincipal.SetActive(false);
        MostrarPasoActual();
    }

    /// <summary>Llamar desde el botón Next / Siguiente.</summary>
    public void Siguiente()
    {
        if (secuenciaActual == null) return;

        bool esUltimoPaso = indiceActual >= secuenciaActual.pasos.Length - 1;

        if (esUltimoPaso)
        {
            VolverAlPrincipal();
            return;
        }

        secuenciaActual.pasos[indiceActual].SetActive(false);
        indiceActual++;
        MostrarPasoActual();
    }

    /// <summary>Llamar desde el botón Back / Atrás.</summary>
    public void Atras()
    {
        if (secuenciaActual == null) return;

        bool esPrimerPaso = indiceActual <= 0;

        if (esPrimerPaso)
        {
            // No hay a dónde regresar, se queda en el mismo panel.
            return;
        }

        secuenciaActual.pasos[indiceActual].SetActive(false);
        indiceActual--;
        MostrarPasoActual();
    }

    private void MostrarPasoActual()
    {
        secuenciaActual.pasos[indiceActual].SetActive(true);
    }

    private void VolverAlPrincipal()
    {
        secuenciaActual.pasos[indiceActual].SetActive(false);
        panelPrincipal.SetActive(true);
        secuenciaActual = null;
        indiceActual = 0;
    }

    /// <summary>
    /// Llamar desde el Menu_button (u otro botón "Home") para forzar
    /// la vuelta al panel principal desde cualquier punto de cualquier secuencia.
    /// A diferencia de Siguiente(), esto no exige estar en el último paso.
    /// </summary>
    public void ForzarVueltaAlPrincipal()
    {
        if (secuenciaActual != null)
        {
            secuenciaActual.pasos[indiceActual].SetActive(false);
            secuenciaActual = null;
            indiceActual = 0;
        }

        panelPrincipal.SetActive(true);
    }

    /// <summary>
    /// Botón único e inteligente para Menu_button:
    /// - Si estás en medio de una secuencia (Inlay/Onlay/Overlay), vuelve a la maqueta.
    /// - Si ya estás en la maqueta (Panel_Principal), sale a la escena de Menú.
    /// Reemplaza los dos botones separados por uno solo con esta lógica.
    /// </summary>
    public void BotonMenuInteligente()
    {
        if (secuenciaActual != null)
        {
            ForzarVueltaAlPrincipal();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
    }
}