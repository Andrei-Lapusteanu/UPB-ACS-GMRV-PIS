using System;
using NaughtyAttributes;
using UnityEngine;

namespace L3.Scripts
{
    public class WaterVolumeDetector : MonoBehaviour
    {
        [SerializeField] 
        [BoxGroup("Internal components")]
        private Collider waterVolumeCollider;

        [SerializeField] 
        [BoxGroup("External components")]
        private Camera playerCamera;

        public event Action<bool> OnUnderwaterStateChanged;
    
        private bool isCamUnderwater;

        private void Start()
        {
            // Assumes character is spawned above water!
            isCamUnderwater = false;
            OnUnderwaterStateChanged?.Invoke(isCamUnderwater);
        }

        private void Update()
        {
            var waterBounds = waterVolumeCollider.bounds;

            // Check if camera is inside bounds.
            if (waterBounds.Contains(playerCamera.transform.position))
            {
                if (isCamUnderwater) return;
                
                isCamUnderwater = true;
                OnUnderwaterStateChanged?.Invoke(true);
            }
            else
            {
                if (!isCamUnderwater) return;
                
                isCamUnderwater = false;
                OnUnderwaterStateChanged?.Invoke(false);
            }
        }
    }
}