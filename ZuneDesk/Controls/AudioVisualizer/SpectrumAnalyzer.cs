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

        public SpectrumAnalyzer(int fftSize, int nBands)
        {
            CurrentIndex = 0;
            
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
           // int offset = 0;
            //for (int i = 0; i < FFTBuffer.Length/2; i++)
            //{
            //       int windowSize = (FFTBuffer.Length/2) / FreqBands.Length;

            //        Complex c = FFTBuffer[offset];
            //       // Debug.WriteLine(j * i);
            //        double intensityDB = 15 * Math.Log10(Math.Sqrt(c.X * c.X + c.Y * c.Y));
            //        double minDB = -90;
            //        if (intensityDB < minDB) intensityDB = minDB;
            //        double percent = intensityDB / minDB;

            //        FreqBands[i/windowSize] += (float)percent * 1080;
            //        offset += 1;

            ////    offset += (i % windowSize == windowSize ? 1 : 0);
            //}

            int offset = 0;
    
            for(int i = 0; i < FreqBands.Length; i++)
            {
                double x = i * 0.034;
                int windowSize = (int)Math.Pow(2, x) + 2;

                for(int j = 0; j < windowSize; j++)
                {
                    float amplitude = GetAmplitude(FFTBuffer[offset]);
                    FreqBands[i] += amplitude * 1080.0f;
                    offset++;
                }

                FreqBands[i] /= windowSize;
                if(FreqBands[i] > 1070.0f)
                {
                    FreqBands[i] = 1070.0f;
                }
               // FreqBands[i] *= 1080.0f;
            }

           // Debug.WriteLine(offset);

            //int windowSize = 4;
            //for(int i = 0; i < FreqBands.Length; i++)
            //{
              
            //    for(int j = 0; j < windowSize; j++)
            //    {
            //        FreqBands[i] += GetAmplitude(FFTBuffer[offset + j]);
            //    }

            //    FreqBands[i] /= windowSize;

            //    if(i % windowSize == 0)
            //    {
            //        windowSize += 1;
            //    }
            //    offset += windowSize;

            //}

            //int index = 0;
            //for(int i = 0; i < FFTBuffer.Length/2; i+=windowSize)
            //{
            //    for(int j = 0; j < windowSize; j++)
            //    {
            //        Complex c = FFTBuffer[i + j];

            //        double intensityDB = 10 * Math.Log10(Math.Sqrt(c.X * c.X + c.Y * c.Y));
            //        double minDB = -90;
            //        if (intensityDB < minDB) intensityDB = minDB;
            //        double percent = intensityDB / minDB;

            //        FreqBands[index] += (float)percent * 1080;
            //    }

            //    FreqBands[index] /= windowSize;


            //    index++;
            //}

            //double intensityDB = 20 * Math.Log10(Math.Sqrt(c.X * c.X + c.Y * c.Y));
            //double minDB = -90;
            //if (intensityDB < minDB) intensityDB = minDB;
            //double percent = intensityDB / minDB;
            //// we want 0dB to be at the top (i.e. yPos = 0)
            //double yPos = percent * MaxBarHeight;
            //return yPos;

            OnBandsCalculated(this, FreqBandsEventArgs);
        }

        private WasapiLoopbackCapture Capture;
        private int CurrentIndex;
        private int MValue;
        private BandsEventArgs FreqBandsEventArgs;
        private Complex[] FFTBuffer;
        private float[] FreqBands;
    }
}
