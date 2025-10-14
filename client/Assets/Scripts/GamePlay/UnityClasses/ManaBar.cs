using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.UnityClasses
{
    public class ManaBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;
        

        public void UpdateMana(float current, float max)
        {
            slider.value = current / max;
            text.text = $"{current:F0}/{max}";
        }
    }
}