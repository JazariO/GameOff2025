using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

class ScriptUsageDspCapture : MonoBehaviour
{
    private FMOD.DSP_READ_CALLBACK mReadCallback;
    private FMOD.DSP mCaptureDSP;
    private float[] mDataBuffer;
    private GCHandle mObjHandle;
    private uint mBufferLength;
    private int mChannels = 0;

    [Header("Recording Settings")]
    public PlayerSaveDataSO playerSaveData;
    public float recordingDuration = 3.0f;

    [Header("Spectrum Settings")]
    public int spectrumWidth = 1024;
    public int spectrumHeight = 256;
    public int fftSize = 2048; // Must be power of 2

    [Header("Spectrum Visualization Adjustments")]
    [Range(0.1f, 10f)]
    [Tooltip("Multiplier for overall spectrum intensity")]
    public float intensityMultiplier = 2.0f;

    [Range(0.1f, 5f)]
    [Tooltip("Power curve for contrast (lower = more contrast)")]
    public float contrastPower = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("Minimum threshold to cut off noise")]
    public float noiseFloor = 0.05f;

    [Range(1f, 10f)]
    [Tooltip("Logarithmic scale factor (higher = more dynamic range)")]
    public float logScaleFactor = 2f;

    [Range(0.5f, 4f)]
    [Tooltip("Frequency distribution curve (higher = more emphasis on high frequencies)")]
    public float frequencyCurve = 2f;

    [Range(0.01f, 1f)]
    [Tooltip("Global normalization threshold (lower = brighter overall)")]
    public float normalizationThreshold = 0.1f;

    private List<float> recordedSamples = new List<float>();
    private bool isRecording = false;
    private int sampleRate = 0;
    private int totalSamplesNeeded = 0;

    [AOT.MonoPInvokeCallback(typeof(FMOD.DSP_READ_CALLBACK))]
    static FMOD.RESULT CaptureDSPReadCallback(ref FMOD.DSP_STATE dsp_state, IntPtr inbuffer, IntPtr outbuffer, uint length, int inchannels, ref int outchannels)
    {
        FMOD.DSP_STATE_FUNCTIONS functions = dsp_state.functions;

        IntPtr userData;
        functions.getuserdata(ref dsp_state, out userData);

        GCHandle objHandle = GCHandle.FromIntPtr(userData);
        ScriptUsageDspCapture obj = objHandle.Target as ScriptUsageDspCapture;

        // Save the channel count out for the update function
        obj.mChannels = inchannels;

        // Copy the incoming buffer to process later
        int lengthElements = (int)length * inchannels;
        Marshal.Copy(inbuffer, obj.mDataBuffer, 0, lengthElements);

        // If recording, capture the audio as mono
        if(obj.isRecording && obj.recordedSamples.Count < obj.totalSamplesNeeded)
        {
            for(int i = 0; i < length; i++)
            {
                // Convert to mono by averaging all channels
                float monoSample = 0f;
                for(int ch = 0; ch < inchannels; ch++)
                {
                    monoSample += obj.mDataBuffer[i * inchannels + ch];
                }
                monoSample /= inchannels;

                obj.recordedSamples.Add(monoSample);

                // Stop recording when we have enough samples
                if(obj.recordedSamples.Count >= obj.totalSamplesNeeded)
                {
                    obj.isRecording = false;
                    break;
                }
            }
        }

        // Copy the inbuffer to the outbuffer so we can still hear it
        Marshal.Copy(obj.mDataBuffer, 0, outbuffer, lengthElements);

        return FMOD.RESULT.OK;
    }

    void Start()
    {
        // Get sample rate
        FMODUnity.RuntimeManager.CoreSystem.getSoftwareFormat(out sampleRate, out _, out _);

        // Assign the callback to a member variable to avoid garbage collection
        mReadCallback = CaptureDSPReadCallback;

        // Allocate a data buffer large enough for 8 channels
        uint bufferLength;
        int numBuffers;
        FMODUnity.RuntimeManager.CoreSystem.getDSPBufferSize(out bufferLength, out numBuffers);
        mDataBuffer = new float[bufferLength * 8];
        mBufferLength = bufferLength;

        // Get a handle to this object to pass into the callback
        mObjHandle = GCHandle.Alloc(this);
        if(mObjHandle != null)
        {
            // Define a basic DSP that receives a callback each mix to capture audio
            FMOD.DSP_DESCRIPTION desc = new FMOD.DSP_DESCRIPTION();
            desc.numinputbuffers = 1;
            desc.numoutputbuffers = 1;
            desc.read = mReadCallback;
            desc.userdata = GCHandle.ToIntPtr(mObjHandle);

            // Create an instance of the capture DSP and attach it to the master channel group to capture all audio
            FMOD.ChannelGroup masterCG;
            if(FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out masterCG) == FMOD.RESULT.OK)
            {
                if(FMODUnity.RuntimeManager.CoreSystem.createDSP(ref desc, out mCaptureDSP) == FMOD.RESULT.OK)
                {
                    if(masterCG.addDSP(0, mCaptureDSP) != FMOD.RESULT.OK)
                    {
                        Debug.LogWarning("FMOD: Unable to add mCaptureDSP to the master channel group");
                    }
                }
                else
                {
                    Debug.LogWarning("FMOD: Unable to create a DSP: mCaptureDSP");
                }
            }
            else
            {
                Debug.LogWarning("FMOD: Unable to create a master channel group: masterCG");
            }
        }
        else
        {
            Debug.LogWarning("FMOD: Unable to create a GCHandle: mObjHandle");
        }
    }

    void OnDestroy()
    {
        if(mObjHandle != null)
        {
            // Remove the capture DSP from the master channel group
            FMOD.ChannelGroup masterCG;
            if(FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out masterCG) == FMOD.RESULT.OK)
            {
                if(mCaptureDSP.hasHandle())
                {
                    masterCG.removeDSP(mCaptureDSP);

                    // Release the DSP and free the object handle
                    mCaptureDSP.release();
                }
            }
            mObjHandle.Free();
        }
    }

    // Call this to start recording
    public void StartRecording()
    {
        if(playerSaveData == null)
        {
            Debug.LogError("PlayerSaveDataSO is not assigned!");
            return;
        }

        recordedSamples.Clear();
        totalSamplesNeeded = Mathf.RoundToInt(sampleRate * recordingDuration);
        isRecording = true;
        Debug.Log($"Started recording {recordingDuration} seconds at {sampleRate} Hz (mono)");
    }

    // Call this to regenerate the spectrum with current settings from existing audio data
    public void RegenerateSpectrum()
    {
        if(playerSaveData == null || playerSaveData.audioClipData == null || playerSaveData.audioClipData.Length == 0)
        {
            Debug.LogError("No audio data available to regenerate spectrum!");
            return;
        }

        Debug.Log("Regenerating spectrum from saved audio data with current settings...");

        // Convert PCM bytes back to float samples
        recordedSamples.Clear();
        int sampleCount = playerSaveData.audioClipData.Length / 2;

        for(int i = 0; i < sampleCount; i++)
        {
            short pcmSample = (short)(playerSaveData.audioClipData[i * 2] | (playerSaveData.audioClipData[i * 2 + 1] << 8));
            float floatSample = pcmSample / 32768.0f;
            recordedSamples.Add(floatSample);
        }

        // Regenerate spectrum
        GenerateSpectrumTexture();

        recordedSamples.Clear();
        Debug.Log("Spectrum regenerated successfully!");
    }

    // Convert float samples to 16-bit PCM and save
    private void SaveRecordingToSO()
    {
        if(recordedSamples.Count == 0) return;

        if(playerSaveData == null)
        {
            Debug.LogError("PlayerSaveDataSO is not assigned! Cannot save recording.");
            recordedSamples.Clear();
            return;
        }

        // Convert float samples (-1.0 to 1.0) to 16-bit PCM
        byte[] pcmData = new byte[recordedSamples.Count * 2]; // 2 bytes per sample

        for(int i = 0; i < recordedSamples.Count; i++)
        {
            // Clamp and convert to 16-bit range
            float clampedSample = Mathf.Clamp(recordedSamples[i], -1.0f, 1.0f);
            short pcmSample = (short)(clampedSample * short.MaxValue);

            // Convert to bytes (little-endian)
            pcmData[i * 2] = (byte)(pcmSample & 0xFF);
            pcmData[i * 2 + 1] = (byte)((pcmSample >> 8) & 0xFF);
        }

        // Save audio data to ScriptableObject
        playerSaveData.audioClipData = pcmData;
        playerSaveData.audioSampleRate = sampleRate;
        playerSaveData.audioClipDuration = recordingDuration;

        Debug.Log($"Saved {pcmData.Length} bytes ({recordedSamples.Count} samples) to PlayerSaveDataSO");

        // Generate and save spectrum texture
        GenerateSpectrumTexture();

        recordedSamples.Clear();
    }

    private void GenerateSpectrumTexture()
    {
        int textureDataSize = spectrumWidth * spectrumHeight * 3; // RGB24
        byte[] textureData = new byte[textureDataSize];

        Debug.Log($"Generating spectrum from {recordedSamples.Count} samples");
        Debug.Log($"Settings - Intensity: {intensityMultiplier}, Contrast: {contrastPower}, NoiseFloor: {noiseFloor}, LogScale: {logScaleFactor}");

        // Calculate how many samples per time slice
        int samplesPerSlice = recordedSamples.Count / spectrumWidth;

        // Store all spectrum slices to find global max for normalization
        List<float[]> allSpectrums = new List<float[]>();
        float globalMax = 0f;

        // First pass: generate all spectrums and find global maximum
        for(int x = 0; x < spectrumWidth; x++)
        {
            // Get samples for this time slice
            int startSample = x * samplesPerSlice;
            int endSample = Mathf.Min(startSample + fftSize, recordedSamples.Count);
            int actualSamples = endSample - startSample;

            if(actualSamples < fftSize / 2) // Need enough samples
            {
                allSpectrums.Add(new float[fftSize / 2]);
                continue;
            }

            // Prepare FFT input
            float[] fftInput = new float[fftSize];
            for(int i = 0; i < actualSamples; i++)
            {
                // Apply Hanning window
                float window = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / actualSamples));
                fftInput[i] = recordedSamples[startSample + i] * window;
            }

            // Perform FFT
            float[] spectrum = PerformFFT(fftInput);
            allSpectrums.Add(spectrum);

            // Track global maximum
            for(int i = 0; i < spectrum.Length; i++)
            {
                if(spectrum[i] > globalMax)
                    globalMax = spectrum[i];
            }
        }

        Debug.Log($"Global max magnitude: {globalMax}");

        // Prevent division by zero and apply normalization threshold
        float effectiveMax = Mathf.Max(globalMax * normalizationThreshold, 0.0001f);

        Debug.Log($"Effective max for normalization: {effectiveMax}");

        // Track statistics for debugging
        int nonZeroPixels = 0;
        float avgMagnitude = 0f;
        float maxProcessedMagnitude = 0f;

        // Second pass: render normalized spectrum to texture
        int halfHeight = spectrumHeight / 2;

        for(int x = 0; x < spectrumWidth; x++)
        {
            float[] spectrum = allSpectrums[x];

            for(int y = 0; y < halfHeight; y++)
            {
                // Map y pixel to frequency bin (adjustable logarithmic frequency scale)
                float normalizedY = (float)y / halfHeight;
                // Use adjustable exponential mapping for frequency distribution
                float freqIndex = Mathf.Pow(normalizedY, frequencyCurve) * (spectrum.Length - 1);
                int binIndex = Mathf.FloorToInt(freqIndex);

                // Get magnitude with interpolation
                float magnitude = 0f;
                if(binIndex < spectrum.Length - 1)
                {
                    float t = freqIndex - binIndex;
                    magnitude = Mathf.Lerp(spectrum[binIndex], spectrum[binIndex + 1], t);
                }
                else if(binIndex < spectrum.Length)
                {
                    magnitude = spectrum[binIndex];
                }

                // Apply intensity multiplier
                magnitude *= intensityMultiplier;

                // Normalize to effective max
                magnitude = magnitude / effectiveMax;

                // Apply noise floor cutoff
                if(magnitude < noiseFloor)
                {
                    magnitude = 0f;
                }

                // Apply logarithmic scaling for better dynamic range
                magnitude = Mathf.Log10(1 + magnitude * 99) / logScaleFactor;

                // Apply adjustable contrast boost
                magnitude = Mathf.Pow(magnitude, contrastPower);

                magnitude = Mathf.Clamp01(magnitude);

                byte intensityByte = (byte)(magnitude * 255);

                // Track stats
                if(intensityByte > 0)
                {
                    nonZeroPixels++;
                    avgMagnitude += magnitude;
                }
                if(magnitude > maxProcessedMagnitude)
                {
                    maxProcessedMagnitude = magnitude;
                }

                // Draw on upper half (mirrored vertically - high freq at top)
                int upperY = halfHeight - 1 - y;
                int upperPixelIndex = (upperY * spectrumWidth + x) * 3;
                textureData[upperPixelIndex] = intensityByte;     // R
                textureData[upperPixelIndex + 1] = intensityByte; // G
                textureData[upperPixelIndex + 2] = intensityByte; // B

                // Mirror to lower half
                int lowerY = halfHeight + y;
                int lowerPixelIndex = (lowerY * spectrumWidth + x) * 3;
                textureData[lowerPixelIndex] = intensityByte;     // R
                textureData[lowerPixelIndex + 1] = intensityByte; // G
                textureData[lowerPixelIndex + 2] = intensityByte; // B
            }
        }

        // Log statistics
        int totalPixels = spectrumWidth * spectrumHeight;
        if(nonZeroPixels > 0)
        {
            avgMagnitude /= nonZeroPixels;
            Debug.Log($"Spectrum stats - Non-zero pixels: {nonZeroPixels}/{totalPixels} ({(float)nonZeroPixels / totalPixels * 100:F1}%), Avg magnitude: {avgMagnitude:F4}, Max processed: {maxProcessedMagnitude:F4}");
        }
        else
        {
            Debug.LogWarning("WARNING: All pixels are zero! No spectrum data generated.");
        }

        // Save to ScriptableObject
        playerSaveData.spectrumTextureData = textureData;
        Debug.Log($"Generated spectrum texture: {spectrumWidth}x{spectrumHeight} ({textureData.Length} bytes)");
    }

    // Simple FFT implementation (Cooley-Tukey algorithm)
    private float[] PerformFFT(float[] input)
    {
        int n = input.Length;

        // Real and imaginary parts
        float[] real = new float[n];
        float[] imag = new float[n];

        Array.Copy(input, real, n);

        // Bit-reverse copy
        int bits = (int)Mathf.Log(n, 2);
        for(int i = 0; i < n; i++)
        {
            int rev = BitReverse(i, bits);
            if(rev > i)
            {
                float temp = real[i];
                real[i] = real[rev];
                real[rev] = temp;
            }
        }

        // FFT computation
        for(int s = 1; s <= bits; s++)
        {
            int m = 1 << s;
            int m2 = m >> 1;
            float omega = -2f * Mathf.PI / m;

            for(int k = 0; k < n; k += m)
            {
                for(int j = 0; j < m2; j++)
                {
                    float angle = omega * j;
                    float cosAngle = Mathf.Cos(angle);
                    float sinAngle = Mathf.Sin(angle);

                    int evenIndex = k + j;
                    int oddIndex = k + j + m2;

                    float tReal = cosAngle * real[oddIndex] - sinAngle * imag[oddIndex];
                    float tImag = sinAngle * real[oddIndex] + cosAngle * imag[oddIndex];

                    real[oddIndex] = real[evenIndex] - tReal;
                    imag[oddIndex] = imag[evenIndex] - tImag;
                    real[evenIndex] += tReal;
                    imag[evenIndex] += tImag;
                }
            }
        }

        // Calculate magnitudes
        float[] magnitudes = new float[n / 2];
        for(int i = 0; i < n / 2; i++)
        {
            magnitudes[i] = Mathf.Sqrt(real[i] * real[i] + imag[i] * imag[i]) / n;
        }

        return magnitudes;
    }

    private int BitReverse(int n, int bits)
    {
        int reversed = 0;
        for(int i = 0; i < bits; i++)
        {
            reversed = (reversed << 1) | (n & 1);
            n >>= 1;
        }
        return reversed;
    }

    const float WIDTH = 0.01f;
    const float HEIGHT = 10.0f;
    const float YOFFSET = 5.0f;

    void Update()
    {
        // Check if recording just finished
        if(!isRecording && recordedSamples.Count > 0)
        {
            SaveRecordingToSO();
        }

        // Visualization (optional - you can remove this if not needed)
        for(int j = 0; j < mBufferLength; j++)
        {
            for(int i = 0; i < mChannels; i++)
            {
                float x = j * WIDTH;
                float y = mDataBuffer[(j * mChannels) + i] * HEIGHT;
                Debug.DrawLine(new Vector3(x, (YOFFSET * i) + y, 0), new Vector3(x, (YOFFSET * i) - y, 0), Color.green);
            }
        }
    }
}