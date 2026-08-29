using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogicaBotones : MonoBehaviour
{
    public static string escenaAnterior;

    void Start()
    {
        
    }

    public void Entrenamiento(){
        CargarEscena("Entrenamiento");
    }

    public void Resumen(){
        CargarEscena("Resumen");
    }

    public void AsistenteIA(){
        Application.OpenURL("https://chatgpt.com/g/g-67c490a077f08191b9e64c5a058f691f-odontosmart-gpt");
    }

    public void Creditos(){
        CargarEscena("Creditos");
    }

    void CargarEscena(string nombreEscena){
        escenaAnterior = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(nombreEscena);
    }

    public void Atras(){
        SceneManager.LoadScene(escenaAnterior);
    }
}