using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalProgressEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioSource audioSource;
    SpaceLoader loader;

    ParticleSystem.EmissionModule emission;
    ParticleSystem.VelocityOverLifetimeModule lifetimeVelocity;

    public float minEmissionRate = 17;
    public float maxEmissionRate = 400; 
    
    public float minEmissionVelocityY = 2f;
    public float maxEmissionVelocityY = 10f;

    [System.Flags]
    public enum Effects
    {
        None = 1,
        EmissionRate = 2,
        EmissionVelocityY = 4
    }
    public Effects effects = Effects.EmissionRate & Effects.EmissionVelocityY;

    private void Awake()
    {
        particles = GetComponentInChildren<ParticleSystem>();
        emission = particles.emission;
        lifetimeVelocity = particles.velocityOverLifetime;
        loader = GetComponentInParent<SpaceLoader>();
        if (!audioSource)
            audioSource = GetComponentInChildren<AudioSource>();

    }
    private void Update()
    {
        if (loader.exitingProgress == 0f)
        {

            if (particles.gameObject.activeSelf)
            {
                particles.gameObject.SetActive(false);
            }
            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();
        }
        else
        {
            if (!particles.gameObject.activeSelf)
            {
                particles.gameObject.SetActive(true);
            }
            if ((effects & Effects.EmissionRate) != 0) emission.rateOverTime = minEmissionRate + loader.exitingProgress * (maxEmissionRate - minEmissionRate);
            if ((effects & Effects.EmissionVelocityY) != 0) lifetimeVelocity.speedModifier = minEmissionVelocityY + loader.exitingProgress * (maxEmissionVelocityY - minEmissionVelocityY);

            if (audioSource && !audioSource.isPlaying)
                audioSource.Play();
            if (audioSource)
                audioSource.volume = loader.exitingProgress;
        }
    }
}
