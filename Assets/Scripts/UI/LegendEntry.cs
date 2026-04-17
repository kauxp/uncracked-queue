using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QueueDungeon.UI {
    /// <summary>
    /// A single legend row: icon (Image) + label (TextMeshProUGUI).
    /// Attach to a prefab with a child Image and child TextMeshProUGUI.
    /// </summary>
    public class LegendEntry : MonoBehaviour {
        [Header("References")]
        public Image iconImage;
        public TextMeshProUGUI labelText;

        /// <summary>
        /// Initialize this legend row with a sprite, color, and descriptive text.
        /// </summary>
        public void Setup(Sprite sprite, Color color, string text) {
            if (iconImage != null) {
                iconImage.sprite = sprite;
                iconImage.color = color;
                iconImage.preserveAspect = true;
            }
            if (labelText != null) {
                labelText.text = text;
            }
        }
    }
}
