using UnityEngine;

namespace SG {
    public class UIManager : MonoBehaviour
    {
        public GameObject selectWindow;

        public void OpenSelectWindow()
        {
            selectWindow.SetActive(true);
        }

        public void CloseSelectWindow()
        {
            selectWindow.SetActive(false);
        }
    }
}