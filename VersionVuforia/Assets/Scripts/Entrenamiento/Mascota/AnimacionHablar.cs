using System.Collections.Generic;
using UnityEngine;

public class AnimacionHablar : MonoBehaviour
{
    [System.Serializable]
    public class ConfigBrazo
    {
        public string hombro;
        public string brazo;
        public string antebrazo;
        public string punta;
    }

    [Header("Cuello y cabeza al hablar")]
    [SerializeField] private string huesoCuelloBase = "Bone_028";
    [SerializeField] private string huesoCuelloMedio = "Bone_027";
    [SerializeField] private string huesoCabeza = "Bone_026";
    [SerializeField] private float anguloCuello = 4f;
    [SerializeField] private float anguloCabeza = 10f;
    [SerializeField] private float anguloBalanceo = 3f;
    [SerializeField] private float anguloInclinacionEscucha = 12f;

    [Header("Brazos")]
    [SerializeField] private ConfigBrazo brazoA = new ConfigBrazo { hombro = "Bone_020", brazo = "Bone_019", antebrazo = "Bone_018", punta = "Bone_016" };
    [SerializeField] private ConfigBrazo brazoB = new ConfigBrazo { hombro = "Bone_025", brazo = "Bone_024", antebrazo = "Bone_023", punta = "Bone_021" };
    [SerializeField] private float anguloBrazoAlHablar = 12f;
    [SerializeField] private float anguloBrazoSaludo = 70f;
    [SerializeField] private float anguloOndaSaludo = 25f;
    [SerializeField] private float duracionSaludo = 2.5f;
    [SerializeField] private float frecuenciaOnda = 3f;

    [Header("Respuesta al volumen")]
    [SerializeField] private float sensibilidad = 6f;
    [SerializeField] private float suavizado = 12f;

    private class HuesoAnimado
    {
        public Transform hueso;
        public Quaternion rotacionBase;
        public float angulo;
        public float balanceo;
        public float escucha;
    }

    private class BrazoAnimado
    {
        public Transform brazo;
        public Transform antebrazo;
        public Transform punta;
        public Quaternion brazoBase;
        public Quaternion antebrazoBase;
        public float signoElevacion;
        public float fase;
    }

    private readonly List<HuesoAnimado> huesos = new List<HuesoAnimado>();
    private readonly float[] muestras = new float[256];
    private BrazoAnimado brazoAnimadoA;
    private BrazoAnimado brazoAnimadoB;
    private BrazoAnimado brazoSaludo;
    private AudioSource fuente;
    private float nivel;
    private float tiempoSaludo = -1f;
    private float pesoEscucha;
    private bool escuchando;

    public void Configurar(AudioSource audio)
    {
        fuente = audio;
        huesos.Clear();

        AgregarHueso(huesoCuelloBase, anguloCuello, 0f, 0.2f);
        AgregarHueso(huesoCuelloMedio, anguloCuello, 0f, 0.3f);
        AgregarHueso(huesoCabeza, anguloCabeza, anguloBalanceo, 1f);

        brazoAnimadoA = CrearBrazo(brazoA, 0f);
        brazoAnimadoB = CrearBrazo(brazoB, 1.7f);

        if (huesos.Count == 0)
            Debug.LogWarning("[AnimacionHablar] No se encontraron los huesos del cuello/cabeza.");
        if (brazoAnimadoA == null && brazoAnimadoB == null)
            Debug.LogWarning("[AnimacionHablar] No se encontraron los huesos de los brazos.");
    }

    public void Escuchando(bool activo)
    {
        escuchando = activo;
    }

    public void Saludar()
    {
        Transform camara = Camera.main != null ? Camera.main.transform : null;

        if (brazoAnimadoA != null && brazoAnimadoB != null && camara != null)
        {
            Vector3 diferencia = brazoAnimadoA.punta.position - brazoAnimadoB.punta.position;
            brazoSaludo = Vector3.Dot(diferencia, camara.right) >= 0f ? brazoAnimadoA : brazoAnimadoB;
        }
        else
        {
            brazoSaludo = brazoAnimadoA != null ? brazoAnimadoA : brazoAnimadoB;
        }

        tiempoSaludo = 0f;
    }

    void LateUpdate()
    {
        float objetivo = 0f;

        if (fuente != null && fuente.isPlaying)
        {
            fuente.GetOutputData(muestras, 0);

            float suma = 0f;
            for (int i = 0; i < muestras.Length; i++)
                suma += muestras[i] * muestras[i];

            objetivo = Mathf.Clamp01(Mathf.Sqrt(suma / muestras.Length) * sensibilidad);
        }

        nivel = Mathf.Lerp(nivel, objetivo, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        pesoEscucha = Mathf.MoveTowards(pesoEscucha, escuchando ? 1f : 0f, Time.deltaTime * 4f);

        AplicarCabeza(nivel);

        float pesoSaludo = CalcularPesoSaludo(out float onda);
        AplicarBrazo(brazoAnimadoA, nivel, brazoAnimadoA == brazoSaludo ? pesoSaludo : 0f, onda);
        AplicarBrazo(brazoAnimadoB, nivel, brazoAnimadoB == brazoSaludo ? pesoSaludo : 0f, onda);
    }

    private float CalcularPesoSaludo(out float onda)
    {
        onda = 0f;
        if (tiempoSaludo < 0f)
            return 0f;

        tiempoSaludo += Time.deltaTime;
        if (tiempoSaludo > duracionSaludo)
        {
            tiempoSaludo = -1f;
            return 0f;
        }

        float entrada = Mathf.SmoothStep(0f, 1f, tiempoSaludo / 0.4f);
        float salida = Mathf.SmoothStep(0f, 1f, (duracionSaludo - tiempoSaludo) / 0.5f);
        onda = Mathf.Sin(tiempoSaludo * frecuenciaOnda * 2f * Mathf.PI);
        return Mathf.Min(entrada, salida);
    }

    private void AplicarCabeza(float valor)
    {
        Vector3 ejeLateral = transform.right;
        Vector3 ejeVertical = transform.up;
        Vector3 ejeFrontal = transform.forward;
        float oscilacion = Mathf.Sin(Time.time * 2.2f);

        foreach (HuesoAnimado h in huesos)
        {
            h.hueso.localRotation = h.rotacionBase;

            Quaternion delta = Quaternion.AngleAxis(valor * h.angulo, ejeLateral);
            if (h.balanceo != 0f)
                delta = Quaternion.AngleAxis(oscilacion * h.balanceo * valor, ejeVertical) * delta;
            if (h.escucha > 0f)
                delta = Quaternion.AngleAxis(pesoEscucha * anguloInclinacionEscucha * h.escucha, ejeFrontal) * delta;

            h.hueso.rotation = delta * h.hueso.rotation;
        }
    }

    private void AplicarBrazo(BrazoAnimado b, float voz, float pesoSaludo, float onda)
    {
        if (b == null)
            return;

        Vector3 eje = transform.forward;

        float gesto = voz * anguloBrazoAlHablar * (0.6f + 0.4f * Mathf.Sin(Time.time * 3.1f + b.fase));
        float elevacion = (gesto + pesoSaludo * anguloBrazoSaludo) * b.signoElevacion;

        b.brazo.localRotation = b.brazoBase;
        b.brazo.rotation = Quaternion.AngleAxis(elevacion, eje) * b.brazo.rotation;

        b.antebrazo.localRotation = b.antebrazoBase;
        b.antebrazo.rotation = Quaternion.AngleAxis(pesoSaludo * anguloOndaSaludo * onda, eje) * b.antebrazo.rotation;
    }

    private void AgregarHueso(string nombre, float angulo, float balanceo, float escucha)
    {
        Transform hueso = BuscarHueso(nombre);
        if (hueso == null)
            return;

        huesos.Add(new HuesoAnimado
        {
            hueso = hueso,
            rotacionBase = hueso.localRotation,
            angulo = angulo,
            balanceo = balanceo,
            escucha = escucha
        });
    }

    private BrazoAnimado CrearBrazo(ConfigBrazo config, float fase)
    {
        Transform hombro = BuscarHueso(config.hombro);
        Transform brazo = BuscarHueso(config.brazo);
        Transform antebrazo = BuscarHueso(config.antebrazo);
        Transform punta = BuscarHueso(config.punta);

        if (hombro == null || brazo == null || antebrazo == null || punta == null)
            return null;

        Vector3 haciaAfuera = Vector3.ProjectOnPlane(punta.position - hombro.position, transform.forward);
        float lateral = Vector3.Dot(Vector3.Cross(transform.forward, haciaAfuera), transform.up);

        return new BrazoAnimado
        {
            brazo = brazo,
            antebrazo = antebrazo,
            punta = punta,
            brazoBase = brazo.localRotation,
            antebrazoBase = antebrazo.localRotation,
            signoElevacion = lateral >= 0f ? 1f : -1f,
            fase = fase
        };
    }

    private Transform BuscarHueso(string nombre)
    {
        foreach (Transform hijo in GetComponentsInChildren<Transform>(true))
        {
            if (hijo.name == nombre)
                return hijo;
        }

        return null;
    }
}
