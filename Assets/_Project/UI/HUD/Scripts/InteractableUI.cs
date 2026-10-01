using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SG {
    public class InteractableUI : MonoBehaviour
    {
        [FormerlySerializedAs("interavtableText")]
        public TMP_Text interactableText;
        public TMP_Text itemText;
        public RawImage itemImage;
    }
}
