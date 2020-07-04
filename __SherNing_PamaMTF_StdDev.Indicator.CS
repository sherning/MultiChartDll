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
    // Reference      : PamaMTF_StdDev
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  
     *  ----------------------------------- Version History -----------------------------------
     

     *  ---------------------------------------- Notes ----------------------------------------
     *  ---------------------------------------- Notes ----------------------------------------
     */

    [SameAsSymbol(false), UpdateOnEveryTick(false)]
    public class __SherNing_PamaMTF_StdDev : IndicatorObject
    {
        [Input] public int Length { get; set; }
        [Input] public int Smooth { get; set; }
        [Input] public int StdDevLength { get; set; }
        [Input] public int PriceType { get; set; }
        [Input] public bool DisplayTF1 { get; set; }
        [Input] public bool DisplayTF2 { get; set; }
        [Input] public bool DisplayTF3 { get; set; }

        public __SherNing_PamaMTF_StdDev(object _ctx) : base(_ctx)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //

            Length = 6;
            Smooth = 10;
            StdDevLength = 6;
            PriceType = 1;

            // default to true
            DisplayTF1 = true;
            DisplayTF2 = true;
            DisplayTF3 = true;
        }

        // fields
        PamaMTF_StdDev PamaStdDevFunc;
        IPlotObject StdDevPlot1, StdDevPlot2, StdDevPlot3;

        protected override void Create()
        {
            PamaStdDevFunc = new PamaMTF_StdDev(this);

            StdDevPlot1 = AddPlot(
                new PlotAttributes("TF 1", EPlotShapes.Line, Color.Yellow, Color.Black, 1, EPlotStyle.Solid, true));

            StdDevPlot2 = AddPlot(
               new PlotAttributes("TF 2", EPlotShapes.Line, Color.Cyan, Color.Black, 1, EPlotStyle.Dash, true));

            StdDevPlot3 = AddPlot(
               new PlotAttributes("TF 3", EPlotShapes.Line, Color.LightSkyBlue, Color.Black, 1, EPlotStyle.DashDot, true));
        }
        protected override void StartCalc()
        {
            PamaStdDevFunc._Length = Length;
            PamaStdDevFunc._Smooth = Smooth;
            PamaStdDevFunc._StdDevLength = StdDevLength;
            PamaStdDevFunc._PriceType = PriceType;
        }
        protected override void CalcBar()
        {
            PamaStdDevFunc.Call();

            if (Bars.Status == EBarState.Close)
            {
                if (DisplayTF1) StdDevPlot1.Set(PamaStdDevFunc.StdDev1[0]);
                if (DisplayTF2) StdDevPlot2.Set(PamaStdDevFunc.StdDev2[0]);
                if (DisplayTF3) StdDevPlot3.Set(PamaStdDevFunc.StdDev3[0]);
            }
        }
    }
}

namespace PowerLanguage.Function
{
    #region Price Action Multitime Frame Standard Deviation
    //==========================================================================================
    // Name           : Price Action Multitime Frame Standard Deviation
    // Description    : Returns the Std Dev for each timeframe
    // Version        : v1.0
    // Date Created   : 04 - Jul - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  - Price Charts 5 min, 15 min, 60 min only
     *  
     *  ----------------------------------- Version History -----------------------------------
     */

    public sealed class PamaMTF_StdDev : FunctionSimple<bool> // v10
    {
        // properties. 
        public int _Length { get; set; }
        public int _StdDevLength { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // get accessors
        public VariableSeries<double> StdDev1 { get; private set; }
        public VariableSeries<double> StdDev2 { get; private set; }
        public VariableSeries<double> StdDev3 { get; private set; }

        public PamaMTF_StdDev(CStudyControl _master) : base(_master)
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
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues, ListOfStdDevValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private List<StandardDeviationCalculatorManual> ListOfStdDevs;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            // list of calculated values
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfStdDevValues = new List<VariableSeries<double>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfStdDevs = new List<StandardDeviationCalculatorManual>();
            ListOfHmas = new List<HullMovingAverage>();

            // std dev return properties
            StdDev1 = new VariableSeries<double>(this);
            StdDev2 = new VariableSeries<double>(this);
            StdDev3 = new VariableSeries<double>(this);

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));
                ListOfStdDevs.Add(new StandardDeviationCalculatorManual(this));
                ListOfStdDevValues.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;
            if (_StdDevLength < 1) _StdDevLength = 1;

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
                ListOfStdDevs[i]._Length = _StdDevLength;
                ListOfStdDevs[i]._PopulationStdDev = true;
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

                // calculate std dev values
                SetStdDevValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetStdDevValues(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass pama data 
                ListOfStdDevs[timeframe].Add(ListOfPamaValues[timeframe].Value);

                // store in list
                ListOfStdDevValues[timeframe].Value = ListOfStdDevs[timeframe].Calculate();
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
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe] = Closes[timeframe];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe]
                            = Highs[timeframe] - Lows[timeframe];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe]
                            = (Highs[timeframe]
                            + Lows[timeframe]
                            + Opens[timeframe]
                            + Closes[timeframe]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe]
                            = (Opens[timeframe]
                            + Closes[timeframe]) * 0.5;
                        break;
                }
            }
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
                    StdDev1 = ListOfStdDevValues[0];

                    IsTimeFrameSelected[5] = 1;
                    StdDev2 = ListOfStdDevValues[5];

                    IsTimeFrameSelected[7] = 1;
                    StdDev3 = ListOfStdDevValues[7];
                    break;

                case 15:
                    IsTimeFrameSelected[2] = 1;
                    StdDev1 = ListOfStdDevValues[2];

                    IsTimeFrameSelected[5] = 1;
                    StdDev2 = ListOfStdDevValues[5];

                    IsTimeFrameSelected[7] = 1;
                    StdDev3 = ListOfStdDevValues[7];
                    break;

                case 60:
                    IsTimeFrameSelected[5] = 1;
                    StdDev1 = ListOfStdDevValues[5];

                    IsTimeFrameSelected[7] = 1;
                    StdDev2 = ListOfStdDevValues[7];

                    IsTimeFrameSelected[8] = 1;
                    StdDev3 = ListOfStdDevValues[8];
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

    #endregion

}
