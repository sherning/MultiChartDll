using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [UpdateOnEveryTick(false)]
    public class __SherNing_PamaMTF_Spread : IndicatorObject
    {
        //==========================================================================================
        // Name           : Price Action Moving Average Spread
        // Description    : Returns the Std Dev of two different pama spreads
        // Version        : v1.0
        // Date Created   : 09 - Jul - 2020
        // Time Taken     : 
        // Remarks        :
        // Reference      : 
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  v10
         *  - Fast time frame must be smaller than slow time frame
         *  ----------------------------------- Version History -----------------------------------
         */

        // properties
        [Input] public int Length { get; set; }
        [Input] public int StdDevLength { get; set; }
        [Input] public int Smooth { get; set; }
        [Input] public string FastTimeFrame { get; set; }
        [Input] public string SlowTimeFrame { get; set; }

        // fields
        private const int NumPlots = 4;
        private SherNing_PamaMTF_Spread PamaSpd;
        private List<IPlotObject> StdDevPlots;
        private IPlotObject SpreadPlot;
        private string[] PlotNames;
        private Color[] PlotColors;

        public __SherNing_PamaMTF_Spread(object _ctx) : base(_ctx)
        {
            // default input values
            Length = 6;
            StdDevLength = 6;
            Smooth = 10;
            FastTimeFrame = "5";
            SlowTimeFrame = "240";
        }
        protected override void Create()
        {
            PamaSpd = new SherNing_PamaMTF_Spread(this);

            // std dev plot properties
            StdDevPlots = new List<IPlotObject>();
            PlotNames = new string[]
            {
                "1st StdDev Upper", "1st StdDev Lower", "2nd StdDev Upper", "2nd StdDev Lower"
            };

            PlotColors = new Color[]
            {
                Color.Cyan, Color.Cyan, Color.Yellow, Color.Yellow
            };

            // 1st and 2nd std dev only
            for (int i = 0; i < NumPlots; i++)
            {
                StdDevPlots.Add(AddPlot(new PlotAttributes(
                    // name,             shape,          color,     bg color, width, style,        lastmarker
                    PlotNames[i], EPlotShapes.Line, PlotColors[i], Color.Black, 1, EPlotStyle.Solid, true)));
            }

            // set color in calcbar, use default color green
            SpreadPlot = AddPlot(new PlotAttributes(
                "Spread", EPlotShapes.Histogram, Color.White, Color.Black, 5, EPlotStyle.Solid, true));
        }
        protected override void StartCalc()
        {
            // set function properties
            PamaSpd._Length = Length;
            PamaSpd._StdDevLength = StdDevLength;
            PamaSpd._Smooth = Smooth;
            PamaSpd._SlowTF = SlowTimeFrame;
            PamaSpd._FastTF = FastTimeFrame;
        }
        protected override void CalcBar()
        {
            PamaSpd.Call();

            // check if update on everytick == false, no need to use EBarState == close ? true.

            // the difference between the two chosen time frame / std of the difference
            double spread = PamaSpd._Spreads[0] / PamaSpd._StdDev[0];
            Color spreadColor;

            // Set spread color
            if (spread > 0)
                spreadColor = Color.LimeGreen;
            else
                spreadColor = Color.Red;

            // set spread plot
            SpreadPlot.Set(spread, spreadColor);

            // Set up the lines.
            StdDevPlots[0].Set(50);
            StdDevPlots[1].Set(-50);
            StdDevPlots[2].Set(100);
            StdDevPlots[3].Set(-100);
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

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

namespace PowerLanguage.Function
{
    #region Pama MTF Spread Function
    public sealed class SherNing_PamaMTF_Spread : FunctionSimple<bool> // v00
    {
        //==========================================================================================
        // Name           : Pama MTF Spread Function
        // Description    : Takes two input and calculate the std dev on the pama of the spread
        // Version        : v1.0
        // Date Created   : 09 - Jul - 2020
        // Time Taken     : 
        // Remarks        :
        // Reference      : 
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  v10
         *  - Fast time frame must be smaller than slow time frame
         *  ----------------------------------- Version History -----------------------------------
         */

        // properties
        public int _Length { get; set; }
        public int _StdDevLength { get; set; }
        public int _Smooth { get; set; }
        public string _FastTF { get; set; }
        public string _SlowTF { get; set; }

        // return properties
        public int _Timeframes { get { return Timeframes; } }
        public VariableSeries<double> _StdDev { get { return StdDevValue; } }
        public VariableSeries<double> _Spreads { get { return PamaSpd; } }

        public SherNing_PamaMTF_Spread(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;
        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> PamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        // track which pama values are being used
        private int FastIdx, SlowIdx;
        private VariableSeries<double> PamaSpd, StdDevValue;

        // std variables
        private StandardDeviationCalculator StdDev;

        protected override void Create()
        {
            // list of calculated values
            PamaValues = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfHmas = new List<HullMovingAverage>();

            for (int i = 0; i < Timeframes; i++)
            {
                PamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));
            }

            // track spread
            PamaSpd = new VariableSeries<double>(this);
            StdDev = new StandardDeviationCalculator(this);
            StdDevValue = new VariableSeries<double>(this);
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;

            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                ListOfHmas[i]._Length = _Length;
                ClosingTicks[i]._TimeFrame = i;
            }

            // set function properties
            StdDev._Length = _StdDevLength;
            StdDev._PopulationStdDev = true;
            StdDev._Price = PamaSpd;

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = ClosingTicks[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            // end of bar calculate the spread of two chosen timeframe
            CalculateSpreads();

            return false;
        }

        #region Helper Methods
        private void CalculateSpreads()
        {
            // track the differences in value
            PamaSpd.Value = PamaValues[FastIdx][0] - PamaValues[SlowIdx][0];

            // reference std dev return function value to return call
            StdDevValue.Value = StdDev.Value;
        }
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - PamaValues[timeframe][1] > 0)
                {
                    PamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - PamaValues[timeframe][1]))
                    + (1.0 - Alpha) * PamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - PamaValues[timeframe][1] < 0)
                {
                    PamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - PamaValues[timeframe][1]))
                   + (1.0 - Alpha) * PamaValues[timeframe][1];
                }
                else
                {
                    PamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - PamaValues[timeframe][1]))
                   + (1.0 - Alpha) * PamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            if (ListOfClosingTicks[timeframe][0] == true)
                Prices[timeframe] = Closes[timeframe];
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // check if fast and slow TF are valid
            FastIdx = TimeFrameIndexConversion(_FastTF);
            SlowIdx = TimeFrameIndexConversion(_SlowTF);

            // check if fast index < slow index
            if (FastIdx >= SlowIdx) return;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            // set time frame for calculation
            IsTimeFrameSelected[FastIdx] = 1;
            IsTimeFrameSelected[SlowIdx] = 1;
        }
        private int TimeFrameIndexConversion(string timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
            switch (timeframe.ToLower())
            {
                case "5":
                    return 0;

                case "10":
                    return 1;

                case "15":
                    return 2;

                case "20":
                    return 3;

                case "30":
                    return 4;

                case "60":
                    return 5;

                case "120":
                    return 6;

                case "240":
                    return 7;

                case "day":
                    return 8;

                case "week":
                    return 9;

                case "month":
                    return 10;
            }

            // if none of the above, return the index of current resolution
            int resoSize = (int)Bars.Info.Resolution.Size;
            return TimeFrameIndexConversion(resoSize.ToString());
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

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

    #endregion
}
