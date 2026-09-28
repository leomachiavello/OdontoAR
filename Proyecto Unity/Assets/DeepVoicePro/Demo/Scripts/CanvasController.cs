using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AiKodexDeepVoicePro
{
    public class CanvasController : MonoBehaviour
    {
        public Button launcher;

        void Start()
        {
#if UNITY_EDITOR
            launcher.onClick.AddListener(TaskOnClick);
#endif
        }

#if UNITY_EDITOR
        void TaskOnClick()
        {
            EditorApplication.ExecuteMenuItem("Window/DeepVoice Pro");
        }
#endif
    }
}
