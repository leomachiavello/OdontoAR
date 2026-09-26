using UnityEngine;

/// <summary>
/// Reemplaza al flujo de spawn de AR Foundation.
/// Ponlo en el Image Target (o en cualquier objeto activo de la escena) y conecta
/// AlEncontrarImagen / AlPerderImagen a los eventos del DefaultObserverEventHandler.
/// </summary>
public class ControladorMascota : MonoBehaviour
{
    [Tooltip("Raíz de la mascota: el objeto que contiene el esqueleto (Bone_0xx)")]
    [SerializeField] private Transform mascota;
    [SerializeField] private GestorSecuenciaPasos gestor;

    private NarradorMascota narrador;
    private ConversacionMascota conversacion;
    private bool configurado;
    private bool narracionIniciada;

#if UNITY_ANDROID && !UNITY_EDITOR
    // Pide el micrófono al arrancar para que el primer toque a la mascota ya pueda grabar.
    private void Start()
    {
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
    }
#endif

    /// <summary>Conectar a "On Target Found" del DefaultObserverEventHandler.</summary>
    public void AlEncontrarImagen()
    {
        if (!configurado)
        {
            narrador = gameObject.AddComponent<NarradorMascota>();
            conversacion = gameObject.AddComponent<ConversacionMascota>();

            narrador.Configurar(mascota, gestor);
            conversacion.Configurar(mascota, narrador, gestor);
            configurado = true;
        }

        conversacion.Activar();

        if (!narracionIniciada)
        {
            narracionIniciada = true;
            narrador.IniciarNarracion();
        }
    }

    /// <summary>Conectar a "On Target Lost".</summary>
    public void AlPerderImagen()
    {
        if (narrador != null)
            narrador.Detener();
    }
}