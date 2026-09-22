using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class AudioClipData
{
    public string name;
    public AudioClip clip;
    public float volume = 1f;
    public bool loop = false;
    public bool is3D = true;
}

public class VRAudioManager : MonoBehaviour
{
    [Header("Audio Configuration")]
    public AudioMixerGroup masterMixer;
    public AudioMixerGroup musicMixer;
    public AudioMixerGroup sfxMixer;
    public AudioMixerGroup uiMixer;
    public AudioMixerGroup voiceMixer;

    [Header("Audio Clips")]
    public List<AudioClipData> audioClips = new List<AudioClipData>();

    [Header("3D Audio Settings")]
    public float maxDistance = 20f;
    public float minDistance = 1f;
    public AnimationCurve rolloffCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Header("Music Settings")]
    public AudioClip backgroundMusic;
    public bool playMusicOnStart = true;
    public float musicVolume = 0.5f;
    public float fadeDuration = 2f;

    [Header("Voice Instructions")]
    public AudioClip[] voiceInstructions;
    public bool enableVoiceGuidance = true;

    private Dictionary<string, AudioClipData> audioDict;
    private AudioSource musicSource;
    private AudioSource voiceSource;
    private List<AudioSource> sfxSources;
    private int maxSFXSources = 10;

    // �tat du gestionnaire audio
    private float masterVolume = 1f;
    private bool isMuted = false;
    private Coroutine currentMusicFade;

    void Awake()
    {
        // Singleton pattern
        if (FindObjectsByType<VRAudioManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeAudioClips();
        SetupAudioSources();

        if (playMusicOnStart && backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    void InitializeAudioClips()
    {
        audioDict = new Dictionary<string, AudioClipData>();

        // Ajoute les clips pr�d�finis
        foreach (AudioClipData clipData in audioClips)
        {
            if (!string.IsNullOrEmpty(clipData.name))
            {
                audioDict[clipData.name] = clipData;
            }
        }

        // Ajoute les clips audio standard pour l'application moteur
        AddStandardAudioClips();
    }

    void AddStandardAudioClips()
    {
        // Vous pouvez ajouter vos clips audio ici
        /*
        audioDict["snap"] = new AudioClipData 
        { 
            name = "snap", 
            clip = Resources.Load<AudioClip>("Audio/snap_sound"),
            volume = 0.8f,
            is3D = true
        };
        */
    }

    void SetupAudioSources()
    {
        // Source pour la musique
        GameObject musicObject = new GameObject("MusicSource");
        musicObject.transform.SetParent(transform);
        musicSource = musicObject.AddComponent<AudioSource>();

        musicSource.outputAudioMixerGroup = musicMixer;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        musicSource.spatialBlend = 0f; // 2D audio pour la musique

        // Source pour les instructions vocales
        GameObject voiceObject = new GameObject("VoiceSource");
        voiceObject.transform.SetParent(transform);
        voiceSource = voiceObject.AddComponent<AudioSource>();

        voiceSource.outputAudioMixerGroup = voiceMixer;
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f; // 2D audio pour les instructions

        // Pool de sources pour les effets sonores
        sfxSources = new List<AudioSource>();
        for (int i = 0; i < maxSFXSources; i++)
        {
            GameObject sfxObject = new GameObject($"SFXSource_{i}");
            sfxObject.transform.SetParent(transform);
            AudioSource sfxSource = sfxObject.AddComponent<AudioSource>();

            sfxSource.outputAudioMixerGroup = sfxMixer;
            sfxSource.playOnAwake = false;
            sfxSources.Add(sfxSource);
        }
    }

    #region Lecture Audio

    public void PlaySound(string soundName, Vector3? position = null, float volumeMultiplier = 1f)
    {
        if (audioDict.ContainsKey(soundName))
        {
            PlaySound(audioDict[soundName], position, volumeMultiplier);
        }
        else
        {
            Debug.LogWarning($"Son non trouv� : {soundName}");
        }
    }

    public void PlaySound(AudioClipData clipData, Vector3? position = null, float volumeMultiplier = 1f)
    {
        if (clipData.clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        if (source == null) return;

        // Configure la source audio
        source.clip = clipData.clip;
        source.volume = clipData.volume * volumeMultiplier * masterVolume;
        source.loop = clipData.loop;

        if (clipData.is3D && position.HasValue)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Custom;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        }
        else
        {
            source.spatialBlend = 0f; // 2D audio
        }

        source.Play();

        // Auto-stop pour les sons non-loop
        if (!clipData.loop)
        {
            StartCoroutine(StopSourceAfterClip(source, clipData.clip.length));
        }
    }

    public void PlaySoundAtTransform(string soundName, Transform target, float volumeMultiplier = 1f)
    {
        if (target != null)
        {
            PlaySound(soundName, target.position, volumeMultiplier);
        }
    }

    AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // Si aucune source disponible, utilise la premi�re (override)
        return sfxSources[0];
    }

    IEnumerator StopSourceAfterClip(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (source != null)
        {
            source.Stop();
        }
    }

    #endregion

    #region Gestion Musique

    public void PlayMusic(AudioClip musicClip, bool fadeIn = true)
    {
        if (musicClip == null || musicSource == null) return;

        if (currentMusicFade != null)
        {
            StopCoroutine(currentMusicFade);
        }

        if (fadeIn)
        {
            currentMusicFade = StartCoroutine(FadeInMusic(musicClip));
        }
        else
        {
            musicSource.clip = musicClip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }

    public void StopMusic(bool fadeOut = true)
    {
        if (musicSource == null) return;

        if (currentMusicFade != null)
        {
            StopCoroutine(currentMusicFade);
        }

        if (fadeOut)
        {
            currentMusicFade = StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }
    }

    IEnumerator FadeInMusic(AudioClip newClip)
    {
        // Fade out current music if playing
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeOutMusic());
        }

        // Start new music
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade in
        float elapsedTime = 0f;
        float targetVolume = musicVolume * masterVolume;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float volume = Mathf.Lerp(0f, targetVolume, elapsedTime / fadeDuration);
            musicSource.volume = volume;
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            musicSource.volume = volume;
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    #endregion

    #region Instructions Vocales

    public void PlayVoiceInstruction(int instructionIndex)
    {
        if (!enableVoiceGuidance || voiceInstructions == null) return;

        if (instructionIndex >= 0 && instructionIndex < voiceInstructions.Length)
        {
            AudioClip instruction = voiceInstructions[instructionIndex];
            if (instruction != null)
            {
                voiceSource.clip = instruction;
                voiceSource.volume = masterVolume;
                voiceSource.Play();
            }
        }
    }

    public void PlayVoiceInstruction(string instructionName)
    {
        // Trouve l'instruction par nom (si vous nommez vos clips)
        for (int i = 0; i < voiceInstructions.Length; i++)
        {
            if (voiceInstructions[i] != null && voiceInstructions[i].name.Contains(instructionName))
            {
                PlayVoiceInstruction(i);
                break;
            }
        }
    }

    public void StopVoiceInstruction()
    {
        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
    }

    #endregion

    #region Contr�les Volume

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        float sfxVolume = Mathf.Clamp01(volume);
        if (masterMixer != null)
        {
            masterMixer.audioMixer.SetFloat("SFXVolume", LinearToDecibel(sfxVolume));
        }
    }

    public void SetVoiceVolume(float volume)
    {
        float voiceVolume = Mathf.Clamp01(volume);
        if (voiceSource != null)
        {
            voiceSource.volume = voiceVolume * masterVolume;
        }
    }

    void UpdateAllVolumes()
    {
        // Met � jour la musique
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }

        // Met � jour les instructions vocales
        if (voiceSource != null)
        {
            voiceSource.volume = masterVolume;
        }

        // Met � jour le mixer principal
        if (masterMixer != null)
        {
            masterMixer.audioMixer.SetFloat("MasterVolume", LinearToDecibel(masterVolume));
        }
    }

    float LinearToDecibel(float linear)
    {
        return linear > 0 ? 20f * Mathf.Log10(linear) : -80f;
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : masterVolume;
    }

    #endregion

    #region Effets Audio Sp�cialis�s

    public void PlaySnapSound(Vector3 position)
    {
        PlaySound("snap", position, 1f);
    }

    public void PlayErrorSound()
    {
        PlaySound("error", null, 0.8f);
    }

    public void PlaySuccessSound()
    {
        PlaySound("success", null, 1f);
    }

    public void PlayButtonClickSound()
    {
        PlaySound("ui_click", null, 0.6f);
    }

    public void PlayPartSelectSound(Vector3 position)
    {
        PlaySound("part_select", position, 0.7f);
    }

    public void PlayAssemblyCompleteSound()
    {
        PlaySound("assembly_complete", null, 1f);
        // Ajoute un d�lai avant l'instruction vocale
        StartCoroutine(PlayDelayedVoiceInstruction("assembly_complete", 1f));
    }

    public void PlayEngineStartSound(Vector3 enginePosition)
    {
        PlaySound("engine_start", enginePosition, 1f);
    }

    IEnumerator PlayDelayedVoiceInstruction(string instructionName, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayVoiceInstruction(instructionName);
    }

    #endregion

    #region Gestion Contextuelle

    public void OnSceneChanged(string sceneName)
    {
        // Change la musique selon la sc�ne
        switch (sceneName)
        {
            case "MainMenu":
                PlayMusic(backgroundMusic, true);
                break;
            case "ExplodedView":
                PlayVoiceInstruction("exploded_view_intro");
                break;
            case "PartExploration":
                PlayVoiceInstruction("exploration_intro");
                break;
            case "ManualAssembly":
                PlayVoiceInstruction("assembly_intro");
                break;
        }
    }

    public void OnPartInteraction(string interactionType, string partName)
    {
        switch (interactionType)
        {
            case "select":
                PlayPartSelectSound(Vector3.zero);
                break;
            case "grab":
                PlaySound("part_grab", null, 0.5f);
                break;
            case "release":
                PlaySound("part_release", null, 0.5f);
                break;
            case "snap":
                PlaySnapSound(Vector3.zero);
                PlayVoiceInstruction($"{partName}_assembled");
                break;
        }
    }

    public void OnAssemblyProgress(float progress)
    {
        // Joue des sons selon le progr�s
        if (progress >= 0.25f && progress < 0.26f)
        {
            PlayVoiceInstruction("quarter_complete");
        }
        else if (progress >= 0.5f && progress < 0.51f)
        {
            PlayVoiceInstruction("half_complete");
        }
        else if (progress >= 0.75f && progress < 0.76f)
        {
            PlayVoiceInstruction("three_quarters_complete");
        }
        else if (progress >= 1f)
        {
            PlayAssemblyCompleteSound();
        }
    }

    #endregion

    #region M�thodes Utilitaires

    public bool IsPlayingMusic()
    {
        return musicSource != null && musicSource.isPlaying;
    }

    public bool IsPlayingVoice()
    {
        return voiceSource != null && voiceSource.isPlaying;
    }

    public void StopAllSounds()
    {
        // Arr�te tous les effets sonores
        foreach (AudioSource source in sfxSources)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }

        // Arr�te les instructions vocales
        StopVoiceInstruction();
    }

    public void PauseAllAudio()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();

        if (sfxSources != null)
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source != null && source.isPlaying)
                    source.Pause();
            }
        }

        if (voiceSource != null && voiceSource.isPlaying)
            voiceSource.Pause();
    }

    public void ResumeAllAudio()
    {
        // V�rifie musicSource avant utilisation
        if (musicSource != null)
            musicSource.UnPause();

        // V�rifie sfxSources avant utilisation
        if (sfxSources != null)
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source != null)  // V�rification suppl�mentaire
                    source.UnPause();
            }
        }

        // V�rifie voiceSource avant utilisation
        if (voiceSource != null)
            voiceSource.UnPause();
    }

    public void AddAudioClip(string name, AudioClip clip, float volume = 1f, bool loop = false, bool is3D = true)
    {
        AudioClipData newClipData = new AudioClipData
        {
            name = name,
            clip = clip,
            volume = volume,
            loop = loop,
            is3D = is3D
        };

        audioDict[name] = newClipData;
        audioClips.Add(newClipData);
    }

    #endregion

    #region Events et Callbacks

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PauseAllAudio();
        }
        else
        {
            ResumeAllAudio();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = isMuted ? 0f : masterVolume;
        }
    }

    void OnDestroy()
    {
        // Nettoie les coroutines
        if (currentMusicFade != null)
        {
            StopCoroutine(currentMusicFade);
        }
    }

    #endregion

    #region Debug et Testing

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    void Update()
    {
        // Touches de debug en �diteur
        if (Input.GetKeyDown(KeyCode.Alpha1))
            PlaySound("snap");
        if (Input.GetKeyDown(KeyCode.Alpha2))
            PlaySound("error");
        if (Input.GetKeyDown(KeyCode.Alpha3))
            PlaySound("success");
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMute();
        if (Input.GetKeyDown(KeyCode.P))
            PauseAllAudio();
        if (Input.GetKeyDown(KeyCode.R))
            ResumeAllAudio();
    }

    #endregion
}