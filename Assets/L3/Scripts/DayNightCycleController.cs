using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace L3.Scripts
{
    public class DayNightCycleController : MonoBehaviour
    {
        [SerializeField]
        [BoxGroup("UI components")]
        private Slider slider;
    
        [SerializeField]
        [BoxGroup("External components")]
        private Light directionalLight;
    
        [SerializeField]
        [BoxGroup("Light Settings")]
        private float dayRotation = 90f;

        [SerializeField]
        [BoxGroup("Light Settings")]
        private float nightRotation = 270f;
    
        [SerializeField]
        [BoxGroup("Day/night Settings")]
        [Range(0f, 2f)]
        private float cycleSpeed = 0.5f;
    
        public event Action<float> OnDayNightCycleValueChanged;

        private float currentXRotation;
    
        private void Start()
        {
            // Initialize slider value based on current light rotation.
            currentXRotation = directionalLight.transform.eulerAngles.x;
            var normalizedValue = Mathf.InverseLerp(dayRotation, nightRotation, currentXRotation);
            slider.SetValueWithoutNotify(normalizedValue);
        }
    
        private void OnEnable() => slider.onValueChanged.AddListener(OnSliderValueChanged);

        private void OnDisable() => slider.onValueChanged.RemoveListener(OnSliderValueChanged);

        private void OnSliderValueChanged(float value)
        {
            // Store the new X rotation.
            currentXRotation = Mathf.Lerp(dayRotation, nightRotation, value);
        
            // Apply the new rotation.
            var targetRotation = Quaternion.Euler(currentXRotation, 0f, 0f);
            directionalLight.transform.rotation = targetRotation;
        
            OnDayNightCycleValueChanged?.Invoke(value);
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.Minus))  slider.value -= Mathf.Clamp01(cycleSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.Equals)) slider.value += Mathf.Clamp01(cycleSpeed * Time.deltaTime);
        }
    }
}
