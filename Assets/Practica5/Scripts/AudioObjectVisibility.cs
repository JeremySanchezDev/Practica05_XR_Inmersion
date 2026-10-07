using UnityEngine;
using UnityEngine.InputSystem;

namespace Practica5
{
    /// <summary>
    /// Segunda prueba del Paso 12 del tutorial: oculta visualmente Audio_Object (sus renderers)
    /// manteniendo el sonido, para localizarlo solo por audio espacial. Tecla V: ocultar/mostrar.
    /// </summary>
    public class AudioObjectVisibility : MonoBehaviour
    {
        [SerializeField] Key m_ToggleKey = Key.V;

        Renderer[] m_Renderers;

        public static bool IsHidden { get; private set; }

        void Awake()
        {
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            IsHidden = false;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[m_ToggleKey].wasPressedThisFrame)
                SetHidden(!IsHidden);
        }

        public void SetHidden(bool hidden)
        {
            IsHidden = hidden;
            foreach (var r in m_Renderers)
                if (r != null)
                    r.enabled = !hidden;
            Debug.Log($"[Practica5] {name} {(hidden ? "oculto: localízalo solo por el sonido" : "visible")}");
        }
    }
}
