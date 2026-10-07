using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Practica5
{
    /// <summary>
    /// Radio agarrable con audio 3D. Con el grip se toma y se mueve (la fuente de sonido
    /// se desplaza con ella); con el gatillo (Activate) se enciende o se apaga.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class RadioInteractable : MonoBehaviour
    {
        [SerializeField] Renderer m_IndicatorLight;
        [SerializeField] Color m_OnColor = new Color(0.2f, 1f, 0.3f);
        [SerializeField] Color m_OffColor = new Color(0.25f, 0.05f, 0.05f);

        static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColor = Shader.PropertyToID("_EmissionColor");

        AudioSource m_Source;
        XRGrabInteractable m_Grab;
        MaterialPropertyBlock m_Block;
        bool m_On;

        /// <summary>Estado lógico de la radio (no depende de si el editor pausa el audio al perder el foco).</summary>
        public bool IsOn => m_On;

        void Awake()
        {
            m_Source = GetComponent<AudioSource>();
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Block = new MaterialPropertyBlock();
            m_On = m_Source.playOnAwake;
        }

        void OnEnable()
        {
            m_Grab.activated.AddListener(OnActivated);
        }

        void Start() => UpdateIndicator();

        void OnDisable()
        {
            m_Grab.activated.RemoveListener(OnActivated);
        }

        void OnActivated(ActivateEventArgs args) => Toggle();

        public void Toggle()
        {
            m_On = !m_On;
            if (m_On)
                m_Source.UnPause();
            else
                m_Source.Pause();
            if (m_On && !m_Source.isPlaying)
                m_Source.Play();
            UpdateIndicator();
        }

        void UpdateIndicator()
        {
            if (m_IndicatorLight == null || m_Source == null)
                return;
            var c = m_On ? m_OnColor : m_OffColor;
            m_IndicatorLight.GetPropertyBlock(m_Block);
            m_Block.SetColor(k_BaseColor, c);
            m_Block.SetColor(k_EmissionColor, c * 2f);
            m_IndicatorLight.SetPropertyBlock(m_Block);
        }
    }
}
