using UnityEngine;

public class NavegacionPasos : MonoBehaviour
{
    [SerializeField] private GameObject[] pasos;

    void Start() => IrAPaso(0);

    public void IrAPaso(int indice)
    {
        for (int i = 0; i < pasos.Length; i++)
            pasos[i].SetActive(i == indice);
    }
}
