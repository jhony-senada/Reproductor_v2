using System;
using NAudio.Wave;
using NAudio.Dsp;

namespace Reproductor
{
    public class MotorAudio
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private EqualizerSampleProvider eqProvider;
        private EqualizerBand[] eqBands;

        private bool detencionManual = false;

        public event EventHandler CancionTerminada;

        public bool IsPlaying => outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing;
        public TimeSpan TiempoActual => audioFile?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TiempoTotal => audioFile?.TotalTime ?? TimeSpan.Zero;

        public float CurrentPeak => eqProvider?.CurrentPeak ?? 0f;
        public float[] FftMagnitudes => eqProvider?.FftMagnitudes;

        public MotorAudio()
        {
            eqBands = new EqualizerBand[]
            {
                new EqualizerBand { Frequency = 31f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 62f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 125f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 250f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 500f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 1000f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 2000f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 4000f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 8000f, Gain = 0f, Bandwidth = 0.8f },
                new EqualizerBand { Frequency = 16000f, Gain = 0f, Bandwidth = 0.8f }
            };
        }

        public void Reproducir(string ruta, float volumenBase, float gananciaPreamp)
        {
            LimpiarAudio();

            try
            {
                audioFile = new AudioFileReader(ruta);
                eqProvider = new EqualizerSampleProvider(audioFile, eqBands);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(eqProvider);

                ActualizarVolumen(volumenBase, gananciaPreamp);

                outputDevice.PlaybackStopped += OnPlaybackStopped;

                detencionManual = false;
                outputDevice.Play();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al inicializar el audio: " + ex.Message);
            }
        }

        public void AlternarPlayPause()
        {
            if (outputDevice != null)
            {
                if (outputDevice.PlaybackState == PlaybackState.Playing) outputDevice.Pause();
                else outputDevice.Play();
            }
        }

        public void CambiarPosicion(double segundos)
        {
            if (audioFile != null) audioFile.CurrentTime = TimeSpan.FromSeconds(segundos);
        }

        public void ActualizarVolumen(float volumenBase, float gananciaPreampDb)
        {
            if (audioFile != null)
            {
                float multiplicadorPreamp = (float)Math.Pow(10, gananciaPreampDb / 20.0);
                audioFile.Volume = Math.Min(volumenBase * multiplicadorPreamp, 2.0f);
            }
        }

        public void ActualizarBandaEcualizador(int indiceBanda, float ganancia)
        {
            if (indiceBanda >= 0 && indiceBanda < eqBands.Length)
            {
                eqBands[indiceBanda].Gain = ganancia;
                eqProvider?.Update();
            }
        }

        public void DetenerTotalmente() => LimpiarAudio();

        private void LimpiarAudio()
        {
            detencionManual = true;
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }
            if (audioFile != null)
            {
                audioFile.Dispose();
                audioFile = null;
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (!detencionManual && audioFile != null)
            {
                double tiempoRestante = audioFile.TotalTime.TotalSeconds - audioFile.CurrentTime.TotalSeconds;
                if (tiempoRestante <= 1.0) CancionTerminada?.Invoke(this, EventArgs.Empty);
            }
        }

        public class EqualizerBand
        {
            public float Frequency { get; set; }
            public float Gain { get; set; }
            public float Bandwidth { get; set; }
        }

        public class EqualizerSampleProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private readonly EqualizerBand[] bands;
            private readonly BiQuadFilter[,] filters;
            private readonly int channels;
            public float CurrentPeak { get; private set; }
            public float[] FftMagnitudes { get; private set; } = new float[512];
            private Complex[] fftBuffer = new Complex[1024];
            private int fftPos = 0;
            private int m = (int)Math.Log(1024, 2);

            public EqualizerSampleProvider(ISampleProvider source, EqualizerBand[] bands)
            {
                this.source = source;
                this.bands = bands;
                channels = source.WaveFormat.Channels;
                filters = new BiQuadFilter[bands.Length, channels];
                Update();
            }

            public WaveFormat WaveFormat => source.WaveFormat;

            public void Update()
            {
                for (int bandIndex = 0; bandIndex < bands.Length; bandIndex++)
                {
                    var band = bands[bandIndex];
                    for (int ch = 0; ch < channels; ch++)
                    {
                        if (filters[bandIndex, ch] == null)
                            filters[bandIndex, ch] = BiQuadFilter.PeakingEQ(source.WaveFormat.SampleRate, band.Frequency, band.Bandwidth, band.Gain);
                        else
                            filters[bandIndex, ch].SetPeakingEq(source.WaveFormat.SampleRate, band.Frequency, band.Bandwidth, band.Gain);
                    }
                }
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesRead = source.Read(buffer, offset, count);
                float max = 0;

                for (int i = 0; i < samplesRead; i++)
                {
                    int ch = i % channels;
                    for (int band = 0; band < bands.Length; band++)
                    {
                        buffer[offset + i] = filters[band, ch].Transform(buffer[offset + i]);
                    }
                    float muestra = buffer[offset + i];
                    float absValue = Math.Abs(muestra);
                    if (absValue > max) max = absValue;
                    if (ch == 0)
                    {
                        fftBuffer[fftPos].X = (float)(muestra * FastFourierTransform.HammingWindow(fftPos, 1024));
                        fftBuffer[fftPos].Y = 0;
                        fftPos++;
                        if (fftPos >= 1024)
                        {
                            FastFourierTransform.FFT(true, m, fftBuffer);
                            for (int j = 0; j < 512; j++)
                            {
                                float magnitud = (float)Math.Sqrt(fftBuffer[j].X * fftBuffer[j].X + fftBuffer[j].Y * fftBuffer[j].Y);
                                FftMagnitudes[j] = Math.Max(magnitud, FftMagnitudes[j] * 0.8f);
                            }
                            fftPos = 0;
                        }
                    }
                }
                CurrentPeak = max;
                return samplesRead;
            }
        }
    }
}