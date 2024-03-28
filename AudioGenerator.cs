using NewTek;
using System.Diagnostics;
using System.Runtime.InteropServices;
public class AudioGenerator : IDisposable
{
    private nint wavePtr;
    private bool disposedValue;

    public NDIlib.audio_frame_v2_t SineFrame { get; private set; }

    public AudioGenerator(int sampleRateHz, int channels, int frequencyHz)
    {
        var samplesPerPeriod = sampleRateHz / (int)frequencyHz;
        var precomputedWave = new float[samplesPerPeriod];

        var angleStep = (2.0 * Math.PI) / samplesPerPeriod;

        for (var i = 0; i < samplesPerPeriod; i++)
        {
            precomputedWave[i] = (float)Math.Sin(angleStep * i);
        }

        var totalCycles = 20;

        this.wavePtr = Marshal.AllocHGlobal(sizeof(float) * samplesPerPeriod * channels * totalCycles);


        for (var i = 0; i < channels; i++)
        {
            for (var loop = 0; loop < totalCycles; loop++)
            {
                var channelPtr = nint.Add(this.wavePtr, (i * samplesPerPeriod * totalCycles + loop * samplesPerPeriod) * sizeof(float));
                Marshal.Copy(precomputedWave, 0, channelPtr, precomputedWave.Length);
            }
        }

        this.SineFrame = new NDIlib.audio_frame_v2_t()
        {
            channel_stride_in_bytes = sizeof(float) * precomputedWave.Length * totalCycles,
            no_channels = channels,
            no_samples = precomputedWave.Length * totalCycles,
            p_data = this.wavePtr,
            sample_rate = sampleRateHz,
            timecode = NDIlib.send_timecode_synthesize,
            p_metadata = nint.Zero
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            if(this.wavePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.wavePtr);
                this.wavePtr = nint.Zero;
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            this.disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~AudioGenerator()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}