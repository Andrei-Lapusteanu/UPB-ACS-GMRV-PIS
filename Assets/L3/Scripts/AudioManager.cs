using NaughtyAttributes;
using UnityEngine;

namespace L3.Scripts
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] 
        [BoxGroup("External components")]
        private FPSController fpsController;

        [SerializeField] 
        [BoxGroup("External components")]
        private DayNightCycleController dayNightCycleController;

        [SerializeField] 
        [BoxGroup("External components")]
        private WaterVolumeDetector waterVolumeDetector;
    }
}