/*
_HMA_Indicator

*/

using System;
using System.Drawing;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class _SN_I_HMA_GainLimit : IndicatorObject
    {
        private HMA_ mHMA;
        private VariableSeries<Double> vHMA;
        private VariableSeries<Double> vHMA_GainLimit;

        private IPlotObject Plot1;

        public _SN_I_HMA_GainLimit (object ctx) : base(ctx)
        {
            Length = 9;
            GainLimit = 30;
            LeastError = 0.0001;
        }

        private ISeries<double> price { get; set; }

        [Input] public int Length { get; set; }
        [Input] public int displace { get; set; }
        [Input] public int GainLimit { get; set; }
        [Input] public double LeastError { get; set; }

        private double alpha, gain, error, bestGain;
        private double leastError;
        private Color vHMA_Color;

        protected override void Create()
        {
            mHMA = new HMA_(this);
            vHMA = new VariableSeries<Double>(this);
            vHMA_GainLimit = new VariableSeries<double>(this);

            Plot1 = AddPlot(new PlotAttributes("HMA_GainLimit", 0, Color.Red,
                                    Color.Empty, 0, 0, true));
        }

        protected override void StartCalc()
        {
            mHMA.price = Bars.Close;
            mHMA.length = Length;
            price = Bars.Close;

            alpha = 2.0 / (Length + 1.0);
        }

        protected override void CalcBar()
        {
            vHMA.Value = mHMA[0];

            for (int i = -GainLimit; i < GainLimit; i++)
            {
                gain = i / 100.0;

                vHMA_GainLimit.Value = alpha * (vHMA.Value + gain * (Bars.Close[0] - vHMA_GainLimit[1]))
                     + (1.0 - alpha) * vHMA_GainLimit[1];

                error = Bars.CloseValue - vHMA_GainLimit.Value;

                if (Math.Abs(error) < LeastError)
                {
                    //leastError = Math.Abs(error);
                    bestGain = gain;
                }
            }

            vHMA_GainLimit.Value = alpha * (vHMA.Value + bestGain * (Bars.Close[0] - vHMA_GainLimit[1]))
                     + (1.0 - alpha) * vHMA_GainLimit[1];

            if (vHMA_GainLimit[0] > vHMA_GainLimit[1])
            {
                vHMA_Color = Color.LimeGreen;
            }

            if (vHMA_GainLimit[0] < vHMA_GainLimit[1])
            {
                vHMA_Color = Color.Red;
            }

            if (((displace >= 0)
                 || Bars.CurrentBar > Math.Abs(displace))
                 && Bars.CurrentBar >= Length)
            {
                // Check out value
                //Output.WriteLine(Bars.TimeValue.ToString() + "\t"
                //    + (vHMA.Value).ToString("F6"));

                //Output.WriteLine("bestGain" + bestGain);

                Plot1.Set(1, vHMA_GainLimit[1], vHMA_Color);
            }
        }
    }
}