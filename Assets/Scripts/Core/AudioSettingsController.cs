
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Ползунки громкости")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private const string MusicParameter = "MusicVolume";
    private const string SfxParameter = "SfxVolume";

    private void Start()
    {
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 0.7f);

        musicVolumeSlider.value = musicVolume;
        sfxVolumeSlider.value = sfxVolume;

        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);

        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat(MusicParameter, ConvertToDecibels(value));
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        audioMixer.SetFloat(SfxParameter, ConvertToDecibels(value));
        PlayerPrefs.SetFloat("SfxVolume", value);
        PlayerPrefs.Save();
    }

    private float ConvertToDecibels(float value)
    {
        return value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
    }
}
