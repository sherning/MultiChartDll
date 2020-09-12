using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class __SherNing_MomentumRider : IndicatorObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Momentum Rider
        // Requirements : 
        // Type         : Indicator / Function
        // Version      : v1.0
        // Date         : 12 - Sep - 2020
        // Time Taken   : 
        // Remarks      : 
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      :
        // Client       : Sher Ning
        // Contact      : 
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.1 - Requirements
         *  a. Line Thickness determine momentum
         *  b. Line Direction determine momentum direction
         *  c. Solely based on price
         *  d. Test momentum with PAL and Price Range
         *  e. Upon Completion, covert to function
         *  
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  1
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Inputs
        [Input] public string IndicatorSettings { get; set; }
        [Input] public int Length { get; set; }
        [Input] public int AvgLength { get; set; }
        [Input] public int Smooth { get; set; }
        [Input] public bool ShowCrossOver { get; set; }
        public __SherNing_MomentumRider(object _ctx) : base(_ctx)
        {
            IndicatorSettings = "Indicator Settings";
            Length = 4;
            AvgLength = 55;
            Smooth = 10;
            ShowCrossOver = true;
        }

        #endregion

        #region References
        IPlotObject Plot, Plot2;
        PriceActionMovingAverage Pamaf;
        VariableSeries<int> PamaDir, TrendStrength, TrendMomentum;
        VariableSeries<double> Pama, PamaMomentum;
        protected override void Create()
        {
            // pama function - single timeframe
            Pamaf = new PriceActionMovingAverage(this);

            // reference variables
            Pama = new VariableSeries<double>(this);
            PamaDir = new VariableSeries<int>(this);
            PamaMomentum = new VariableSeries<double>(this);

            TrendStrength = new VariableSeries<int>(this);
            TrendMomentum = new VariableSeries<int>(this);

            Plot = AddPlot(new PlotAttributes(
                "Momentum", EPlotShapes.Line, Color.White, Color.White, 1, EPlotStyle.Solid, true));

            Plot2 = AddPlot(new PlotAttributes(
                "MovingAverage", EPlotShapes.Line, Color.White, Color.White, 1, EPlotStyle.Dash, true));
        }
        #endregion

        #region Fields
        Color PamaColor;
        bool LongCrossOver, ShortCrossOver;
        protected override void StartCalc()
        {
            Pamaf._Price = Bars.Close;
            Pamaf._Length = Length;
            Pamaf._Smooth = Smooth;

            PamaColor = Color.White;
            LongCrossOver = ShortCrossOver = false;
        }
        #endregion

        protected override void CalcBar()
        {
            // call pamaf only once per bar
            if (Bars.Status == EBarState.Close)
                Pama.Value = Pamaf[0];

            // ensure sufficient data before plotting
            if (Bars.CurrentBar < 500) return;

            if (PositivePivot())  PamaDir.Value = 1;
            if (NegativePivot()) PamaDir.Value = -1;

            // calculate pama momentum
            PamaMomentum.Value = Math.Abs(Pama[0] - Pama[1]);

            CalculateMomentum();

            // long direction
            if (PamaDir[0] > 0)
            {
                TrendStrength.Value++;
                PamaColor = Color.Lime;
            }
            
            // short direction
            if (PamaDir[0] < 0)
            {
                TrendStrength.Value--;
                PamaColor = Color.Red;
            }

            double avgStrength = Average(AvgLength);

            IArrowObject arrow;
            if (TrendStrength[1] > avgStrength && LongCrossOver == false && ShowCrossOver)
            {
                arrow = DrwArrow.Create(new ChartPoint(Bars.Time[1], avgStrength), false, true);
                arrow.Color = Color.FromArgb(128, 128, 255);
                LongCrossOver = true;
                ShortCrossOver = false;
            }
            else if (TrendStrength[1] < avgStrength && ShortCrossOver == false && ShowCrossOver)
            {
                // arrow down set properties to direction to true
                arrow = DrwArrow.Create(new ChartPoint(Bars.Time[1], avgStrength), true, true);
                arrow.Color = Color.DeepPink;
                LongCrossOver = false;
                ShortCrossOver = true;
            }

            Plot.Set(1, TrendStrength[1], PamaColor, TrendMomentum[1]);
            Plot2.Set(1, avgStrength);
        }

        #region Methods
        private double Average(int length)
        {
            double sum = 0;
            for (int i = 0; i < length; i++)
                sum += TrendStrength[i];

            return sum / length;
        }
        private void CalculateMomentum()
        {
            // default
            TrendMomentum.Value = 3;

            // 5 power settings. Nested to fulfil all conditions
            if (PamaMomentum[0] > PamaMomentum[1])
            {
                TrendMomentum.Value = 6;

                if (PamaMomentum[0] > PamaMomentum[2])
                {
                    TrendMomentum.Value = 9;

                    if (PamaMomentum[0] > PamaMomentum[3])
                    {
                        TrendMomentum.Value = 12;

                        if (PamaMomentum[0] > PamaMomentum[4])
                        {
                            TrendMomentum.Value = 15;

                            if (PamaMomentum[0] > PamaMomentum[5])
                            {
                                TrendMomentum.Value = 18;
                            }
                        }
                    }
                }
            }
        }
        private bool PositivePivot()
        {
            // [0] > [1] < [2]
            if (Pama[0] > Pama[1] && Pama[1] < Pama[2]) return true;
            return false;
        }

        private bool NegativePivot()
        {
            // [0] < [1] > [2]
            if (Pama[0] < Pama[1] && Pama[1] > Pama[2]) return true;
            return false;
        }
        #endregion

        #region Print
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false || Environment.Optimizing) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
}