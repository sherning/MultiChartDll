/*
_SN_ZeroLagMA_120     20200103

*/

using System;
using System.Drawing;
// using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class _SN_ZeroLagMA_121 : IndicatorObject
    {
        [Input]
        public int Length { get; set; }
        [Input]
        public int GainLimit { get; set; }

        /// constructor
        public _SN_ZeroLagMA_121(object _ctx) : base(_ctx)
        {
            // define input default values
            Length = 20;
            GainLimit = 50;
        }

        private IPlotObject plotZLMA, plotEMA;
        private VariableSeries<double> EMA, EC;
        double alpha, Gain, BestGain, Error, LeastError, OneMinusAlpha;

        ///  Create instances of objects
        protected override void Create()
		{
            // create variable objects, function objects, plot objects etc.
            // In Create() method, create plot before using it
            plotZLMA = AddPlot(new PlotAttributes("ZeroLagMA", EPlotShapes.Line, Color.Red));
            plotEMA = AddPlot(new PlotAttributes("EMA", EPlotShapes.Line, Color.Green));

            EMA = new VariableSeries<double>(this);
            EC = new VariableSeries<double>(this);
        }

        /// Setting fixed properties of objects, e.g. MALength
        protected override void StartCalc()
		{
            alpha = 2.0 / ((double)Length + 1.0);
            OneMinusAlpha = 1.0 - alpha;
        }

        protected override void CalcBar()
        {
            if (Bars.CurrentBar > 1)
            {
                EMA.Value = alpha * Bars.Close[0] + OneMinusAlpha * EMA[1];
                LeastError = 1000000.0;

                for (int gainIdx = -GainLimit; gainIdx <= GainLimit; gainIdx++)
                {

                    Gain = (double)gainIdx / 10.0;
                    EC.Value = alpha * (EMA[0] + Gain * (Bars.Close[0] - EC[1])) + OneMinusAlpha * EC[1];
                    Error = Bars.Close[0] - EC[0];

                    if (Error < 0.0) Error = -Error;

                    if (Error < LeastError)
                    {
                        LeastError = Error;
                        BestGain = Gain;
                    }

                }

                EC.Value = alpha * (EMA[0] + BestGain * (Bars.Close[0] - EC[1])) + OneMinusAlpha * EC[1];

            }
            else
            {
                EMA.Value = Bars.Close[0];
                EC.Value = Bars.Close[0];
            }

            plotZLMA.Set(EC[0]);
            plotEMA.Set(EMA[0]);
        }
    }
}