using System;
using UnityEngine;

namespace Practica5
{
    /// <summary>
    /// Objeto invisible que solo se puede localizar por su sonido espacial (HRTF).
    /// Cuando la cabeza del usuario se acerca a menos de <see cref="m_RevealRadius"/>,
    /// el objeto se revela, suena un chime y, tras unos segundos, se reubica en otro punto.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class HiddenSoundTarget : MonoBehaviour
    {
        [SerializeField] float m_RevealRadius = 0.6f;
        [SerializeField] float m_RelocateDelay = 4f;
        [SerializeField] GameObject m_RevealVisual;
        [SerializeField] AudioClip m_FoundClip;
        [SerializeField] Transform[] m_SpawnPoints = Array.Empty<Transform>();

        AudioSource m_Beacon;
        Transform m_Head;
        int m_SpawnIndex;
        float m_FoundTime = -1f;

        public static event Action<HiddenSoundTarget> Found;

        public int TimesFound { get; private set; }
        public bool IsRevealed => m_FoundTime >= 0f;
        public AudioSource Beacon => m_Beacon;

        void Awake()
        {
            m_Beacon = GetComponent<AudioSource>();
            SetRevealed(false);
        }

        void Start()
        {
            if (m_SpawnPoints.Length > 0)
                MoveTo(0);
        }

        void Update()
        {
            if (m_Head == null)
            {
                var cam = Camera.main;
                if (cam == null)
                    return;
                m_Head = cam.transform;
            }

            if (IsRevealed)
            {
                if (Time.time - m_FoundTime >= m_RelocateDelay)
                    Relocate();
                return;
            }

            if ((m_Head.position - transform.position).sqrMagnitude <= m_RevealRadius * m_RevealRadius)
                Reveal();
        }

        void Reveal()
        {
            m_FoundTime = Time.time;
            TimesFound++;
            SetRevealed(true);
            m_Beacon.Stop();
            if (m_FoundClip != null)
                m_Beacon.PlayOneShot(m_FoundClip);
            Debug.Log($"[Practica5] Objeto invisible encontrado ({TimesFound}) en {transform.position}");
            Found?.Invoke(this);
        }

        void Relocate()
        {
            m_FoundTime = -1f;
            SetRevealed(false);
            if (m_SpawnPoints.Length > 1)
                MoveTo((m_SpawnIndex + 1) % m_SpawnPoints.Length);
            m_Beacon.Play();
        }

        void MoveTo(int index)
        {
            var point = m_SpawnPoints[index];
            if (point == null)
                return;
            m_SpawnIndex = index;
            transform.position = point.position;
        }

        void SetRevealed(bool revealed)
        {
            if (m_RevealVisual != null)
                m_RevealVisual.SetActive(revealed);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_RevealRadius);
        }
    }
}
