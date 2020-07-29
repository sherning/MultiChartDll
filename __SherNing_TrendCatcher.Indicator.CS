
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true), UpdateOnEveryTick(false)]
    public class __SherNing_TrendCatcher : IndicatorObject
    {
        //==========================================================================================
        // Name           : __SherNing_TrendCatcher
        // Description    : Trend Catcher Indicator
        // Version        : v1.0
        // Date Created   : 28 - Jul - 2020
        // Time Taken     : 
        // Remarks        : Protocol for starting and completing a project
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  
         *  ----------------------------------- Version History -----------------------------------


         *  ------------------------------------ Project Notes ------------------------------------
         *  - Plot on actual chart, calculate at end of bar
         *  - Step 1: Project Overview
         *  - Step 2: Timeframe
         *  - Step 3: Indicator Logic
         *  
         *  - Determine starting point of trend catcher
         *  - Two Entry Zones: Breakout Zone and Retracement Zone
         *  - Make use of different timeframes to determine entry zone
         *  - Or we can use the std dev of the fractal line
         *  
         *  - How do we determine the fractal line ?
         *  - Use a variable to track the trend direction
         *  
         *  ------------------------------------ Project Notes ------------------------------------
         */

        // inputs
        [Input] public string IndicatorSettings { get; set; }
        [Input] public int FractalLength { get; set; }
        [Input] public int StdDevLength { get; set; }
        [Input] public string DisplaySettings { get; set; }
        [Input] public bool DisplayPama1 { get; set; }
        [Input] public bool DisplayPama1_StdDev1 { get; set; }
        [Input] public bool DisplayPama1_StdDev2 { get; set; }
        [Input] public bool DisplayFractal1 { get; set; }
        [Input] public bool DisplayPama2 { get; set; }
        [Input] public bool DisplayPama2_StdDev1 { get; set; }
        [Input] public bool DisplayPama2_StdDev2 { get; set; }
        [Input] public bool DisplayPama3 { get; set; }
        [Input] public bool DisplayPama3_StdDev1 { get; set; }
        [Input] public bool DisplayPama3_StdDev2 { get; set; }


        public __SherNing_TrendCatcher(object _ctx) : base(_ctx)
        {
            IndicatorSettings = "Indicator Settings";
            FractalLength = 10;
            StdDevLength = 6;

            DisplaySettings = "Display Settings";
            DisplayPama1 = true;
            DisplayPama2 = true;
            DisplayPama3 = true;

            DisplayPama1_StdDev1 = false;
            DisplayPama2_StdDev1 = false;
            DisplayPama3_StdDev1 = false;

            DisplayPama1_StdDev2 = false;
            DisplayPama2_StdDev2 = false;
            DisplayPama3_StdDev2 = false;

            DisplayFractal1 = true;
        }

        // private fields
        const int NumOfPamaPlots = 3;
        const int NumOfStdDevPlots = 12;

        // main function
        TrendCatcherFunction TrendCatcher;

        // list
        List<IPlotObject> ListOfPamaPlots, ListOfPamaStdDevPlots;
        List<VariableSeries<double>> Pama, PamaStdDev, StdDevChannel;

        List<double> Pama2L, Pama3L;
        VariableSeries<Color> Pama1Color, Pama2Color, Pama3Color;

        // fractal channel, variableseries remembers the state base on bar interval
        VariableSeries<Fractal> FractalOne;
        IPlotObject PlotFractalTop, PlotFractalBottom;

        protected override void Create()
        {
            // trend catcher
            TrendCatcher = new TrendCatcherFunction(this);

            // fractal
            FractalOne = new VariableSeries<Fractal>(this);

            // list
            ListOfPamaPlots = new List<IPlotObject>();
            ListOfPamaStdDevPlots = new List<IPlotObject>();

            // replace pama1, pama2, pama3
            Pama = new List<VariableSeries<double>>();
            PamaStdDev = new List<VariableSeries<double>>();
            StdDevChannel = new List<VariableSeries<double>>();

            Pama2L = new List<double>();
            Pama3L = new List<double>();

            Pama1Color = new VariableSeries<Color>(this);
            Pama2Color = new VariableSeries<Color>(this);
            Pama3Color = new VariableSeries<Color>(this);

            // can use local variable for plot properties
            string[] pamaNames = new[] { "Pama 1", "Pama 2", "Pama 3" };
            int[] pamaWidths = new[] { 1, 3, 5 };

            // pamaplots
            for (int i = 0; i < NumOfPamaPlots; i++)
            {
                // each is plot object data stored on the heap
                ListOfPamaPlots.Add(AddPlot(new PlotAttributes(
                    pamaNames[i],
                    EPlotShapes.Line,
                    Color.White,
                    Color.Black,
                    pamaWidths[i],
                    EPlotStyle.Solid,
                    true
                    )));
            }

            string[] stdDevNames = new[]
            {
                "Pama1 1st StdDev Upper",
                "Pama1 1st StdDev Lower",
                "Pama1 2nd StdDev Upper",
                "Pama1 2nd StdDev Lower",
                "Pama2 1st StdDev Upper",
                "Pama2 1st StdDev Lower",
                "Pama2 2nd StdDev Upper",
                "Pama2 2nd StdDev Lower",
                "Pama3 1st StdDev Upper",
                "Pama3 1st StdDev Lower",
                "Pama3 2nd StdDev Upper",
                "Pama3 2nd StdDev Lower",
            };

            EPlotStyle[] stdDevStyle = new[]
            {
                EPlotStyle.Dash,
                EPlotStyle.Dash,
                EPlotStyle.Dash,
                EPlotStyle.Dash,
                EPlotStyle.DashDot,
                EPlotStyle.DashDot,
                EPlotStyle.DashDot,
                EPlotStyle.DashDot,
                EPlotStyle.Dot,
                EPlotStyle.Dot,
                EPlotStyle.Dot,
                EPlotStyle.Dot,
            };

            for (int i = 0; i < NumOfStdDevPlots; i++)
            {
                ListOfPamaStdDevPlots.Add(AddPlot(new PlotAttributes(
                    stdDevNames[i],
                    EPlotShapes.Line,
                    Color.White,
                    Color.Black,
                    1,
                    stdDevStyle[i],
                    true
                    )));
            }

            // fractal 1 upper line
            PlotFractalTop = AddPlot(new PlotAttributes(
                "Fractal 1 Top",
                EPlotShapes.Point,
                Color.Transparent,
                Color.Transparent,
                7,
                EPlotStyle.Solid,
                true
                ));

            // fractal 1 lower line
            PlotFractalBottom = AddPlot(new PlotAttributes(
               "Fractal 1 Bottom",
               EPlotShapes.Point,
               Color.Transparent,
               Color.Transparent,
               7,
               EPlotStyle.Solid,
               true
               ));

            // skip index[0] when using
            for (int i = 0; i < 4; i++)
            {
                Pama.Add(new VariableSeries<double>(this));
                PamaStdDev.Add(new VariableSeries<double>(this));
            }

            // num of std dev plots
            for (int i = 0; i < NumOfStdDevPlots; i++)
                StdDevChannel.Add(new VariableSeries<double>(this));
        }
        protected override void StartCalc()
        {
            TrendCatcher._Length = 6;
            TrendCatcher._Smooth = 10;
            TrendCatcher._StdDevLength = StdDevLength;
            TrendCatcher._PriceType = 1;
        }
        protected override void CalcBar()
        {
            // invoke trend catcher function
            TrendCatcher.Call();

            // start calc of indicator is called before function
            if (Bars.CurrentBar == 1)
            {
                // assign reference to function reference
                Pama[1] = TrendCatcher.Pama1;
                Pama[2] = TrendCatcher.Pama2;
                Pama[3] = TrendCatcher.Pama3;

                // assign std dev reference from function
                PamaStdDev[1] = TrendCatcher.PamaStdDev1;
                PamaStdDev[2] = TrendCatcher.PamaStdDev2;
                PamaStdDev[3] = TrendCatcher.PamaStdDev3;
            }

            // fractal one logic
            if (Bars.CurrentBar > 1)
            {
                // add distinct pama 2 and 3 values to list
                Pama2DistinctValues();
                Pama3DistinctValues();

                // check for null exception error 
                //CalcFractalChannel();

                // set plot values on chart
                PlotPamaValues();
               // PlotFractalChannel();
            }
        }

        #region Indicator Logic
        private void CalcFractalChannel()
        {
            double close = Bars.CloseValue;
            double pamaValue = Pama[1][0];

            // start of indicator calculation
            if (FractalOne[0].Direction == 0)
            {
                FractalOne[0].Top = CalcPivotHigh();
                FractalOne[0].Bottom = CalcPivotLow();
            }

            // current bar close above top line, start of uptrend
            if (close > FractalOne[0].Top)
            {
                // put all the first bar logic here
                double startValue = CalcPivotLow();

                // update fractal properties
                FractalOne[0].Direction = 1;
                FractalOne[0].Bottom = startValue;
                FractalOne[0].StartingValue = startValue;
                FractalOne[0].StartingPamaValue = pamaValue;
                FractalOne[0].IsPrevBarStart = true;

                // nothing else to update
                return;
            }

            // current bar close below bottom line, start of downtrend
            else if (close < FractalOne[0].Bottom)
            {
                // put all the first bar logic here
                double startValue = CalcPivotHigh();

                // update fractal properties
                FractalOne[0].Direction = -1;
                FractalOne[0].Top = startValue;
                FractalOne[0].StartingValue = startValue;
                FractalOne[0].StartingPamaValue = pamaValue;
                FractalOne[0].IsPrevBarStart = true;

                return;
            }

            // uptrend, 2nd bar onwards
            if (FractalOne[0].Direction == 1)
            {
                double pama = 0;

                // logic for 2nd bar of trend
                if (FractalOne[0].IsPrevBarStart)
                {
                    double pamaStart = FractalOne[0].StartingPamaValue;

                    // use start value instead of current value
                    if (pamaValue > pamaStart)
                    {
                        // difference btw current and prev pama value
                        pama = pamaStart - pamaValue;
                    }

                    // update current to new pama value
                    FractalOne[0].PamaValue = Math.Max(pamaStart, pamaValue);

                    // update bottom line, non retracing logic
                    FractalOne[0].Bottom = pamaStart + pama;

                    // not the first bar of the new trend any more
                    FractalOne[0].IsPrevBarStart = false;
                }

                // logic for 3rd bar onwards
                else
                {
                    // prev stored pama value
                    double pamaStart = FractalOne[0].PamaValue;

                    // use start value instead of current value
                    if (pamaValue > pamaStart)
                    {
                        // difference btw current and prev pama value
                        pama = pamaStart - pamaValue;
                    }

                    // update current to new pama value
                    FractalOne[0].PamaValue = Math.Max(pamaStart, pamaValue);

                    // update bottom line
                    FractalOne[0].Bottom = pamaStart + pama;
                }

                // update the top
                FractalOne[0].Top = Math.Max(FractalOne[0].Top, Bars.HighValue);

                return;
            }

            // downtrend logic, 2nd bar onwards
            else if (FractalOne[0].Direction == -1)
            {
                double pama = 0;

                // logic for 2nd bar of trend
                if (FractalOne[0].IsPrevBarStart)
                {
                    double pamaStart = FractalOne[0].StartingPamaValue;

                    // use start value instead of current value
                    if (pamaValue < pamaStart)
                    {
                        // difference btw current and prev pama value
                        pama = Math.Abs(pamaStart - pamaValue);
                    }

                    // update current to new pama value
                    FractalOne[0].PamaValue = Math.Min(pamaStart, pamaValue);

                    // update bottom line, non retracing logic
                    FractalOne[0].Top = pamaStart - pama;

                    // not the first bar of the new trend any more
                    FractalOne[0].IsPrevBarStart = false;
                }

                // logic for 3rd bar onwards
                else
                {
                    // prev stored pama value
                    double pamaStart = FractalOne[0].PamaValue;

                    // use start value instead of current value
                    if (pamaValue < pamaStart)
                    {
                        // difference btw current and prev pama value
                        pama = Math.Abs(pamaStart - pamaValue);
                    }

                    // update current to new pama value
                    FractalOne[0].PamaValue = Math.Min(pamaStart, pamaValue);

                    // update bottom line
                    FractalOne[0].Top = pamaStart - pama;
                }

                // update the bottom
                FractalOne[0].Bottom = Math.Min(FractalOne[0].Bottom, Bars.LowValue);

                return;
            }
        }

        private double CalcPivotHigh()
        {
            double high = 0;

            for (int i = 0; i < FractalLength; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
        }
        private double CalcPivotLow()
        {
            double low = double.MaxValue;

            for (int i = 0; i < FractalLength; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
        }
        private void Pama2DistinctValues()
        {
            if (Pama[2][0] == Pama[2][1]) return;

            // newest value in front
            Pama2L.Insert(0, Pama[2][0]);

            // only keep last 3 values
            if (Pama2L.Count == 4) Pama2L.Remove(3);
        }
        private void Pama3DistinctValues()
        {
            if (Pama[3][0] == Pama[3][1]) return;

            // newest value in front
            Pama3L.Insert(0, Pama[3][0]);

            // only keep last 3 values
            if (Pama3L.Count == 4) Pama3L.Remove(3);
        }
        private Color Pama1Direction()
        {
            // set plot colors for pama1
            if (Pama[1][0] > Pama[1][1])
                Pama1Color.Value = Color.LimeGreen;
            else
                Pama1Color.Value = Color.Red;

            return Pama1Color[0];
        }
        private Color Pama2Direction()
        {
            // no change in price
            if (Pama[2][0] == Pama[2][1]) return Pama2Color[0];

            // pama direction
            if (Pama2L.Count >= 2)
            {
                if (Pama2L[0] > Pama2L[1])
                    Pama2Color.Value = Color.LimeGreen;

                else if (Pama2L[0] < Pama2L[1])
                    Pama2Color.Value = Color.Red;
            }

            return Pama2Color[0];
        }
        private Color Pama3Direction()
        {
            // no change in price
            if (Pama[3][0] == Pama[3][1]) return Pama3Color[0];

            // check pama direction
            if (Pama3L.Count >= 2)
            {
                if (Pama3L[0] > Pama3L[1])
                    Pama3Color.Value = Color.LimeGreen;

                else if (Pama3L[0] < Pama3L[1])
                    Pama3Color.Value = Color.Red;
            }

            return Pama3Color[0];
        }

        #endregion

        #region Indicator Plots
        private Color Darken(Color color)
        {
            // check color
            if (color == Color.LimeGreen) return Color.Cyan;
            else if (color == Color.Red) return Color.Magenta;

            // return default color
            else return Color.Black;
        }
        private void PlotFractalChannel()
        {
            if (FractalOne[0].Direction == 1)
            {
                PlotFractalBottom.Set(FractalOne[0].Bottom);
            }

            else if (FractalOne[0].Direction == -1)
            {
                PlotFractalTop.Set(FractalOne[0].Top);
            }
        }
        private void PlotPamaValues()
        {
            // display pama1, 2, 3
            if (DisplayPama1)
                ListOfPamaPlots[0].Set(Pama[1].Value, Pama1Direction());

            if (DisplayPama2)
                ListOfPamaPlots[1].Set(Pama[2].Value, Pama2Direction());

            if (DisplayPama3)
                ListOfPamaPlots[2].Set(Pama[3].Value, Pama3Direction());

            // display pama1 1st std dev
            if (DisplayPama1 && DisplayPama1_StdDev1)
            {
                ListOfPamaStdDevPlots[0].Set(Pama[1].Value + PamaStdDev[1].Value, Darken(Pama1Color[0]));
                ListOfPamaStdDevPlots[1].Set(Pama[1].Value - PamaStdDev[1].Value, Darken(Pama1Color[0]));
            }

            // display pama1 2nd std dev
            if (DisplayPama1 && DisplayPama1_StdDev2)
            {
                ListOfPamaStdDevPlots[2].Set(Pama[1].Value + 2 * PamaStdDev[1].Value, Darken(Pama1Color[0]));
                ListOfPamaStdDevPlots[3].Set(Pama[1].Value - 2 * PamaStdDev[1].Value, Darken(Pama1Color[0]));
            }

            // display pama2 1st std dev
            if (DisplayPama2 && DisplayPama2_StdDev1)
            {
                ListOfPamaStdDevPlots[4].Set(Pama[2].Value + PamaStdDev[2].Value, Darken(Pama2Color[0]));
                ListOfPamaStdDevPlots[5].Set(Pama[2].Value - PamaStdDev[2].Value, Darken(Pama2Color[0]));
            }

            // display pama2 2nd std dev
            if (DisplayPama2 && DisplayPama2_StdDev2)
            {
                ListOfPamaStdDevPlots[6].Set(Pama[2].Value + 2 * PamaStdDev[2].Value, Darken(Pama2Color[0]));
                ListOfPamaStdDevPlots[7].Set(Pama[2].Value - 2 * PamaStdDev[2].Value, Darken(Pama2Color[0]));
            }

            // display pama3 1st std dev
            if (DisplayPama3 && DisplayPama3_StdDev1)
            {
                ListOfPamaStdDevPlots[8].Set(Pama[3].Value + PamaStdDev[3].Value, Darken(Pama3Color[0]));
                ListOfPamaStdDevPlots[9].Set(Pama[3].Value - PamaStdDev[3].Value, Darken(Pama3Color[0]));
            }

            // display pama3 2nd std dev
            if (DisplayPama3 && DisplayPama3_StdDev2)
            {
                ListOfPamaStdDevPlots[10].Set(Pama[3].Value + 2 * PamaStdDev[3].Value, Darken(Pama3Color[0]));
                ListOfPamaStdDevPlots[11].Set(Pama[3].Value - 2 * PamaStdDev[3].Value, Darken(Pama3Color[0]));
            }
        }

        #endregion

        #region Fractal Class
        class Fractal
        {
            // Fractal Channel Container Class

            /// <summary>
            /// Is previous bar the start of a new trend ?
            /// </summary>
            public bool IsPrevBarStart { get; set; }
            /// <summary>
            /// Starting value of new trend
            /// </summary>
            public double StartingValue { get; set; }
            /// <summary>
            /// Upper fractal value
            /// </summary>
            public double Top { get; set; }
            /// <summary>
            /// Lower fractal value
            /// </summary>
            public double Bottom { get; set; }
            /// <summary>
            /// 0: Flat, 1: Long, -1: Short
            /// </summary>
            public int Direction { get; set; }
            /// <summary>
            /// Current pama value
            /// </summary>
            public double PamaValue { get; set; }
            /// <summary>
            /// Starting pama value
            /// </summary>
            public double StartingPamaValue { get; set; }
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

namespace PowerLanguage.Function
{
    //==========================================================================================
    // Name           : Price Action Moving Average Strategy Function
    // Description    : Strategy function, customized for Pama MTF Strategy
    // Version        : v1.0
    // Date Created   : 24 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  ----------------------------------- Version History -----------------------------------
    
     *  ------------------------------------ Project Notes ------------------------------------
     *  - Chart Selection 5, 15, 60 min only
     *  - Input to select higher time frame
     *  
     *  ------------------------------------ Project Notes ------------------------------------
     */
    public sealed class TrendCatcherFunction : FunctionSimple<bool>
    {
        // class properties. 
        public int _Length { get; set; }
        /// <summary>
        /// Standard Deviation look back length
        /// </summary>
        public int _StdDevLength { get; set; }
        /// <summary>
        /// Number of bars back to determine pivot high or low
        /// </summary>
        public int _PivotLength { get; set; }
        /// <summary>
        /// Amount of smoothness
        /// </summary>
        public int _Smooth { get; set; }
        /// <summary>
        /// 1: Close Price, 2: Range, 3: HLOC, 4: OC Average
        /// </summary>
        public int _PriceType { get; set; }
        /// <summary>
        /// Returns the total time frames
        /// </summary>
        public int _Timeframes { get { return Timeframes; } }

        // return pama series reference
        public VariableSeries<double> Pama1 { get; private set; }
        public VariableSeries<double> Pama2 { get; private set; }
        public VariableSeries<double> Pama3 { get; private set; }

        // return std dev series reference
        public VariableSeries<double> PamaStdDev1 { get; private set; }
        public VariableSeries<double> PamaStdDev2 { get; private set; }
        public VariableSeries<double> PamaStdDev3 { get; private set; }

        // constructor
        public TrendCatcherFunction(CStudyControl _master) : base(_master)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //

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
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private List<StandardDeviationCalculatorManual> ListOfStdDevs;
        private List<MovingAveragePivotsManual> ListOfMaPivots;
        private List<VariableSeries<double>> ListOfStdDevsValues, ListOfMaPivotsValues;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfMaPivotsValues = new List<VariableSeries<double>>();
            ListOfStdDevsValues = new List<VariableSeries<double>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfStdDevs = new List<StandardDeviationCalculatorManual>();
            ListOfMaPivots = new List<MovingAveragePivotsManual>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));
                ListOfStdDevs.Add(new StandardDeviationCalculatorManual(this));
                ListOfMaPivots.Add(new MovingAveragePivotsManual(this));
                ListOfStdDevsValues.Add(new VariableSeries<double>(this));
            }

            // Create one extra plot
            // for each MA pivot there is an upper bound and lower bound value
            for (int i = 0; i <= 2 * Timeframes; i++)
                ListOfMaPivotsValues.Add(new VariableSeries<double>(this));
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;
            if (_StdDevLength < 1) _StdDevLength = 1;
            if (_PivotLength < 3) _PivotLength = 3;

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
                ListOfMaPivots[i]._Length = _PivotLength;
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

                // set MA pivot values
                SetMovingAveragePivots(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetMovingAveragePivots(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // cache pama value
                double pamaValue = ListOfPamaValues[timeframe].Value;

                // add data
                ListOfMaPivots[timeframe].Add(pamaValue);
                ListOfMaPivots[timeframe].AddHigh(Highs[timeframe]);
                ListOfMaPivots[timeframe].AddLow(Lows[timeframe]);

                // cache data
                double pivotHigh = ListOfMaPivots[timeframe].PivotHighs;
                double pivotLow = ListOfMaPivots[timeframe].PivotLows;

                // assign data
                ListOfMaPivotsValues[(timeframe * 2) + 1].Value = pivotHigh;
                ListOfMaPivotsValues[(timeframe * 2) + 2].Value = pivotLow;
            }
        }
        private void SetStdDevValues(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass pama data 
                ListOfStdDevs[timeframe].Add(ListOfPamaValues[timeframe].Value);

                // cache 
                double stdDev = ListOfStdDevs[timeframe].Calculate();

                // save to std dev list
                ListOfStdDevsValues[timeframe].Value = stdDev;
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

            // set by default to disable calculation
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            // set and assign function return values
            if (chartTF == 5 || chartTF == 15 || chartTF == 60)
            {
                // set property reference 
                switch (chartTF)
                {
                    // 5, 60, 240 min only
                    case 5:
                        IsTimeFrameSelected[0] = 1;
                        Pama1 = ListOfPamaValues[0];
                        PamaStdDev1 = ListOfStdDevsValues[0];

                        IsTimeFrameSelected[5] = 1;
                        Pama2 = ListOfPamaValues[5];
                        PamaStdDev2 = ListOfStdDevsValues[5];

                        IsTimeFrameSelected[7] = 1;
                        Pama3 = ListOfPamaValues[7];
                        PamaStdDev3 = ListOfStdDevsValues[7];
                        break;

                    // 15, 60, 240 min only
                    case 15:
                        IsTimeFrameSelected[2] = 1;
                        Pama1 = ListOfPamaValues[2];
                        PamaStdDev1 = ListOfStdDevsValues[2];

                        IsTimeFrameSelected[5] = 1;
                        Pama2 = ListOfPamaValues[5];
                        PamaStdDev2 = ListOfStdDevsValues[5];

                        IsTimeFrameSelected[7] = 1;
                        Pama3 = ListOfPamaValues[7];
                        PamaStdDev3 = ListOfStdDevsValues[7];
                        break;

                    // 60, 240 and daily only
                    case 60:
                        IsTimeFrameSelected[5] = 1;
                        Pama1 = ListOfPamaValues[5];
                        PamaStdDev1 = ListOfStdDevsValues[5];

                        IsTimeFrameSelected[7] = 1;
                        Pama2 = ListOfPamaValues[7];
                        PamaStdDev2 = ListOfStdDevsValues[7];

                        IsTimeFrameSelected[8] = 1;
                        Pama3 = ListOfPamaValues[8];
                        PamaStdDev3 = ListOfStdDevsValues[8];
                        break;
                }
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
