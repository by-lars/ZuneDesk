using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Dsp;
using NAudio.Extras;
using System.Diagnostics;

namespace ZuneDesk.Controls.AudioVisualizer
{
    public class BandsEventArgs : EventArgs
    {
        public BandsEventArgs(float[] result) 
        {
            Result = result;    
        }

        public float[] Result { get; private set; }
    }

    class SpectrumAnalyzer
    {
        public event EventHandler<BandsEventArgs> OnBandsCalculated;

        public SpectrumAnalyzer(int fftSize, int nBands, int BandHeight)
        {
            CurrentIndex = 0;
            MaxBandHeight = BandHeight;
            //FFT is mirrored at half, so we need 2x the size
            FFTBuffer = new Complex[fftSize * 2];
            FreqBands = new float[nBands];
            MValue = (int)Math.Log(fftSize*2, 2.0);
            FreqBandsEventArgs = new BandsEventArgs(FreqBands);

            Capture = new WasapiLoopbackCapture();
            Capture.DataAvailable += OnCaptureDataAvailable;
        }

        public void Start()
        {
            Capture.StartRecording();
        }

        public void Stop()
        {
            Capture.StopRecording();
        }

        private void OnCaptureDataAvailable(object sender, WaveInEventArgs e)
        {
            for(int i = 0; i < e.BytesRecorded; i += Capture.WaveFormat.BlockAlign)
            {
                float sample = BitConverter.ToSingle(e.Buffer, i);

                FFTBuffer[CurrentIndex].X = (float)(sample );
                FFTBuffer[CurrentIndex].Y = 0;
                CurrentIndex++;

                if(CurrentIndex >= FFTBuffer.Length)
                {
                    CurrentIndex = 0;
                    CalculateBands();
                }
            }
        }

        private float GetAmplitude(Complex c)
        {
            double intensityDB = 20 * Math.Log10(Math.Sqrt(c.X * c.X + c.Y * c.Y));
            double minDB = -100;
            if (intensityDB < minDB) intensityDB = minDB;
            double percent = intensityDB / minDB;
            return (float)percent;
        }

        private void CalculateBands()
        {
            FastFourierTransform.FFT(true, MValue, FFTBuffer);

            int offset = 0;
    
            for(int i = 0; i < FreqBands.Length; i++)
            {
                double x = i * 0.034;
                int windowSize = (int)Math.Pow(2, x) + 2;

                for(int j = 0; j < windowSize; j++)
                {
                    float amplitude = GetAmplitude(FFTBuffer[offset]);
                    FreqBands[i] += amplitude * MaxBandHeight;
                    offset++;
                }

                FreqBands[i] /= windowSize;
                if(FreqBands[i] > MaxBandHeight - 10)
                {
                    FreqBands[i] = MaxBandHeight - 10;
                }
     
            }

 
            OnBandsCalculated(this, FreqBandsEventArgs);
        }

        private WasapiLoopbackCapture Capture;
        private int CurrentIndex;
        private int MValue;
        private BandsEventArgs FreqBandsEventArgs;
        private Complex[] FFTBuffer;
        private float[] FreqBands;
        private int MaxBandHeight;
    }
}
