using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Seengene.XDK
{

    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        private AudioSource m_AudioSource = null;
        [SerializeField] private AudioClip m_DefaultAudioClip = null;

        private static MusicPlayer m_Instance = null;
        public static MusicPlayer Instance
        {
            get { return m_Instance; }
        }

        void Awake()
        {
            m_Instance = this;
            m_AudioSource = GetComponent<AudioSource>();
        }

        public void PlayMusic()
        {
            m_AudioSource.clip = m_DefaultAudioClip;
            m_AudioSource.Play();
        }

        public void PlayMusic(AudioClip audioClip)
        {
            m_AudioSource.clip = audioClip;
            m_AudioSource.Play();
        }
    }
}