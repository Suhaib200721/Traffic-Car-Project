using UnityEngine;

/// <summary>
/// Lightweight procedural audio manager for Traffic Escape 3D.
/// Generates clean arcade audio waveforms in-memory so no external audio files are required.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource engineSource;

    private AudioClip coinClip;
    private AudioClip crashClip;
    private AudioClip boostClip;
    private AudioClip buttonClickClip;
    private AudioClip gameOverClip;
    private AudioClip shieldClip;
    private AudioClip missionCompleteClip;
    private AudioClip hornClip;

    private bool isMuted = false;
    private const string MutePrefKey = "TRAFFIC_ESCAPE_AUDIO_MUTED";

    // Debounce timestamps to avoid audio clipping / overlapping
    private float lastCoinSoundTime;
    private float lastCrashSoundTime;
    private float lastBoostSoundTime;
    private float lastShieldSoundTime;
    private float lastButtonSoundTime;
    private float lastMissionSoundTime;
    private float lastHornSoundTime;

    public bool IsMuted => isMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        if (engineSource == null)
            engineSource = gameObject.AddComponent<AudioSource>();

        engineSource.loop = true;
        engineSource.volume = 0.25f;

        isMuted = PlayerPrefs.GetInt(MutePrefKey, 0) == 1;
        ApplyMute();

        GenerateProceduralClips();
    }

    public void ToggleMute()
    {
        SetMuted(!isMuted);
    }

    public void SetMuted(bool mute)
    {
        isMuted = mute;
        PlayerPrefs.SetInt(MutePrefKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMute();
    }

    private void ApplyMute()
    {
        if (sfxSource != null) sfxSource.mute = isMuted;
        if (engineSource != null) engineSource.mute = isMuted;
    }

    private void GenerateProceduralClips()
    {
        coinClip = CreateCoinChime();
        crashClip = CreateCrashSound();
        boostClip = CreateBoostSound();
        buttonClickClip = CreateButtonClickSound();
        gameOverClip = CreateGameOverJingle();
        shieldClip = CreateShieldSound();
        missionCompleteClip = CreateMissionCompleteFanfare();
        hornClip = CreateHornSound();

        // Engine hum loop
        engineSource.clip = CreateEngineLoop();
    }

    public void PlayCoinSound()
    {
        if (isMuted) return;
        if (Time.unscaledTime - lastCoinSoundTime < 0.05f) return;
        lastCoinSoundTime = Time.unscaledTime;

        if (sfxSource && coinClip)
            sfxSource.PlayOneShot(coinClip, 0.7f);
    }

    public void PlayCrashSound()
    {
        if (isMuted) return;
        if (Time.unscaledTime - lastCrashSoundTime < 0.1f) return;
        lastCrashSoundTime = Time.unscaledTime;

        if (sfxSource && crashClip)
            sfxSource.PlayOneShot(crashClip, 0.9f);
    }

    public void PlayBoostSound()
    {
        if (isMuted) return;
        if (Time.unscaledTime - lastBoostSoundTime < 0.1f) return;
        lastBoostSoundTime = Time.unscaledTime;

        if (sfxSource && boostClip)
            sfxSource.PlayOneShot(boostClip, 0.6f);
    }

    public void PlayButtonClick()
    {
        if (isMuted) return;
        if (Time.unscaledTime - lastButtonSoundTime < 0.05f) return;
        lastButtonSoundTime = Time.unscaledTime;

        if (sfxSource && buttonClickClip)
            sfxSource.PlayOneShot(buttonClickClip, 0.6f);
    }

    public void PlayGameOverSound()
    {
        if (isMuted) return;
        if (sfxSource && gameOverClip)
            sfxSource.PlayOneShot(gameOverClip, 0.8f);
    }

    public void PlayShieldSound()
    {
        if (isMuted) return;
        if (Time.unscaledTime - lastShieldSoundTime < 0.08f) return;
        lastShieldSoundTime = Time.unscaledTime;

        if (sfxSource && shieldClip)
            sfxSource.PlayOneShot(shieldClip, 0.7f);
    }

    public void PlayMissionCompleteSound()
    {
        if (isMuted) return;
        if (Time.unscaledTime - lastMissionSoundTime < 0.2f) return;
        lastMissionSoundTime = Time.unscaledTime;

        if (sfxSource && missionCompleteClip)
            sfxSource.PlayOneShot(missionCompleteClip, 0.75f);
    }

    public void PlayHornSound()
    {
        if (isMuted) return;
        if (Time.unscaledTime - lastHornSoundTime < 0.25f) return;
        lastHornSoundTime = Time.unscaledTime;

        if (sfxSource && hornClip)
            sfxSource.PlayOneShot(hornClip, 0.85f);
    }

    public void StartEngine()
    {
        if (engineSource && !engineSource.isPlaying)
            engineSource.Play();
    }

    public void StopEngine()
    {
        if (engineSource && engineSource.isPlaying)
            engineSource.Stop();
    }

    public void SetEnginePitch(float pitch)
    {
        if (engineSource)
            engineSource.pitch = Mathf.Clamp(pitch, 0.8f, 1.8f);
    }

    // --- Procedural Audio Synthesizers ---

    private AudioClip CreateCoinChime()
    {
        int sampleRate = 44100;
        float duration = 0.25f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = t < 0.12f ? 987.77f : 1318.51f; // B5 to E6
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("CoinChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCrashSound()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        System.Random rnd = new System.Random(42);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-7f * t);
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float lowRumble = Mathf.Sin(2f * Mathf.PI * 75f * t);
            samples[i] = (noise * 0.7f + lowRumble * 0.3f) * envelope * 0.7f;
        }

        AudioClip clip = AudioClip.Create("CrashSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBoostSound()
    {
        int sampleRate = 44100;
        float duration = 0.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(250f, 750f, t / duration);
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("BoostSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateButtonClickSound()
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * 800f * t) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("ButtonClick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateGameOverJingle()
    {
        int sampleRate = 44100;
        float duration = 0.8f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(440f, 180f, t / duration);
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("GameOverJingle", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateShieldSound()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(400f, 900f, t / duration);
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("ShieldSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateMissionCompleteFanfare()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // 4-note ascending fanfare: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
        float stepTime = duration / 4f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            int step = Mathf.Clamp((int)(t / stepTime), 0, 3);
            float freq = step switch
            {
                0 => 523.25f,
                1 => 659.25f,
                2 => 783.99f,
                _ => 1046.50f
            };
            float noteT = (t - step * stepTime) / stepTime;
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(noteT)) * (1f - (t / duration) * 0.3f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("MissionCompleteFanfare", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateEngineLoop()
    {
        int sampleRate = 44100;
        float duration = 1.0f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float baseTone = Mathf.Sin(2f * Mathf.PI * 60f * t);
            float harmonic = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.5f;
            float sub = Mathf.Sin(2f * Mathf.PI * 30f * t) * 0.3f;
            samples[i] = (baseTone + harmonic + sub) * 0.3f;
        }

        AudioClip clip = AudioClip.Create("EngineLoop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateHornSound()
    {
        int sampleRate = 44100;
        float duration = 0.38f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Dual automotive horn frequencies: F#4 (370Hz) and A#4 (466Hz) with rich harmonic overtone
        float f1 = 370f;
        float f2 = 466f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float attack = Mathf.Clamp01(t / 0.02f);
            float release = Mathf.Clamp01((duration - t) / 0.04f);
            float env = attack * release;

            float tone1 = Mathf.Sin(2f * Mathf.PI * f1 * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f1 * 2f * t);
            float tone2 = Mathf.Sin(2f * Mathf.PI * f2 * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f2 * 2f * t);
            samples[i] = (tone1 + tone2) * 0.4f * env;
        }

        AudioClip clip = AudioClip.Create("HornBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
