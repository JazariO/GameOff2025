using System;
using UnityEngine;
using System.Runtime.InteropServices;

public class AudioPlaybackController : MonoBehaviour
{
    [SerializeField] private PlayerSaveDataSO playerSaveDataSO;

    [Header("FMOD Channel Group Settings")]
    [SerializeField] private bool useMasterChannelGroup = true;
    [SerializeField] private string customChannelGroupPath = ""; // e.g., "Bus:/SFX" or leave empty for master

    private FMOD.ChannelGroup channelGroup;
    private FMOD.Sound sound;
    private FMOD.Channel channel;
    private bool isPlaying = false;

    private void Start()
    {
        // Initialize the channel group
        InitializeChannelGroup();
    }

    private void InitializeChannelGroup()
    {
        FMOD.System system = FMODUnity.RuntimeManager.CoreSystem;

        if(useMasterChannelGroup)
        {
            // Use master channel group
            FMOD.RESULT result = system.getMasterChannelGroup(out channelGroup);
            if(result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to get master channel group: {result}");
            }
            else
            {
                Debug.Log("Using master channel group for playback");
            }
        }
        else if(!string.IsNullOrEmpty(customChannelGroupPath))
        {
            // Try to get a bus as a channel group
            FMOD.Studio.Bus bus = FMODUnity.RuntimeManager.GetBus(customChannelGroupPath);
            if(bus.isValid())
            {
                FMOD.RESULT result = bus.getChannelGroup(out channelGroup);
                if(result != FMOD.RESULT.OK)
                {
                    Debug.LogError($"Failed to get channel group from bus '{customChannelGroupPath}': {result}");
                    // Fallback to master
                    system.getMasterChannelGroup(out channelGroup);
                }
                else
                {
                    Debug.Log($"Using bus channel group: {customChannelGroupPath}");
                }
            }
            else
            {
                Debug.LogWarning($"Bus '{customChannelGroupPath}' not found. Using master channel group.");
                system.getMasterChannelGroup(out channelGroup);
            }
        }
        else
        {
            // Default to master if nothing specified
            system.getMasterChannelGroup(out channelGroup);
            Debug.Log("No custom channel group specified. Using master channel group.");
        }
    }

    // Call this to play the recorded audio
    public void PlayRecordedAudio()
    {
        if(playerSaveDataSO == null)
        {
            Debug.LogError("PlayerSaveDataSO is not assigned!");
            return;
        }

        if(playerSaveDataSO.audioClipData == null || playerSaveDataSO.audioClipData.Length == 0)
        {
            Debug.LogError("No audio data found in PlayerSaveDataSO!");
            return;
        }

        // Stop any currently playing sound
        StopPlayback();

        // Convert 16-bit PCM bytes back to float samples
        float[] floatSamples = ConvertPCMToFloat(playerSaveDataSO.audioClipData);

        // Create FMOD sound from the float samples
        CreateAndPlayFMODSound(floatSamples, playerSaveDataSO.audioSampleRate);
    }

    private float[] ConvertPCMToFloat(byte[] pcmData)
    {
        int sampleCount = pcmData.Length / 2; // 2 bytes per 16-bit sample
        float[] floatSamples = new float[sampleCount];

        for(int i = 0; i < sampleCount; i++)
        {
            // Read 16-bit sample (little-endian)
            short pcmSample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));

            // Convert to float range (-1.0 to 1.0)
            floatSamples[i] = pcmSample / 32768.0f;
        }

        return floatSamples;
    }

    private void CreateAndPlayFMODSound(float[] samples, int sampleRate)
    {
        FMOD.System system = FMODUnity.RuntimeManager.CoreSystem;

        // Create sound info
        FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();
        exinfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        exinfo.numchannels = 1; // Mono
        exinfo.defaultfrequency = sampleRate;
        exinfo.length = (uint)(samples.Length * sizeof(float));
        exinfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;

        // Create the sound
        FMOD.RESULT result = system.createSound(
            "",
            FMOD.MODE.OPENUSER | FMOD.MODE.LOOP_OFF,
            ref exinfo,
            out sound
        );

        if(result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to create FMOD sound: {result}");
            return;
        }

        // Lock the sound to write data
        IntPtr ptr1, ptr2;
        uint len1, len2;
        result = sound.@lock(0, exinfo.length, out ptr1, out ptr2, out len1, out len2);

        if(result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to lock FMOD sound: {result}");
            sound.release();
            return;
        }

        // Copy float samples to FMOD sound buffer
        Marshal.Copy(samples, 0, ptr1, samples.Length);

        // Unlock the sound
        sound.unlock(ptr1, ptr2, len1, len2);

        // Play the sound on the specified channel group
        result = system.playSound(sound, channelGroup, false, out channel);

        if(result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to play FMOD sound: {result}");
            sound.release();
            return;
        }

        isPlaying = true;
        Debug.Log($"Playing back {samples.Length} samples at {sampleRate} Hz");
    }

    public void StopPlayback()
    {
        if(isPlaying && channel.hasHandle())
        {
            channel.stop();
            isPlaying = false;
        }

        if(sound.hasHandle())
        {
            sound.release();
        }
    }

    public bool IsPlaying()
    {
        if(!isPlaying) return false;

        if(channel.hasHandle())
        {
            bool playing;
            channel.isPlaying(out playing);

            if(!playing)
            {
                isPlaying = false;
                sound.release();
            }

            return playing;
        }

        return false;
    }

    private void OnDestroy()
    {
        StopPlayback();
    }

    private void Update()
    {
        // Optional: Check if playback finished
        if(isPlaying && !IsPlaying())
        {
            Debug.Log("Playback finished");
        }
    }
}