using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using NAudio.Dsp;
using System.Windows;
using System.Diagnostics;

namespace ZuneDesk.Controls.AudioVisualizer
{
    public class SpectrumVisualizer : D2dControl.D2dControl
    {
        private SharpDX.DirectWrite.TextFormat debugForamt;
        const int FREQ_BANDS = 128;
        const int FFT_SIZE = 1024;

        public SpectrumVisualizer()
        {
            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;

            resCache.Add("WhiteBrush", t => new SolidColorBrush(t, new RawColor4(1.0f, 1.0f, 1.0f, 0.2f)));
            resCache.Add("RedBrush", t => new SolidColorBrush(t, new RawColor4(1.0f, 0.0f, 0.0f, 1.0f)));
            resCache.Add("GradientBrush", t => new LinearGradientBrush(
                t,
                new LinearGradientBrushProperties { StartPoint = new RawVector2 { X = 0, Y = (float)Height }, EndPoint = new RawVector2 { X = 0, Y = (float)(Height-(Height/4)) } },
                new GradientStopCollection(t,
                new[]
                {
                    new GradientStop
                    {
                        Color = new RawColor4(0.96f, 0.3f, 0.19f, 1.0f),
                        Position = 0
                    },
                    new GradientStop
                    {
                        Color = new RawColor4(0.95f, 0.15f, 0.37f, 1.0f),
                        Position = 1
                    }
                })
                ));      
       
            
            SharpDX.DirectWrite.Factory fontFactory = new SharpDX.DirectWrite.Factory();
            debugForamt = new SharpDX.DirectWrite.TextFormat(fontFactory, "Segoe UI", 12);

            Analyzer = new SpectrumAnalyzer(FFT_SIZE, FREQ_BANDS, (int)Height);
            FreqBands = new float[FREQ_BANDS];
            LerpedFreqBands = new float[FREQ_BANDS];
            Analyzer.OnBandsCalculated += OnBandsCalculated;
            Analyzer.Start();
        }

        private void OnBandsCalculated(object sender, BandsEventArgs e)
        {
            FreqBands = e.Result;
        }

        private float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        public override void Render(RenderTarget target)
        {
            target.Clear(new RawColor4(1.0f, 1.0f, 1.0f, 0.0f));
            RawRectangleF rect = new RawRectangleF(0, (float)Height - 64, 0, (float)Height);

              float width = (float)Width / FreqBands.Length;

            //  target.DrawText("FPS: " + Fps, debugForamt, new RawRectangleF(0, 0, 100, 100), resCache["RedBrush"] as Brush);

            for (int i = 0; i < FreqBands.Length; i++)
            {
                //  FreqBands[i] = Lerp(FreqBands[i], (float)Height, 0.009f);

                LerpedFreqBands[i] = Lerp(LerpedFreqBands[i], FreqBands[i], 0.4f);

                rect.Left = i * width;
                rect.Right = i * (float)width + width;
                rect.Top = LerpedFreqBands[i];
                //rect.Bottom = Height;
                //  Debug.WriteLine(FFTBuffer[i].X);
                // target.DrawText("F" + FreqBands[i], debugForamt, rect, resCache["RedBrush"] as Brush);
                target.FillRectangle(rect, resCache["GradientBrush"] as Brush);
            }
        }

        private SpectrumAnalyzer Analyzer;
        private float[] FreqBands;
        private float[] LerpedFreqBands;
    }
}
