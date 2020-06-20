/*
_HMA_List_f_Test1	20191214
* Test function _HMA_List_f
*/

using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using System.Collections.Generic;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class _HMA_List_f_Test1 : IndicatorObject
    {
        [Input]
        public int HMA_Length { get; set; }

        public _HMA_List_f_Test1(object _ctx) : base(_ctx)
        {
            // define input default values
            HMA_Length = 9;
        }

        private IPlotObject plot_Test1;
        double ValueToPlot;
        List<double> PriceList;
        _HMA_List_f mHMAList;
        private int HMA_LenM1;

        protected override void Create()
		{
			plot_Test1 = AddPlot(new PlotAttributes("Test1", EPlotShapes.Line, Color.Red));

            PriceList = new List<double>();
            mHMAList = new _HMA_List_f(this);
        }

        bool onOpenTick;
        int currentBarPT;
        int lastPriceIdx;

        /// Setting fixed properties of objects, e.g. MALength
        protected override void StartCalc()
		{
            // Assume HMA_Length >= 1
            HMA_LenM1 = HMA_Length - 1;
            mHMAList.Length = HMA_Length;
            mHMAList.PriceList = PriceList;

            PriceList.Clear();

            onOpenTick = false;
            currentBarPT = 0;
            lastPriceIdx = 0;
            ValueToPlot = -1;
        }

        protected override void CalcBar()
        {
            onOpenTick = (Bars.CurrentBar > currentBarPT);
            currentBarPT = Bars.CurrentBar;

            // Critical: this algorithm is tested closed bar only, not intrabar tick by tick
            if (Bars.Status == EBarState.Close)
            {
                if (onOpenTick)
                {
                    if (PriceList.Count > HMA_LenM1)
                    {
                        PriceList.RemoveAt(0);
                    }
                    PriceList.Add(Bars.Close[0]);
                    lastPriceIdx = PriceList.Count - 1;
                }
                PriceList[lastPriceIdx] = Bars.Close[0];

                if (PriceList.Count == HMA_Length)
                {
                    ValueToPlot = mHMAList.Value;
                }

                // Plot only when the list has HMA_Length elements
                if (ValueToPlot > 0)
                {
                    plot_Test1.Set(ValueToPlot);
                }
            }
        }
    }
}
