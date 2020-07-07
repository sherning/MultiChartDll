using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    //==========================================================================================
    // Name           : Price Action Multitime Frame Standard Deviation
    // Description    : Returns the Std Dev on a separate subchart
    // Version        : v1.0
    // Date Created   : 04 - Jul - 2020
    // Time Taken     : 
    // Remarks        :
    // Reference      : SherNingPamaMTF_Funnel
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  
     *  ----------------------------------- Version History -----------------------------------
     */
    [SameAsSymbol(true)]
    public class __SherNing_PamaMTF_Funnel : IndicatorObject // v10
    {
        // inputs
        [Input] public int Length { get; set; }
        [Input] public int Smooth { get; set; }
        [Input] public bool DisplayTF1 { get; set; }
        [Input] public bool DisplayTF2 { get; set; }
        [Input] public bool DisplayTF3 { get; set; }

        // fields
        private List<IPlotObject> ListOfPlots;
        private string[] PlotNames;
        private EPlotStyle[] PlotStyles;
        private int[] PlotWidths;
        private Color[] PlotColors;
        public __SherNing_PamaMTF_Funnel(object _ctx) : base(_ctx)
        {
            Length = 6;
            Smooth = 10;

            DisplayTF1 = false;
            DisplayTF2 = false;
            DisplayTF3 = true;
        }

        SherNing_PamaMTF_Funnel Funnel;
        protected override void Create()
        {
            // main function
            Funnel = new SherNing_PamaMTF_Funnel(this);

            // List Of Plots
            ListOfPlots = new List<IPlotObject>();
            PlotNames = new string[6]
            {
                "TF1_Upper", "TF1_Lower",
                "TF2_Upper", "TF2_Lower",
                "TF3_Upper", "TF3_Lower"
            };

            PlotStyles = new EPlotStyle[6]
            {
                // TF 1
                EPlotStyle.Solid, EPlotStyle.Solid,

                // TF 2
                EPlotStyle.Dash, EPlotStyle.Dash,

                // TF 3
                EPlotStyle.Solid, EPlotStyle.Solid,
            };

            PlotWidths = new int[6]
            {
                // TF 1
                1,1,

                // TF 2
                0,0,

                // TF 3
                3,3
            };

            PlotColors = new Color[6]
            {
                // TF 1
                Color.Yellow, Color.Yellow,
                
                // TF 2
                Color.Cyan, Color.Cyan,

                // TF 3
                Color.Magenta, Color.Magenta,
            };

            // 3 time frames, upper and lower
            for (int i = 0; i < 6; i++)
            {
                ListOfPlots.Add(AddPlot(new PlotAttributes(
                        PlotNames[i],
                        EPlotShapes.Line,
                        PlotColors[i],
                        Color.Black,
                        PlotWidths[i],
                        PlotStyles[i],
                        true)));
            }

        }
        protected override void StartCalc()
        {
            // assign inputs 
            Funnel._Length = Length;
            Funnel._Smooth = 10;
        }
        protected override void CalcBar()
        {
            // indicator logic 
            Funnel.Call();

            if (DisplayTF1)
            {

               
            }

            if (DisplayTF2)
            {

                
            }

            if (DisplayTF3)
            {

            }
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
    //==========================================================================================
    // Name           : Price Action Multitime Frame Standard Deviation
    // Description    : Returns the Std Dev on a separate subchart
    // Version        : v1.1
    // Date Created   : 04 - Jul - 2020
    // Time Taken     : 
    // Remarks        :
    // Reference      : 
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v00
     *  - Original File
     *  v10
     *  - Create Funnel Function
     *  v11
     *  - Bug fixes, refactorization
     *  ----------------------------------- Version History -----------------------------------
     */
    public sealed class SherNing_PamaMTF_Funnel : FunctionSimple<bool> // v11
    {
        // properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        public SherNing_PamaMTF_Funnel(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private const int GreenColor = 1;
        private const int RedColor = -1;

        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        // list of arrays to track turning points
        private List<double[]> ListOfPivots;

        protected override void Create()
        {
            // list of calculated values
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfHmas = new List<HullMovingAverage>();

            // list of arrays
            ListOfPivots = new List<double[]>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));

                // only 3 points needed to determine pivot
                ListOfPivots.Add(new double[3]);
            }
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

            // initialize list of pivots in event of recalculation
            for (int i = 0; i < Timeframes; i++)
            {
                for (int j = 0; j < 3; j++)
                    ListOfPivots[i][j] = 0;
            }

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

                // set pivot direction
                SetPamaDirection(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaDirection(int timeframe)
        {
            // if time frame closing tick is true
            if (ListOfClosingTicks[timeframe][0])
            {
                // shift cells forward
                for (int i = 2; i >= 1; i--)
                    ListOfPivots[timeframe][i] = ListOfPivots[timeframe][i - 1];

                // add newest data to index 0
                ListOfPivots[timeframe][0] = ListOfPamaValues[timeframe].Value;

                // peak [0] < [1] > [2]
                if (ListOfPivots[timeframe][0] < ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] > ListOfPivots[timeframe][2])
                {
                }
                else // bottom [0] > [1] < [2]
                if (ListOfPivots[timeframe][0] > ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] < ListOfPivots[timeframe][2])
                {
                }
              
                // *variable series will use prev value if there is no update.
            }
            else
            {
                // there is no pivot change 
            }
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
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
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

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    IsTimeFrameSelected[0] = 1;
                   

                    IsTimeFrameSelected[5] = 1;
                   

                    IsTimeFrameSelected[7] = 1;
                   

                    break;

                case 15:
                    IsTimeFrameSelected[2] = 1;
                  
                    IsTimeFrameSelected[5] = 1;
                   
                    IsTimeFrameSelected[7] = 1;
                   
                    break;

                case 60:
                    IsTimeFrameSelected[5] = 1;
                   

                    IsTimeFrameSelected[7] = 1;
                    
                    IsTimeFrameSelected[8] = 1;
                  
                    break;
            }
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
    public sealed class SherNing_PamaMTF_Funnelv10 : FunctionSimple<bool> // v10
    {
        // properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // return properties 
        public VariableSeries<int> PivotChng_TF1 { get; private set; }
        public VariableSeries<int> PivotChng_TF2 { get; private set; }
        public VariableSeries<int> PivotChng_TF3 { get; private set; }

        // track pivot high, reference bars back
        public VariableSeries<double> PivotHigh_TF1 { get; private set; }
        public VariableSeries<double> PivotHigh_TF2 { get; private set; }
        public VariableSeries<double> PivotHigh_TF3 { get; private set; }

        public VariableSeries<int> PivotHighBarAgo_TF1 { get; private set; }
        public VariableSeries<int> PivotHighBarAgo_TF2 { get; private set; }
        public VariableSeries<int> PivotHighBarAgo_TF3 { get; private set; }

        public VariableSeries<double> PivotLow_TF1 { get; private set; }
        public VariableSeries<double> PivotLow_TF2 { get; private set; }
        public VariableSeries<double> PivotLow_TF3 { get; private set; }

        public VariableSeries<int> PivotLowBarAgo_TF1 { get; private set; }
        public VariableSeries<int> PivotLowBarAgo_TF2 { get; private set; }
        public VariableSeries<int> PivotLowBarAgo_TF3 { get; private set; }

        public SherNing_PamaMTF_Funnelv10 (CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private const int GreenColor = 1;
        private const int RedColor = -1;

        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<VariableSeries<double>> ListOfPivotHighs, ListOfPivotLows;
        private List<VariableSeries<int>> ListOfPivotValues, ListOfBarNumber;
        private List<VariableSeries<int>> ListOfHighsBarsAgo, ListOfLowsBarsAgo;
        private List<VariableSeries<int>> ListOfPivotChg;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        // list of arrays to track turning points
        private List<double[]> ListOfPivots;

        protected override void Create()
        {
            // list of calculated values
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfPivotValues = new List<VariableSeries<int>>();
            ListOfPivotChg = new List<VariableSeries<int>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfHmas = new List<HullMovingAverage>();

            // list of arrays
            ListOfPivots = new List<double[]>();
            ListOfPivotHighs = new List<VariableSeries<double>>();
            ListOfPivotLows = new List<VariableSeries<double>>();
            ListOfBarNumber = new List<VariableSeries<int>>();
            ListOfHighsBarsAgo = new List<VariableSeries<int>>();
            ListOfLowsBarsAgo = new List<VariableSeries<int>>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));

                // only 3 points needed to determine pivot
                ListOfPivots.Add(new double[3]);
                ListOfPivotValues.Add(new VariableSeries<int>(this));
                ListOfPivotChg.Add(new VariableSeries<int>(this));
                ListOfPivotHighs.Add(new VariableSeries<double>(this));
                ListOfPivotLows.Add(new VariableSeries<double>(this));
                ListOfBarNumber.Add(new VariableSeries<int>(this));
                ListOfHighsBarsAgo.Add(new VariableSeries<int>(this));
                ListOfLowsBarsAgo.Add(new VariableSeries<int>(this));
            }
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

            // initialize list of pivots in event of recalculation
            for (int i = 0; i < Timeframes; i++)
            {
                for (int j = 0; j < 3; j++)
                    ListOfPivots[i][j] = 0;
            }

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

                // set pivot direction
                SetPamaDirection(timeframe);

                // track actual pivot
                TrackPivotLocation(timeframe);

                // track bar ago for pivot highs and lows
                TrackBarNumber(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void TrackBarNumber(int timeframe)
        {
            // bottom turning point, red to green
            if (ListOfPivotChg[timeframe][0] == 1)
            {
                // list of bar number [0], in case it was updated current bar
                ListOfLowsBarsAgo[timeframe].Value =
                    Bars.CurrentBar - ListOfBarNumber[timeframe][0];
            }
            else // peak turning point, green to red
            if (ListOfPivotChg[timeframe][0] == -1)
            {
                ListOfHighsBarsAgo[timeframe].Value =
                    Bars.CurrentBar - ListOfBarNumber[timeframe][0];
            }
        }
        private void TrackPivotLocation(int timeframe)
        {
            // if green track bar's highest high
            if (ListOfPivotValues[timeframe].Value == 1)
            {
                // reset to max value
                ListOfPivotLows[timeframe].Value = double.MaxValue;

                // requires if statement to track bar number
                if (Bars.HighValue > ListOfPivotHighs[timeframe][1])
                {
                    // update current high
                    ListOfPivotHighs[timeframe].Value = Bars.HighValue;

                    // track bar number
                    ListOfBarNumber[timeframe].Value = Bars.CurrentBar;
                }

                // variable series will follow prev cache value
            }
            // if red track bar's lowest low
            else if (ListOfPivotValues[timeframe].Value == -1)
            {
                // reset to min value
                ListOfPivotHighs[timeframe].Value = double.MinValue;

                // compare current with prev low
                if (Bars.LowValue < ListOfPivotLows[timeframe][1])
                {
                    // update low value
                    ListOfPivotLows[timeframe].Value = Bars.LowValue;

                    // track bar number
                    ListOfBarNumber[timeframe].Value = Bars.CurrentBar;
                }

                // bar number is forward moving, no need to reset
            }
        }
        private void SetPamaDirection(int timeframe)
        {
            // if time frame closing tick is true
            if (ListOfClosingTicks[timeframe][0])
            {
                // shift cells forward
                for (int i = 2; i >= 1; i--)
                    ListOfPivots[timeframe][i] = ListOfPivots[timeframe][i - 1];

                // add newest data to index 0
                ListOfPivots[timeframe][0] = ListOfPamaValues[timeframe].Value;

                // peak [0] < [1] > [2]
                if (ListOfPivots[timeframe][0] < ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] > ListOfPivots[timeframe][2])
                {
                    ListOfPivotValues[timeframe].Value = RedColor;
                    ListOfPivotChg[timeframe].Value = RedColor;
                }
                else // bottom [0] > [1] < [2]
                if (ListOfPivots[timeframe][0] > ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] < ListOfPivots[timeframe][2])
                {
                    ListOfPivotValues[timeframe].Value = GreenColor;
                    ListOfPivotChg[timeframe].Value = GreenColor;
                }

                // *variable series will use prev value if there is no update.
            }
            else
            {
                // there is no pivot change 
                ListOfPivotChg[timeframe].Value = 0;
            }
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
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
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

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    IsTimeFrameSelected[0] = 1;
                    PivotChng_TF1 = ListOfPivotChg[0];
                    PivotHigh_TF1 = ListOfPivotHighs[0];
                    PivotLow_TF1 = ListOfPivotLows[0];
                    PivotHighBarAgo_TF1 = ListOfHighsBarsAgo[0];
                    PivotLowBarAgo_TF1 = ListOfLowsBarsAgo[0];

                    IsTimeFrameSelected[5] = 1;
                    PivotChng_TF2 = ListOfPivotChg[5];
                    PivotHigh_TF2 = ListOfPivotHighs[5];
                    PivotLow_TF2 = ListOfPivotLows[5];
                    PivotHighBarAgo_TF2 = ListOfHighsBarsAgo[5];
                    PivotLowBarAgo_TF2 = ListOfLowsBarsAgo[5];

                    IsTimeFrameSelected[7] = 1;
                    PivotChng_TF3 = ListOfPivotChg[7];
                    PivotHigh_TF3 = ListOfPivotHighs[7];
                    PivotLow_TF3 = ListOfPivotLows[7];
                    PivotHighBarAgo_TF3 = ListOfHighsBarsAgo[7];
                    PivotLowBarAgo_TF3 = ListOfLowsBarsAgo[7];

                    break;

                case 15:
                    IsTimeFrameSelected[2] = 1;
                    PivotChng_TF1 = ListOfPivotChg[2];
                    PivotHigh_TF1 = ListOfPivotHighs[2];
                    PivotLow_TF1 = ListOfPivotLows[2];
                    PivotHighBarAgo_TF1 = ListOfHighsBarsAgo[2];
                    PivotLowBarAgo_TF1 = ListOfLowsBarsAgo[2];

                    IsTimeFrameSelected[5] = 1;
                    PivotChng_TF2 = ListOfPivotChg[5];
                    PivotHigh_TF2 = ListOfPivotHighs[5];
                    PivotLow_TF2 = ListOfPivotLows[5];
                    PivotHighBarAgo_TF2 = ListOfHighsBarsAgo[5];
                    PivotLowBarAgo_TF2 = ListOfLowsBarsAgo[5];

                    IsTimeFrameSelected[7] = 1;
                    PivotChng_TF3 = ListOfPivotChg[7];
                    PivotHigh_TF3 = ListOfPivotHighs[7];
                    PivotLow_TF3 = ListOfPivotLows[7];
                    PivotHighBarAgo_TF3 = ListOfHighsBarsAgo[7];
                    PivotLowBarAgo_TF3 = ListOfLowsBarsAgo[7];

                    break;

                case 60:
                    IsTimeFrameSelected[5] = 1;
                    PivotChng_TF1 = ListOfPivotChg[5];
                    PivotHigh_TF1 = ListOfPivotHighs[5];
                    PivotLow_TF1 = ListOfPivotLows[5];
                    PivotHighBarAgo_TF1 = ListOfHighsBarsAgo[5];
                    PivotLowBarAgo_TF1 = ListOfLowsBarsAgo[5];

                    IsTimeFrameSelected[7] = 1;
                    PivotChng_TF2 = ListOfPivotChg[7];
                    PivotHigh_TF2 = ListOfPivotHighs[7];
                    PivotLow_TF2 = ListOfPivotLows[7];
                    PivotHighBarAgo_TF2 = ListOfHighsBarsAgo[7];
                    PivotLowBarAgo_TF2 = ListOfLowsBarsAgo[7];

                    IsTimeFrameSelected[8] = 1;
                    PivotChng_TF3 = ListOfPivotChg[8];
                    PivotHigh_TF3 = ListOfPivotHighs[8];
                    PivotLow_TF3 = ListOfPivotLows[8];
                    PivotHighBarAgo_TF3 = ListOfHighsBarsAgo[8];
                    PivotLowBarAgo_TF3 = ListOfLowsBarsAgo[8];

                    break;
            }
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
    public sealed class SherNing_PamaMTF_Funnelv00 : FunctionSimple<bool> // v00
    {
        // properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        public SherNing_PamaMTF_Funnelv00 (CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private const int GreenColor = 1;
        private const int RedColor = -1;

        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        // list of arrays to track turning points
        private List<double[]> ListOfPivots;

        protected override void Create()
        {
            // list of calculated values
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfHmas = new List<HullMovingAverage>();

            // list of arrays
            ListOfPivots = new List<double[]>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));

                // only 3 points needed to determine pivot
                ListOfPivots.Add(new double[3]);
            }
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

            // initialize list of pivots in event of recalculation
            for (int i = 0; i < Timeframes; i++)
            {
                for (int j = 0; j < 3; j++)
                    ListOfPivots[i][j] = 0;
            }

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

                // set pivot direction
                SetPamaDirection(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaDirection(int timeframe)
        {
            // if time frame closing tick is true
            if (ListOfClosingTicks[timeframe][0])
            {
                // shift cells forward
                for (int i = 2; i >= 1; i--)
                    ListOfPivots[timeframe][i] = ListOfPivots[timeframe][i - 1];

                // add newest data to index 0
                ListOfPivots[timeframe][0] = ListOfPamaValues[timeframe].Value;

                // peak [0] < [1] > [2]
                if (ListOfPivots[timeframe][0] < ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] > ListOfPivots[timeframe][2])
                {
                }
                else // bottom [0] > [1] < [2]
                if (ListOfPivots[timeframe][0] > ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] < ListOfPivots[timeframe][2])
                {
                }
              
                // *variable series will use prev value if there is no update.
            }
            else
            {
                // there is no pivot change 
            }
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
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
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

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    IsTimeFrameSelected[0] = 1;
                   

                    IsTimeFrameSelected[5] = 1;
                   

                    IsTimeFrameSelected[7] = 1;
                   

                    break;

                case 15:
                    IsTimeFrameSelected[2] = 1;
                  
                    IsTimeFrameSelected[5] = 1;
                   
                    IsTimeFrameSelected[7] = 1;
                   
                    break;

                case 60:
                    IsTimeFrameSelected[5] = 1;
                   

                    IsTimeFrameSelected[7] = 1;
                    
                    IsTimeFrameSelected[8] = 1;
                  
                    break;
            }
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

}