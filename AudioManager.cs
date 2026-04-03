using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Games
{
    internal sealed class AudioManager : IDisposable
    {
        private readonly object syncRoot = new();

        private readonly CachedSoundChain? landingSoundChain;
        private readonly CachedSoundChain? lineClearSoundChain;

        private readonly AudioFileReader? backgroundMusicReader;
        private readonly LoopStream? backgroundMusicLoop;
        private readonly WaveOutEvent? backgroundMusicOutput;

        private bool disposed;

        public AudioManager(string landingSoundPath, string lineClearSoundPath, string backgroundMusicPath)
        {
            landingSoundChain = CreateSoundEffectChain(landingSoundPath);
            lineClearSoundChain = CreateSoundEffectChain(lineClearSoundPath);
            (backgroundMusicReader, backgroundMusicLoop, backgroundMusicOutput) = CreateBackgroundMusicChain(backgroundMusicPath);
        }

        public void PlayLandingImpact()
        {
            PlaySoundEffect(landingSoundChain);
        }

        public void PlayLineClear()
        {
            PlaySoundEffect(lineClearSoundChain);
        }

        private void PlaySoundEffect(CachedSoundChain? soundChain)
        {
            if (disposed || soundChain is null)
            {
                return;
            }

            lock (syncRoot)
            {
                try
                {
                    soundChain.Mixer.AddMixerInput(new CachedSampleProvider(soundChain.Format, soundChain.Samples));
                }
                catch
                {
                    // Keep gameplay uninterrupted if audio playback fails.
                }
            }
        }

        public void StartBackgroundMusic()
        {
            if (disposed || backgroundMusicLoop is null || backgroundMusicOutput is null)
            {
                return;
            }

            lock (syncRoot)
            {
                try
                {
                    backgroundMusicOutput.Stop();
                    backgroundMusicLoop.Position = 0;
                    backgroundMusicOutput.Play();
                }
                catch
                {
                    // Keep gameplay uninterrupted if audio playback fails.
                }
            }
        }

        public void StopBackgroundMusic()
        {
            if (disposed || backgroundMusicLoop is null || backgroundMusicOutput is null)
            {
                return;
            }

            lock (syncRoot)
            {
                try
                {
                    backgroundMusicOutput.Stop();
                    backgroundMusicLoop.Position = 0;
                }
                catch
                {
                    // Keep gameplay uninterrupted if audio playback fails.
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            lock (syncRoot)
            {
                landingSoundChain?.Output.Stop();
                lineClearSoundChain?.Output.Stop();
                backgroundMusicOutput?.Stop();

                landingSoundChain?.Output.Dispose();
                lineClearSoundChain?.Output.Dispose();
                backgroundMusicOutput?.Dispose();
                backgroundMusicLoop?.Dispose();
                backgroundMusicReader?.Dispose();
            }
        }

        private static CachedSoundChain? CreateSoundEffectChain(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using AudioFileReader reader = new(path);
                WaveFormat format = reader.WaveFormat;
                List<float> samples = new();
                float[] buffer = new float[format.SampleRate * format.Channels];
                int read;

                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        samples.Add(buffer[i]);
                    }
                }

                MixingSampleProvider mixer = new(format) { ReadFully = true };
                WaveOutEvent output = new()
                {
                    DesiredLatency = 50,
                    NumberOfBuffers = 2
                };
                output.Init(mixer);
                output.Play();
                return new CachedSoundChain(format, samples.ToArray(), mixer, output);
            }
            catch
            {
                return null;
            }
        }

        private static (AudioFileReader? reader, LoopStream? loop, WaveOutEvent? output) CreateBackgroundMusicChain(string path)
        {
            if (!File.Exists(path))
            {
                return (null, null, null);
            }

            try
            {
                AudioFileReader reader = new(path);
                LoopStream loop = new(reader);
                WaveOutEvent output = new()
                {
                    DesiredLatency = 120,
                    NumberOfBuffers = 2
                };
                output.Init(loop);
                return (reader, loop, output);
            }
            catch
            {
                return (null, null, null);
            }
        }

        private sealed class CachedSampleProvider : ISampleProvider
        {
            private readonly WaveFormat waveFormat;
            private readonly float[] samples;
            private int position;

            public CachedSampleProvider(WaveFormat waveFormat, float[] samples)
            {
                this.waveFormat = waveFormat;
                this.samples = samples;
            }

            public WaveFormat WaveFormat => waveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                int availableSamples = samples.Length - position;
                int samplesToCopy = Math.Min(availableSamples, count);
                Array.Copy(samples, position, buffer, offset, samplesToCopy);
                position += samplesToCopy;
                return samplesToCopy;
            }
        }

        private sealed class CachedSoundChain
        {
            public CachedSoundChain(WaveFormat format, float[] samples, MixingSampleProvider mixer, WaveOutEvent output)
            {
                Format = format;
                Samples = samples;
                Mixer = mixer;
                Output = output;
            }

            public WaveFormat Format { get; }
            public float[] Samples { get; }
            public MixingSampleProvider Mixer { get; }
            public WaveOutEvent Output { get; }
        }

        private sealed class LoopStream : WaveStream
        {
            private readonly WaveStream sourceStream;

            public LoopStream(WaveStream sourceStream)
            {
                this.sourceStream = sourceStream;
            }

            public override WaveFormat WaveFormat => sourceStream.WaveFormat;

            public override long Length => sourceStream.Length;

            public override long Position
            {
                get => sourceStream.Position;
                set => sourceStream.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int totalBytesRead = 0;

                while (totalBytesRead < count)
                {
                    int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        if (sourceStream.Position == 0)
                        {
                            break;
                        }

                        sourceStream.Position = 0;
                        continue;
                    }

                    totalBytesRead += bytesRead;
                }

                return totalBytesRead;
            }
        }
    }
}
