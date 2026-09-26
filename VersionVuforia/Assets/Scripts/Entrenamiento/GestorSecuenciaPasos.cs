using System;
using UnityEngine;

/// <summary>
/// Maneja secuencias lineales de pasos (Inlay, Onlay, Overlay, etc.)
/// - Back en el primer paso: no hace nada.
/// - Next en el último paso: vuelve al panel principal (maqueta).
///
/// Expone PasoCambiado y NombrePasoActual para que la mascota
/// sepa qué panel está viendo el alumno.
/// </summary>
public class GestorSecuenciaPasos : MonoBehaviour
{
    [Serializable]
    public class Secuencia
    {
        public string nombre; // solo para identificar en el Inspector (Inlay, Onlay, Overlay)
        public GameObject[] pasos; // pasos en orden: [Paso1, Paso2, ..., PasoN]
    }

    [Header("Panel principal (la maqueta con Inlay/Onlay/Overlay Explorar)")]
    [SerializeField] private GameObject panelPrincipal;

    [Header("Secuencias disponibles")]
    [SerializeField] private Secuencia[] secuencias;

    /// <summary>Se dispara cada vez que cambia el panel visible (paso nuevo o vuelta al principal).</summary>
    public event Action<int> PasoCambiado;

    public bool EnSecuencia => secuenciaActual != null;

    /// <summary>Nombre del GameObject del panel que se está viendo ahora.</summary>
    public string NombrePasoActual
    {
        get
        {
            if (secuenciaActual != null)
                return secuenciaActual.pasos[indiceActual].name;
            return panelPrincipal != null ? panelPrincipal.name : null;
        }
    }

    private Secuencia secuenciaActual;
    private int indiceActual;

    private void Start()
    {
        // Apaga todos los pasos al arrancar, por si quedó alguno prendido en el editor.
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
        if (indiceActual <= 0) return; // primer paso: no hay a dónde regresar

        secuenciaActual.pasos[indiceActual].SetActive(false);
        indiceActual--;
        MostrarPasoActual();
    }

    private void MostrarPasoActual()
    {
        secuenciaActual.pasos[indiceActual].SetActive(true);
        PasoCambiado?.Invoke(indiceActual);
    }

    private void VolverAlPrincipal()
    {
        secuenciaActual.pasos[indiceActual].SetActive(false);
        panelPrincipal.SetActive(true);
        secuenciaActual = null;
        indiceActual = 0;
        PasoCambiado?.Invoke(indiceActual);
    }

    /// <summary>
    /// Llamar desde un botón "Home" para volver al panel principal
    /// desde cualquier punto de cualquier secuencia.
    /// </summary>
    public void ForzarVueltaAlPrincipal()
    {
        if (secuenciaActual != null)
        {
            secuenciaActual.pasos[indiceActual].SetActive(false);
            secuenciaActual = null;
            indiceActual = 0;
            panelPrincipal.SetActive(true);
            PasoCambiado?.Invoke(indiceActual);
        }
        else
        {
            panelPrincipal.SetActive(true);
        }
    }

    /// <summary>
    /// Botón único para Menu_button:
    /// - En medio de una secuencia: vuelve a la maqueta.
    /// - Ya en la maqueta: sale a la escena "Menu".
    /// </summary>
    public void BotonMenuInteligente()
    {
        if (secuenciaActual != null)
            ForzarVueltaAlPrincipal();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}