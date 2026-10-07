using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Practica5
{
    /// <summary>
    /// Los mandos simulados en PC no tienen hápticos: cada intento de vibración genera el aviso
    /// "Failed to get haptic capabilities of XRSimulatedController". Si no hay un visor real activo,
    /// se desactiva la vibración del rig; con un visor (Meta Quest) se mantiene.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SimulatorHapticsGuard : MonoBehaviour
    {
        void Awake()
        {
            if (XRSettings.isDeviceActive)
                return;

            foreach (var feedback in GetComponentsInChildren<SimpleHapticFeedback>(true))
                feedback.enabled = false;
            foreach (var player in GetComponentsInChildren<HapticImpulsePlayer>(true))
                player.enabled = false;
        }
    }
}
