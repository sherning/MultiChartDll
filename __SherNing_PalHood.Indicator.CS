using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    //==============================================================================
    // Name           : PAL Under the Hood
    // Description    : Calculates the PAMA for a given length
    // Version        : v.1.0.0
    // Date Created   : 20 - May - 2020
    // Time Taken     : 
    // Remarks        :
    //==============================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==============================================================================

    /* ------------------------------- Version 1.0.0 -------------------------------
     * Investigate why harrison code can plot one bar back.
     * The executing class name has to match the script name.
     */
    public class __SherNing_PalHood : IndicatorObject
    {
        #region Indicator Inputs
        // Inputs for the PAL function to work 
        [Input] public string HMA_Function { get; set; }
        [Input] public int HMA_Length { get; set; }
        [Input] public int Smoothness { get; set; }
        [Input] public int Price_Input { get; set; }

        [Input] public string Trend_Function { get; set; }
        [Input] public int Price_Trend_On { get; set; }

        [Input] public string Display_Inputs { get; set; }
        [Input] public int HMA_X01_Switch { get; set; }
        [Input] public int HMA_X04_Switch { get; set; }
        [Input] public int HMA_X16_Switch { get; set; }
        [Input] public int HMA_X64_Switch { get; set; }

        [Input] public string Upper_Active_Color_Name { get; set; }
        [Input] public string Lower_Active_Color_Name { get; set; }

        [Input] public int StdDev_Length { get; set; }
        [Input] public int StdDev_DataType { get; set; }

        [Input] public string StandardDeviationSettings { get; set; }
        [Input] public int StdDev_X01_Switch { get; set; }
        [Input] public int StdDev_X04_Switch { get; set; }
        [Input] public int StdDev_X16_Switch { get; set; }
        [Input] public int StdDev_X64_Switch { get; set; }

        [Input] public double NumDevs1Value { get; set; }
        [Input] public double NumDevs2Value { get; set; }
        [Input] public double NumDevs3Value { get; set; }
        [Input] public string NumDevs1ColorName { get; set; }
        [Input] public string NumDevs2ColorName { get; set; }
        [Input] public string NumDevs3ColorName { get; set; }

        [Input] public string PriceActionLineSettings { get; set; }
        [Input] public int PivotDuration { get; set; }
        [Input] public int PAC_StepUp_Or_ConnectingLine { get; set; }
        [Input] public Color PAC_Top_Color { get; set; }
        [Input] public Color PAC_Bottom_Color { get; set; }
        [Input] public bool ShowMarketBias { get; set; }
        [Input] public int PAC_X01_Switch { get; set; }
        [Input] public int PAC_X04_Switch { get; set; }
        [Input] public int PAC_X16_Switch { get; set; }
        [Input] public int PAC_X64_Switch { get; set; }
        [Input] public string End_Inputs { get; set; }
        #endregion

        // debug mode to print messages, use variable instead of property to make swift switch
        private bool DeBugMode = false;

        public __SherNing_PalHood(object _ctx) : base(_ctx)
        {
            // define input default values

            HMA_Function = "HMA Settings";
            HMA_Length = 6;
            Smoothness = 10;

            // Price_Input: 1-close only; 2-open && close; 3- HLOC 
            Price_Input = 1;

            // Inputs for Trend Function to Work 
            Trend_Function = "Trend Settings";
            Price_Trend_On = 0;

            Display_Inputs = "Display Inputs";
            // on / off switch: 1 to plot, 0 or negative value to turn display off
            // can have multiple ones on
            HMA_X01_Switch = 1;
            HMA_X04_Switch = 0;
            HMA_X16_Switch = 1;
            HMA_X64_Switch = 0;

            Upper_Active_Color_Name = "LawnGreen";
            Lower_Active_Color_Name = "Crimson";

            StdDev_Length = 20;
            // StdDev_DataType 1 for population, 2 for sample
            StdDev_DataType = 1;

            // on / off switch for Std Dev: 1 to plot, 0 or negative value to turn display off
            // To avoid too many plots on charts (and in program), just plot StdDev's for one TF only
            // Set one of them to 1, all the others are set to 0 or -1
            // The last one switch that is set to 1 is used;
            StandardDeviationSettings = "Standard Deviation Settings";
            StdDev_X01_Switch = 0;
            StdDev_X04_Switch = 0;
            StdDev_X16_Switch = 0;
            StdDev_X64_Switch = 0;

            // These input also server as switch to plot standard deviation, both direction negative and positive.
            // Set to 0 if not to plot.
            NumDevs1Value = 1.0;
            NumDevs2Value = 2.0;
            NumDevs3Value = 3.0;

            NumDevs1ColorName = "Yellow";
            NumDevs2ColorName = "Magenta";
            NumDevs3ColorName = "Cyan";

            // PAC_StepUp_Or_ConnectingLine = 1 for StepUp, 2 for ConnectingLine
            PriceActionLineSettings = "Price Action Line Settings";
            PivotDuration = 4;
            PAC_StepUp_Or_ConnectingLine = 1;
            PAC_Top_Color = Color.Gray;
            PAC_Bottom_Color = Color.White;

            ShowMarketBias = true;

            PAC_X01_Switch = 1;
            PAC_X04_Switch = 0;
            PAC_X16_Switch = 0;
            PAC_X64_Switch = 0;

            End_Inputs = "End";
        }

        // Number of Time Frames
        private const int numOfCalcTFs = 4;

        // Number of StdDev plots
        private const int numOfStdDev = 6;

        // int numbers for each Upper_Active_Color_Name and Lower_Active_Color_Name
        private const int Upper_Active_ClrNum = 1;
        private const int Lower_Active_ClrNum = 2;

        private List<VariableSeries<double>> vHMAs = new List<VariableSeries<double>>();
        private List<VariableObject<double>> vHMA1s = new List<VariableObject<double>>();
        private List<VariableObject<int>> vActiveClrNum = new List<VariableObject<int>>();
        private List<VariableObject<double>> vStDevs = new List<VariableObject<double>>();

        private List<VariableSeries<double>> vPvtHighPrices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtHighPrices1 = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtHighPrices2 = new List<VariableSeries<double>>();
        private List<VariableSeries<DateTime>> vPvtHighTimes = new List<VariableSeries<DateTime>>();

        private List<VariableSeries<double>> vPvtLowPrices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtLowPrices1 = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtLowPrices2 = new List<VariableSeries<double>>();
        private List<VariableSeries<DateTime>> vPvtLowTimes = new List<VariableSeries<DateTime>>();

        private VariableObject<int> vActiveClrNum_5;

        private List<VariableSeries<double>> StdDevBands = new List<VariableSeries<double>>();

        private List<IPlotObject> HMA_Plots = new List<IPlotObject>();
        private List<IPlotObject> PAC_Top_Plots = new List<IPlotObject>();
        private List<IPlotObject> PAC_Bottom_Plots = new List<IPlotObject>();

        private List<IPlotObject> StdDevPlots = new List<IPlotObject>();
        private VariableObject<int> Value40, PAL_Width;

        private string[] HMA_PlotNames;
        private string[] StdDevPlotNames;
        private string[] PAC_Top_PlotNames;
        private string[] PAC_Bottom_PlotNames;
        private int[] thePlotWidth;
        private EPlotStyle[] thePlotstyles;
        private EPlotStyle[] StdDevPlotStyles;
        private int[] thePlotDisplays;
        private int[] StdDevPlotDisplays = new int[6];
        private int[] PAC_Displays;
        private Color[] ActiveColorFrActiveClrNum;
        private Color[] StdDevPlotColors;

        // Create MarketBias Color Change
        private string[] MarketBias = new string[numOfCalcTFs];
        private Color[] PAC_UpperColor = new Color[numOfCalcTFs];
        private Color[] PAC_LowerColor = new Color[numOfCalcTFs];

        // PAC turning points
        private List<IPlotObject> PAC_UpperTurn = new List<IPlotObject>();
        private List<IPlotObject> PAC_LowerTurn = new List<IPlotObject>();

        private _SN_Trend_Tool_Price_Input trend_Tool_Price_Input1;
        private _SherNing_PalHood_f mHMA_MTF_Smooth;

        private VariableSeries<double> vHMA2U4StdDev, StdDevValue2U;
        private int BarResolution;

        protected override void Create()
        {
            // Toolbar 
            //HMA_Length = 6;
            //Smoothness = 10;

            // define Plot Names
            //BarResolution = (int)Bars.Info.Resolution.Duration().TotalMinutes;

            //switch (BarResolution)
            //{
            //    default:
            //    case 5:
            //        HMA_PlotNames = new string[] { "5 Min", "60 min", "240 min", "Daily" };
            //        break;

            //    case 15:
            //        HMA_PlotNames = new string[] { "15 Min", "60 min", "240 min", "Daily" };
            //        break;

            //    case 20:
            //        HMA_PlotNames = new string[] { "20 Min", "60 min", "240 min", "Daily" };
            //        break;

            //    case 30:
            //        HMA_PlotNames = new string[] { "30 Min", "60 min", "240 min", "Daily" };
            //        break;

            //    case 60:
            //        HMA_PlotNames = new string[] { "60 Min", "240 min", "Daily", "Weekly" };
            //        break;

            //    case 240:
            //        HMA_PlotNames = new string[] { "240 min", "Daily", "Weekly", "Monthly" };
            //        break;
            //}

            HMA_PlotNames = new string[] { "HMA_X01", "HMA_X04", "HMA_X16", "HMA_X64" };
            PAC_Top_PlotNames = new string[] { "PAC_X01_Top", "PAC_X04_Top", "PAC_X16_Top", "PAC_X64_Top" };
            PAC_Bottom_PlotNames = new string[] { "PAC_X01_Bot", "PAC_X04_Bot", "PAC_X16_Bot", "PAC_X64_Bot" };

            string[] PAC_UpperPivotPlotNames = new string[]
            { "PAC_X01_Top_Turn", "PAC_X04_Top_Turn", "PAC_X16_Top_Turn", "PAC_X64_Top_Turn" };

            string[] PAC_LowerPivotPlotNames = new string[]
            { "PAC_X01_Bot_Turn", "PAC_X04_Bot_Turn", "PAC_X16_Bot_Turn", "PAC_X64_Bot_Turn" };


            // ----------------------------- 7 time intervals ----------------------------- //
            thePlotstyles = new EPlotStyle[] { EPlotStyle.Solid, EPlotStyle.Solid, EPlotStyle.Solid, EPlotStyle.DashDot };

            thePlotWidth = new int[] { 1, 3, 5, 1 };
            // ----------------------------- 7 time intervals ----------------------------- //

            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfCalcTFs; j++)
            {
                vHMAs.Add(new VariableSeries<double>(this));
                vHMA1s.Add(new VariableObject<double>(this));
                vActiveClrNum.Add(new VariableObject<int>(this));
                vStDevs.Add(new VariableObject<double>(this));

                vPvtHighPrices.Add(new VariableSeries<double>(this));
                vPvtHighPrices1.Add(new VariableSeries<double>(this));
                vPvtHighPrices2.Add(new VariableSeries<double>(this));
                vPvtHighTimes.Add(new VariableSeries<DateTime>(this));

                vPvtLowPrices.Add(new VariableSeries<double>(this));
                vPvtLowPrices1.Add(new VariableSeries<double>(this));
                vPvtLowPrices2.Add(new VariableSeries<double>(this));
                vPvtLowTimes.Add(new VariableSeries<DateTime>(this));

                HMA_Plots.Add(AddPlot(new PlotAttributes(HMA_PlotNames[j], EPlotShapes.Line, Color.Yellow,
                    Color.Black, thePlotWidth[j], thePlotstyles[j], true)));

                PAC_Top_Plots.Add(AddPlot(new PlotAttributes(PAC_Bottom_PlotNames[j], EPlotShapes.Line, PAC_Top_Color,
                    Color.Black, thePlotWidth[j], thePlotstyles[j], true)));
                PAC_Bottom_Plots.Add(AddPlot(new PlotAttributes(PAC_Bottom_PlotNames[j], EPlotShapes.Line, PAC_Bottom_Color,
                    Color.Black, thePlotWidth[j], thePlotstyles[j], true)));

                PAC_UpperTurn.Add
                (
                    AddPlot
                    (
                    new PlotAttributes(
                        PAC_UpperPivotPlotNames[j],
                        EPlotShapes.Point,
                        Color.Transparent,
                        Color.Black,
                        1, 1, true)
                    )
                );

                PAC_LowerTurn.Add
                (
                    AddPlot
                    (
                    new PlotAttributes(
                        PAC_LowerPivotPlotNames[j],
                        EPlotShapes.Point,
                        Color.Transparent,
                        Color.Black,
                        1, 1, true)
                    )
                );
            }


            Value40 = new VariableObject<int>(this);
            PAL_Width = new VariableObject<int>(this);

            trend_Tool_Price_Input1 = new _SN_Trend_Tool_Price_Input(this);
            mHMA_MTF_Smooth = new _SherNing_PalHood_f(this);

            // StdDev plots
            StdDevPlotNames = new string[] { "+StdDev1", "-StdDev1",
                                "+StdDev2", "-StdDev2", "+StdDev3", "-StdDev3" };

            vHMA2U4StdDev = new VariableSeries<double>(this);
            StdDevValue2U = new VariableSeries<double>(this);

            StdDevPlotStyles = new EPlotStyle[] {EPlotStyle.Solid, EPlotStyle.Solid,
                    EPlotStyle.Dot, EPlotStyle.Dot, EPlotStyle.DashDot,EPlotStyle.DashDot };
            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfStdDev; j++)
            {
                StdDevBands.Add(new VariableSeries<double>(this));

                StdDevPlots.Add(AddPlot(new PlotAttributes(StdDevPlotNames[j], EPlotShapes.Line, Color.Yellow,
                    Color.Black, 0, StdDevPlotStyles[j], true)));
            }
        }

        private int stdDevTFidx;

        protected override void StartCalc()
        {
            // clear screen

            //CreateToolStrip();
            ActiveColorFrActiveClrNum = new Color[]
               { Color.White, Color.FromName(Upper_Active_Color_Name), Color.FromName(Lower_Active_Color_Name)};

            // display settings
            thePlotDisplays = new int[] { HMA_X01_Switch, HMA_X04_Switch, HMA_X16_Switch, HMA_X64_Switch };

            PAC_Displays = new int[] { PAC_X01_Switch, PAC_X04_Switch, PAC_X16_Switch, PAC_X64_Switch };

            StdDevPlotColors = new Color[] { Color.FromName(NumDevs1ColorName), Color.FromName(NumDevs1ColorName),
                                    Color.FromName(NumDevs2ColorName), Color.FromName(NumDevs2ColorName),
                                    Color.FromName(NumDevs3ColorName), Color.FromName(NumDevs3ColorName)};

            //3 initialize to 0 until proven; otherwise changing NumDevs1Value input to 0 will still plot
            for (int j = 0; j < numOfStdDev; j++)
            {
                StdDevPlotDisplays[j] = 0;
            }

            if (NumDevs1Value > 0.0)
            {
                StdDevPlotDisplays[0] = 1;
                StdDevPlotDisplays[1] = 1;
            }
            if (NumDevs2Value > 0.0)
            {
                StdDevPlotDisplays[2] = 1;
                StdDevPlotDisplays[3] = 1;
            }
            if (NumDevs3Value > 0.0)
            {
                StdDevPlotDisplays[4] = 1;
                StdDevPlotDisplays[5] = 1;
            }

            mHMA_MTF_Smooth.HMA_Length = this.HMA_Length;
            mHMA_MTF_Smooth.Smoothness = this.Smoothness;
            mHMA_MTF_Smooth.Price_Input = this.Price_Input;
            mHMA_MTF_Smooth.StdDev_Length = this.StdDev_Length;
            mHMA_MTF_Smooth.StdDev_DataType = this.StdDev_DataType;
            mHMA_MTF_Smooth.PivotDuration = this.PivotDuration;

            trend_Tool_Price_Input1.Price_Input = this.Price_Input;
        }


        protected override void CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                mHMA_MTF_Smooth.Call();

                vHMAs[0].Value = mHMA_MTF_Smooth.HMA_X01[0];
                vHMAs[1].Value = mHMA_MTF_Smooth.HMA_X04[0];
                vHMAs[2].Value = mHMA_MTF_Smooth.HMA_X16[0];
                vHMAs[3].Value = mHMA_MTF_Smooth.HMA_X64[0];

                vHMA1s[0].Value = mHMA_MTF_Smooth.HMA1_X01[0];
                vHMA1s[1].Value = mHMA_MTF_Smooth.HMA1_X04[0];
                vHMA1s[2].Value = mHMA_MTF_Smooth.HMA1_X16[0];
                vHMA1s[3].Value = mHMA_MTF_Smooth.HMA1_X64[0];

                vStDevs[0].Value = mHMA_MTF_Smooth.StdDev_X01[0];
                vStDevs[1].Value = mHMA_MTF_Smooth.StdDev_X04[0];
                vStDevs[2].Value = mHMA_MTF_Smooth.StdDev_X16[0];
                vStDevs[3].Value = mHMA_MTF_Smooth.StdDev_X64[0];

                vPvtHighPrices[0].Value = mHMA_MTF_Smooth.PvtHighPrice_X01[0];
                vPvtHighPrices[1].Value = mHMA_MTF_Smooth.PvtHighPrice_X04[0];
                vPvtHighPrices[2].Value = mHMA_MTF_Smooth.PvtHighPrice_X16[0];
                vPvtHighPrices[3].Value = mHMA_MTF_Smooth.PvtHighPrice_X64[0];

                vPvtHighPrices1[0].Value = mHMA_MTF_Smooth.PvtHighPrice1_X01[0];
                vPvtHighPrices1[1].Value = mHMA_MTF_Smooth.PvtHighPrice1_X04[0];
                vPvtHighPrices1[2].Value = mHMA_MTF_Smooth.PvtHighPrice1_X16[0];
                vPvtHighPrices1[3].Value = mHMA_MTF_Smooth.PvtHighPrice1_X64[0];

                vPvtHighPrices2[0].Value = mHMA_MTF_Smooth.PvtHighPrice2_X01[0];
                vPvtHighPrices2[1].Value = mHMA_MTF_Smooth.PvtHighPrice2_X04[0];
                vPvtHighPrices2[2].Value = mHMA_MTF_Smooth.PvtHighPrice2_X16[0];
                vPvtHighPrices2[3].Value = mHMA_MTF_Smooth.PvtHighPrice2_X64[0];

                vPvtHighTimes[0].Value = mHMA_MTF_Smooth.PvtHighTime_X01[0];
                vPvtHighTimes[1].Value = mHMA_MTF_Smooth.PvtHighTime_X04[0];
                vPvtHighTimes[2].Value = mHMA_MTF_Smooth.PvtHighTime_X16[0];
                vPvtHighTimes[3].Value = mHMA_MTF_Smooth.PvtHighTime_X64[0];

                vPvtLowPrices[0].Value = mHMA_MTF_Smooth.PvtLowPrice_X01[0];
                vPvtLowPrices[1].Value = mHMA_MTF_Smooth.PvtLowPrice_X04[0];
                vPvtLowPrices[2].Value = mHMA_MTF_Smooth.PvtLowPrice_X16[0];
                vPvtLowPrices[3].Value = mHMA_MTF_Smooth.PvtLowPrice_X64[0];

                vPvtLowPrices1[0].Value = mHMA_MTF_Smooth.PvtLowPrice1_X01[0];
                vPvtLowPrices1[1].Value = mHMA_MTF_Smooth.PvtLowPrice1_X04[0];
                vPvtLowPrices1[2].Value = mHMA_MTF_Smooth.PvtLowPrice1_X16[0];
                vPvtLowPrices1[3].Value = mHMA_MTF_Smooth.PvtLowPrice1_X64[0];

                vPvtLowPrices2[0].Value = mHMA_MTF_Smooth.PvtLowPrice2_X01[0];
                vPvtLowPrices2[1].Value = mHMA_MTF_Smooth.PvtLowPrice2_X04[0];
                vPvtLowPrices2[2].Value = mHMA_MTF_Smooth.PvtLowPrice2_X16[0];
                vPvtLowPrices2[3].Value = mHMA_MTF_Smooth.PvtLowPrice2_X64[0];

                vPvtLowTimes[0].Value = mHMA_MTF_Smooth.PvtLowTime_X01[0];
                vPvtLowTimes[1].Value = mHMA_MTF_Smooth.PvtLowTime_X04[0];
                vPvtLowTimes[2].Value = mHMA_MTF_Smooth.PvtLowTime_X16[0];
                vPvtLowTimes[3].Value = mHMA_MTF_Smooth.PvtLowTime_X64[0];

                for (int idxTF = 0; idxTF < numOfCalcTFs; idxTF++)
                {
                    if (vHMAs[idxTF][0] > vHMA1s[idxTF].Value)
                        vActiveClrNum[idxTF].Value = Upper_Active_ClrNum;
                    else if (vHMAs[idxTF][0] < vHMA1s[idxTF].Value)
                        vActiveClrNum[idxTF].Value = Lower_Active_ClrNum;
                }

                // ++++++++++++++++++++++++++++++++++++++  SN_Trend_Tool function  ++++++++++++++++++++++++++++++ 
                if (Price_Trend_On == 1)
                {
                    Value40.Value = trend_Tool_Price_Input1.Value;

                    // LONG PAL && DOWN TREND 
                    if (vActiveClrNum[0].Value == Upper_Active_ClrNum && Value40[0] < 0)
                        PAL_Width.Value = 0;
                    else
                    // SHORT PAL && UP TREND 
                    if (vActiveClrNum[0].Value == Lower_Active_ClrNum && Value40[0] > 0)
                        PAL_Width.Value = 0;
                    else
                    // LONG PAL && UP TREND 
                    if (vActiveClrNum[0].Value == Upper_Active_ClrNum && Value40[0] > 0)
                        PAL_Width.Value = Value40[0];
                    else
                    // SHORT PAL && DOWN TREND 
                    if (vActiveClrNum[0].Value == Lower_Active_ClrNum && Value40[0] < 0)
                        PAL_Width.Value = Math.Abs(Value40[0]);
                }

                if (Price_Trend_On != 1)
                    PAL_Width.Value = 2;

                // ++++++++++++++++++++++++++++++++++++  end of SN_Trend_Tool function ++++++++++++++++++++++++++ 

                if (StdDev_X01_Switch == 1)
                {
                    stdDevTFidx = 0;
                    vHMA2U4StdDev.Value = vHMAs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                if (StdDev_X04_Switch == 1)
                {
                    stdDevTFidx = 1;
                    vHMA2U4StdDev.Value = vHMAs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                if (StdDev_X16_Switch == 1)
                {
                    stdDevTFidx = 2;
                    vHMA2U4StdDev.Value = vHMAs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                if (StdDev_X64_Switch == 1)
                {
                    stdDevTFidx = 3;
                    vHMA2U4StdDev.Value = vHMAs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                // +/-, +/-, +/-
                StdDevBands[0].Value = vHMA2U4StdDev[0] + NumDevs1Value * StdDevValue2U[0];
                StdDevBands[1].Value = vHMA2U4StdDev[0] - NumDevs1Value * StdDevValue2U[0];
                StdDevBands[2].Value = vHMA2U4StdDev[0] + NumDevs2Value * StdDevValue2U[0];
                StdDevBands[3].Value = vHMA2U4StdDev[0] - NumDevs2Value * StdDevValue2U[0];
                StdDevBands[4].Value = vHMA2U4StdDev[0] + NumDevs3Value * StdDevValue2U[0];
                StdDevBands[5].Value = vHMA2U4StdDev[0] - NumDevs3Value * StdDevValue2U[0];
            }

            // HMA Plots
            for (int plotIdx1 = 0; plotIdx1 < numOfCalcTFs; plotIdx1++)
            {
                if (thePlotDisplays[plotIdx1] == 1 && vHMAs[plotIdx1][1] > 0)
                {
                    HMA_Plots[plotIdx1].Set(1, vHMAs[plotIdx1][1], ActiveColorFrActiveClrNum[vActiveClrNum[plotIdx1].Value]);
                }
            }

            // StdDev Plots
            for (int plotIdx2 = 0; plotIdx2 < numOfStdDev; plotIdx2++)
            {
                if (StdDevPlotDisplays[plotIdx2] == 1 && StdDevValue2U[1] > 0)
                {
                    StdDevPlots[plotIdx2].Set(1, StdDevBands[plotIdx2][1], StdDevPlotColors[plotIdx2]);
                }
            }

            // Creat Marker to identify PAC pivots.

            // Determine market bias, 4 time frames all different color. 
            if (ShowMarketBias == true)
            {
                for (int i = 0; i < numOfCalcTFs; i++)
                {
                    // change to true after test
                    if (DeBugMode == true)
                    {
                        Output.WriteLine("PAC_HighPrices[0]: " + vPvtHighPrices[i][0] + " i = " + i);
                        Output.WriteLine("PAC_LowPrices[0]: " + vPvtLowPrices[i][0] + " i = " + i);
                        Output.WriteLine("PAC_HighPrices[1]: " + vPvtHighPrices1[i][0] + " i = " + i);
                        Output.WriteLine("PAC_LowPrices[1]: " + vPvtLowPrices1[i][0] + " i = " + i);
                    }

                    if (vPvtHighPrices[i][0] > vPvtHighPrices1[i][0] && vPvtLowPrices[i][0] > vPvtLowPrices1[i][0])
                    {
                        // both upper and lower sloping upwards. Uptrend
                        MarketBias[i] = "long";
                    }
                    else if (vPvtHighPrices[i][0] < vPvtHighPrices1[i][0] && vPvtLowPrices[i][0] < vPvtLowPrices1[i][0])
                    {
                        // both upper and lower sloping downwards. Downtrend
                        MarketBias[i] = "short";
                    }
                    else if (vPvtHighPrices[i][0] > vPvtHighPrices1[i][0] && vPvtLowPrices[i][0] < vPvtLowPrices1[i][0])
                    {
                        // upper moving up, lower moving down. Range
                        MarketBias[i] = "range";
                    }
                    else if (vPvtHighPrices[i][0] < vPvtHighPrices1[i][0] && vPvtLowPrices[i][0] > vPvtLowPrices1[i][0])
                    {
                        // consolidation.
                        MarketBias[i] = "consolidation";
                    }
                    else
                    {
                        MarketBias[i] = "undefined";
                    }

                    if (DeBugMode == true)
                    {
                        Output.WriteLine("Market Bias: " + MarketBias[i]);

                        // DONT TRY THIS. IT WILL CRASH YOUR COMPUTER.
                        //for (int j = 0; j <= 20; j++)
                        //{
                        //    if (vPvtHighPrices[i][0] != vPvtHighPrices[i][j])
                        //    {
                        //        Output.WriteLine("The last price change for PAC_High value was {0} bars ago", j - 1);
                        //        break;
                        //    }
                        //}
                    }

                }

                for (int i = 0; i < numOfCalcTFs; i++)
                {
                    switch (MarketBias[i])
                    {
                        case "long":
                            PAC_UpperColor[i] = Color.LawnGreen;
                            PAC_LowerColor[i] = Color.LawnGreen;
                            break;

                        case "short":
                            PAC_UpperColor[i] = Color.Red;
                            PAC_LowerColor[i] = Color.Red;
                            break;

                        case "range":
                            PAC_UpperColor[i] = Color.Yellow;
                            PAC_LowerColor[i] = Color.Yellow;
                            break;

                        case "consolidation":
                            PAC_UpperColor[i] = Color.Cyan;
                            PAC_LowerColor[i] = Color.Cyan;
                            break;

                        default:
                        case "undefined":
                            PAC_UpperColor[i] = Color.Green;
                            PAC_LowerColor[i] = Color.Green;
                            break;
                    }
                }
            }

            // >>> PAC Plots
            for (int plotIdx1 = 0; plotIdx1 < numOfCalcTFs; plotIdx1++)
            {
                // Top, High
                if (PAC_Displays[plotIdx1] == 1 && vPvtHighPrices[plotIdx1][0] > 0)
                {
                    if (PAC_StepUp_Or_ConnectingLine == 1)
                    {
                        PAC_Top_Plots[plotIdx1].Set(0, vPvtHighPrices[plotIdx1][0], PAC_UpperColor[plotIdx1]);
                    }
                    else //  if (PAC_StepUp_Or_ConnectingLine == 2)
                    {
                        // On PvtHighTime change
                        if (vPvtHighTimes[plotIdx1][0] > vPvtHighTimes[plotIdx1][1])
                        {
                            PAC_Top_Plots[plotIdx1].Set(0, vPvtHighPrices[plotIdx1][0], PAC_UpperColor[plotIdx1]);
                        }
                    }
                }

                // Bottom, Low
                if (PAC_Displays[plotIdx1] == 1 && vPvtLowPrices[plotIdx1][0] > 0)
                {
                    if (PAC_StepUp_Or_ConnectingLine == 1)
                    {
                        PAC_Bottom_Plots[plotIdx1].Set(0, vPvtLowPrices[plotIdx1][0], PAC_LowerColor[plotIdx1]);
                    }
                    else //  if (PAC_StepUp_Or_ConnectingLine == 2)
                    {
                        // On PvtLowTime change
                        if (vPvtLowTimes[plotIdx1][0] > vPvtLowTimes[plotIdx1][1])
                        {
                            PAC_Bottom_Plots[plotIdx1].Set(0, vPvtLowPrices[plotIdx1][0], PAC_LowerColor[plotIdx1]);
                        }
                    }
                }
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

        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            if (arg.buttons != MouseButtons.Left)
            {
                return;
            }

            if (arg.keys == Keys.Shift)
            {
                ExecControl.Recalculate();
            }
        }

    }

}

namespace PowerLanguage.Function
{
    public sealed class _SherNing_PalHood_f : FunctionSimple<double>
    {
        // Data1 only
        public _SherNing_PalHood_f (CStudyControl _master) : base(_master) { }

        public int HMA_Length { get; set; }
        public int Price_Input { get; set; }

        public int Smoothness { get; set; }
        public int StdDev_Length { get; set; }
        // StdDev_DataType 1 for population, 2 for sample
        public int StdDev_DataType { get; set; }

        public int PivotDuration { get; set; }

        // to remove SettingOriginalOrNew and TickBuffer
        private int SettingOriginalOrNew = 1;
        private int TickBuffer = 1;

        // Number of Calculation Time Frames for this instance
        // 5 min, 15min, 20 min, 30 min, 60 min, 240 min, Daily
        private const int numOfCalcTFs = 7;

        // Number of All possible Time Frames
        private const int numOfAllTFs = 11;

        // List index from 0 to 10, total 11
        private const int TF_5M = 0;
        private const int TF_10M = 1;
        private const int TF_15M = 2;
        private const int TF_20M = 3;
        private const int TF_30M = 4;
        private const int TF_60M = 5;
        private const int TF_120M = 6;
        private const int TF_240M = 7;
        private const int TF_Day = 8;
        private const int TF_Week = 9;
        private const int TF_Month = 10;

        // Smoothness fields
        double alpha, gain;

        // int numbers for each Upper_Active_Color_Name and Lower_Active_Color_Name
        private const int Upper_Active_ClrNum = 1;
        private const int Lower_Active_ClrNum = 2;

        // Sufficient to use VariableObject for vHAMs
        private List<VariableObject<double>> vHMAs = new List<VariableObject<double>>();

        private List<VariableObject<double>> vHMAs_Temp = new List<VariableObject<double>>();
        private List<VariableObject<double>> vHMA1s_Temp = new List<VariableObject<double>>();

        private List<VariableObject<double>> vHMA1s = new List<VariableObject<double>>();
        private List<VariableSeries<int>> vActiveClrNum = new List<VariableSeries<int>>();
        private List<VariableObject<double>> vStDevs = new List<VariableObject<double>>();

        private List<VariableSeries<double>> vPvtHighPrices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtHighPrices1 = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtHighPrices2 = new List<VariableSeries<double>>();
        private List<VariableObject<DateTime>> vPvtHighTimes = new List<VariableObject<DateTime>>();

        private List<VariableSeries<double>> vPvtLowPrices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtLowPrices1 = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vPvtLowPrices2 = new List<VariableSeries<double>>();
        private List<VariableObject<DateTime>> vPvtLowTimes = new List<VariableObject<DateTime>>();

        private List<List<double>> PriceList = new List<List<double>>();

        private int[] HTF_TFs;
        private int[] HTF_BarCnts;

        private List<VariableSeries<bool>> HTF_OnCloseTicks = new List<VariableSeries<bool>>();

        // >> Need to use arrays if we need price history
        private List<VariableSeries<double>> HTF_Prices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Opens = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Closes = new List<VariableSeries<double>>();

        private List<VariableSeries<double>> HTF_Highs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Lows = new List<VariableSeries<double>>();
        // Keep track of the Low
        // private List<VariableSeries<DateTime>> HTF_DateTimeAtLows = new List<VariableSeries<DateTime>>();

        private List<VariableSeries<double>> HTF_OpenCloseAvgs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_OHLCAvgs = new List<VariableSeries<double>>();
        private List<List<double>> PriceLists = new List<List<double>>();
        private List<_HMA_List_f> mHMALists = new List<_HMA_List_f>();

        private double OnePip, OneTick;
        private int vCategory, sessEnd_ELTime;
        private double pointBuffer;
        private double[] pointBuffersAll;
        private double[] pointBuffers;

        /*----------------------------------- 7 time intervals total -----------------------------------*/

        int[] dataCnts = new int[7];
        // index1 is for TF, index2 is data point number for the time frame
        // Max StdDevLen2U is limited to 201
        int HTFArrSizeMax = 201;
        // Closes_Arr: idx1 = HTF, idx2 is HTF bars back
        double[,] Closes_Arr = new double[7, 201];

        // HighPrices_Arr: idx1 = HTF, idx2 is HTF bars back
        // Assume PivotDuration <= 20 (Array size 2)
        double[,] HighPrices_Arr = new double[7, 20];
        DateTime[,] HighTimes_Arr = new DateTime[7, 20];

        // LowPrices_Arr: idx1 = HTF, idx2 is HTF bars back
        // Assume PivotDuration <= 20 (Array size 2)
        double[,] LowPrices_Arr = new double[7, 20];
        DateTime[,] LowTimes_Arr = new DateTime[7, 20];

        /*----------------------------------- 7 time intervals total -----------------------------------*/
        public ISeries<double> HMA_X01 { get { return vHMAs[0]; } }
        public ISeries<double> HMA_X04 { get { return vHMAs[1]; } }
        public ISeries<double> HMA_X16 { get { return vHMAs[2]; } }
        public ISeries<double> HMA_X64 { get { return vHMAs[3]; } }

        // public ISeries<int> vActiveClrNum_5 { get { return vActiveClrNum[0]; } }

        public ISeries<double> HMA1_X01 { get { return vHMA1s[0]; } }
        public ISeries<double> HMA1_X04 { get { return vHMA1s[1]; } }
        public ISeries<double> HMA1_X16 { get { return vHMA1s[2]; } }
        public ISeries<double> HMA1_X64 { get { return vHMA1s[3]; } }

        // Check for StdDev > 0 before using the value
        public ISeries<double> StdDev_X01 { get { return vStDevs[0]; } }
        public ISeries<double> StdDev_X04 { get { return vStDevs[1]; } }
        public ISeries<double> StdDev_X16 { get { return vStDevs[2]; } }
        public ISeries<double> StdDev_X64 { get { return vStDevs[3]; } }

        public ISeries<double> PvtHighPrice_X01 { get { return vPvtHighPrices[0]; } }
        public ISeries<double> PvtHighPrice_X04 { get { return vPvtHighPrices[1]; } }
        public ISeries<double> PvtHighPrice_X16 { get { return vPvtHighPrices[2]; } }
        public ISeries<double> PvtHighPrice_X64 { get { return vPvtHighPrices[3]; } }

        public ISeries<double> PvtHighPrice1_X01 { get { return vPvtHighPrices1[0]; } }
        public ISeries<double> PvtHighPrice1_X04 { get { return vPvtHighPrices1[1]; } }
        public ISeries<double> PvtHighPrice1_X16 { get { return vPvtHighPrices1[2]; } }
        public ISeries<double> PvtHighPrice1_X64 { get { return vPvtHighPrices1[3]; } }

        public ISeries<double> PvtHighPrice2_X01 { get { return vPvtHighPrices2[0]; } }
        public ISeries<double> PvtHighPrice2_X04 { get { return vPvtHighPrices2[1]; } }
        public ISeries<double> PvtHighPrice2_X16 { get { return vPvtHighPrices2[2]; } }
        public ISeries<double> PvtHighPrice2_X64 { get { return vPvtHighPrices2[3]; } }


        public ISeries<DateTime> PvtHighTime_X01 { get { return vPvtHighTimes[0]; } }
        public ISeries<DateTime> PvtHighTime_X04 { get { return vPvtHighTimes[1]; } }
        public ISeries<DateTime> PvtHighTime_X16 { get { return vPvtHighTimes[2]; } }
        public ISeries<DateTime> PvtHighTime_X64 { get { return vPvtHighTimes[3]; } }


        public ISeries<double> PvtLowPrice_X01 { get { return vPvtLowPrices[0]; } }
        public ISeries<double> PvtLowPrice_X04 { get { return vPvtLowPrices[1]; } }
        public ISeries<double> PvtLowPrice_X16 { get { return vPvtLowPrices[2]; } }
        public ISeries<double> PvtLowPrice_X64 { get { return vPvtLowPrices[3]; } }

        public ISeries<double> PvtLowPrice1_X01 { get { return vPvtLowPrices1[0]; } }
        public ISeries<double> PvtLowPrice1_X04 { get { return vPvtLowPrices1[1]; } }
        public ISeries<double> PvtLowPrice1_X16 { get { return vPvtLowPrices1[2]; } }
        public ISeries<double> PvtLowPrice1_X64 { get { return vPvtLowPrices1[3]; } }

        public ISeries<double> PvtLowPrice2_X01 { get { return vPvtLowPrices2[0]; } }
        public ISeries<double> PvtLowPrice2_X04 { get { return vPvtLowPrices2[1]; } }
        public ISeries<double> PvtLowPrice2_X16 { get { return vPvtLowPrices2[2]; } }
        public ISeries<double> PvtLowPrice2_X64 { get { return vPvtLowPrices2[3]; } }


        public ISeries<DateTime> PvtLowTime_X01 { get { return vPvtLowTimes[0]; } }
        public ISeries<DateTime> PvtLowTime_X04 { get { return vPvtLowTimes[1]; } }
        public ISeries<DateTime> PvtLowTime_X16 { get { return vPvtLowTimes[2]; } }
        public ISeries<DateTime> PvtLowTime_X64 { get { return vPvtLowTimes[3]; } }

        string filePath;

        bool OKToRun;
        double StdDevInvLen2U;
        int StdDevLen2U;
        DayOfWeek myDOW;
        DateTime myDate;

        private HP_BarMntFromSessOpen_f mBarMFSO;
        private int barMFSO;

        double closeSum, closeAvg;
        double diff, diffSqSum, diffSqAvg;
        double stdDev;
        bool HTF_OnCloseTick;

        protected override void Create()
        {
            mBarMFSO = new HP_BarMntFromSessOpen_f(this);

            // Define 7 Time intervals to calculate
            HTF_TFs = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            HTF_BarCnts = new int[] { 0, 0, 0, 0, 0, 0, 0 };

            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfCalcTFs; j++)
            {
                // vHMAs.Add(new VariableSeries<double>(this));
                vHMAs_Temp.Add(new VariableObject<double>(this));
                vHMA1s_Temp.Add(new VariableObject<double>(this));

                vHMAs.Add(new VariableObject<double>(this));
                vHMA1s.Add(new VariableObject<double>(this));
                vActiveClrNum.Add(new VariableSeries<int>(this));
                vStDevs.Add(new VariableObject<double>(this));

                vPvtHighPrices.Add(new VariableSeries<double>(this));
                vPvtHighPrices1.Add(new VariableSeries<double>(this));
                vPvtHighPrices2.Add(new VariableSeries<double>(this));
                vPvtHighTimes.Add(new VariableObject<DateTime>(this));

                vPvtLowPrices.Add(new VariableSeries<double>(this));
                vPvtLowPrices1.Add(new VariableSeries<double>(this));
                vPvtLowPrices2.Add(new VariableSeries<double>(this));
                vPvtLowTimes.Add(new VariableObject<DateTime>(this));

                PriceLists.Add(new List<double>());
                mHMALists.Add(new _HMA_List_f(this));
            }

            // trend_Tool_Price_Input1 = new _SN_Trend_Tool_Price_Input(this);

            for (int j = 0; j < numOfCalcTFs; j++)
            {
                HTF_OnCloseTicks.Add(new VariableSeries<bool>(this));
                HTF_Prices.Add(new VariableSeries<double>(this));
                HTF_Opens.Add(new VariableSeries<double>(this));
                HTF_Closes.Add(new VariableSeries<double>(this));
                HTF_Highs.Add(new VariableSeries<double>(this));
                HTF_Lows.Add(new VariableSeries<double>(this));
                HTF_OpenCloseAvgs.Add(new VariableSeries<double>(this));
                HTF_OHLCAvgs.Add(new VariableSeries<double>(this));
            }
        }

        private int HMA_LenM1;

        protected override void StartCalc()
        {
            // trend_Tool_Price_Input1.Price_Input = this.Price_Input;
            alpha = 2.0 / (HMA_Length + 1.0);
            gain = (Smoothness) / 100.0;

            OnePip = (10.0 * Bars.Info.MinMove / Bars.Info.PriceScale);
            OneTick = (Bars.Info.MinMove / Bars.Info.PriceScale);

            vCategory = (int)Bars.Info.Category;
            // Need to include 9
            if (vCategory != 0 && vCategory != 2 && vCategory != 9 && vCategory != 12)
                vCategory = 4;

            int MntInterval = (int)Bars.Info.Resolution.Duration().TotalMinutes;

            if (Bars.Info.Resolution.ChartType == ChartType.Regular
                && (MntInterval == 5 || MntInterval == 15 || MntInterval == 20 || MntInterval == 30 || MntInterval == 60 || MntInterval == 240))
            {
                OKToRun = true;
            }
            else
            {
                OKToRun = false;
            }

            if (OKToRun)
            {

                // Define the 4 TFs to calc

                switch (MntInterval)
                {
                    default:
                    case 5:
                        HTF_TFs[0] = TF_5M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;

                    case 15:
                        HTF_TFs[0] = TF_15M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;

                    case 20:
                        HTF_TFs[0] = TF_20M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;

                    case 30:
                        HTF_TFs[0] = TF_30M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;

                    case 60:
                        HTF_TFs[0] = TF_60M;
                        HTF_TFs[1] = TF_240M;
                        HTF_TFs[2] = TF_Day;
                        HTF_TFs[3] = TF_Week;
                        break;

                    case 240:
                        HTF_TFs[0] = TF_240M;
                        HTF_TFs[1] = TF_Day;
                        HTF_TFs[2] = TF_Week;
                        HTF_TFs[3] = TF_Month;
                        break;
                }

                pointBuffer = ((double)TickBuffer) * OneTick;

                if (SettingOriginalOrNew == 1)
                {
                    pointBuffersAll = new double[] { OneTick, OneTick, OneTick, OneTick, OneTick,
                    OneTick, OneTick, OneTick, OneTick, OneTick, OneTick };
                }
                else
                {
                    pointBuffersAll = new double[] { OneTick, OneTick * 2, OneTick * 3, OneTick * 4, OneTick * 6,
                    OneTick * 12, OneTick * 24, OneTick * 48, OneTick * 100, OneTick * 100, OneTick * 100 };
                }

                pointBuffers = new double[] { pointBuffersAll[HTF_TFs[0]],
                                        pointBuffersAll[HTF_TFs[1]],
                                        pointBuffersAll[HTF_TFs[2]],
                                        pointBuffersAll[HTF_TFs[3]] };

                // Output.WriteLine("StartCalc: 900: pointBuffer {0} TickBuffer {1} OneTick {2}", pointBuffer, TickBuffer, OneTick);

                sessEnd_ELTime = 100 * (Bars.Sessions[0].EndTime.Hours) + Bars.Sessions[0].EndTime.Minutes;

                // for StdDev
                StdDevLen2U = StdDev_Length;
                if (StdDev_Length > 500)
                {
                    StdDevLen2U = 500;
                }

                StdDevInvLen2U = 1.0 / (double)StdDevLen2U;

                // reset data 
                for (int idx = 0; idx < numOfCalcTFs; idx++)
                {
                    HTF_BarCnts[idx] = 0;
                    // reset closes
                    for (int j = 0; j < StdDevLen2U; j++)
                    {
                        Closes_Arr[idx, j] = 0.0;
                    }

                    for (int j = 0; j < PivotDuration; j++)
                    {
                        HighPrices_Arr[idx, j] = 0.0;
                        LowPrices_Arr[idx, j] = 0.0;
                    }

                    mHMALists[idx].Length = HMA_Length;
                    mHMALists[idx].PriceList = PriceLists[idx];
                }

                HMA_LenM1 = HMA_Length - 1;

            }
            // EO: if (OKToRun)
        }

        protected override double CalcBar()
        {
            if (OKToRun && Bars.Status == EBarState.Close)
            {
                // EL Time
                int ELTime = 100 * (Bars.Time[0].Hour) + Bars.Time[0].Minute;
                barMFSO = mBarMFSO[0];

                // Loop through each TF
                for (int idx = 0; idx < numOfCalcTFs; idx++)
                {
                    int idxTF = HTF_TFs[idx];

                    // Define Bar Closed Condition for each TF
                    // Note that the bar end times defined do NOT match with the MC.Net IB EUR.USD chart bar end time
                    HTF_OnCloseTick = false;

                    switch (idxTF)
                    {
                        case TF_5M:
                            HTF_OnCloseTick = (barMFSO % 005) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_10M:
                            HTF_OnCloseTick = (barMFSO % 010) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_15M:
                            HTF_OnCloseTick = (barMFSO % 015) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_20M:
                            HTF_OnCloseTick = (barMFSO % 020) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_30M:
                            HTF_OnCloseTick = (barMFSO % 030) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_60M:
                            HTF_OnCloseTick = (barMFSO % 060) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_120M:
                            HTF_OnCloseTick = (barMFSO % 120) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_240M:
                            HTF_OnCloseTick = (barMFSO % 240) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_Day:
                            HTF_OnCloseTick = (ELTime == sessEnd_ELTime);
                            break;

                        case TF_Week:
                            HTF_OnCloseTick = (
        /* 
        // >> Need to define for other markets (than Forex)
         (vCategory == 2 && (ELTime == 1600 || ELTime == sessEnd_ELTime))
        || (vCategory == 4 && ELTime == sessEnd_ELTime)
        || (vCategory == 0 && ELTime == sessEnd_ELTime)
        || 
        */
        // End of Week for Forex: end of Session on Friday
        // May have rare exception
        ((vCategory == 9 || vCategory == 12) && (ELTime == sessEnd_ELTime && Bars.Time[0].DayOfWeek == DayOfWeek.Friday)));
                            break;

                        case TF_Month:
                            myDOW = Bars.Time[0].DayOfWeek;
                            myDate = Bars.Time[0].Date;
                            HTF_OnCloseTick = (
                                /* 
                                // >> Need to define for other markets (than Forex)
                                 (vCategory == 2 && (ELTime == 1600 || ELTime == sessEnd_ELTime))
                                || (vCategory == 4 && ELTime == sessEnd_ELTime)
                                || (vCategory == 0 && ELTime == sessEnd_ELTime)
                                || 
                                */
                                // End of Months for Forex: end of Session on Friday
                                // May have rare exception
                                ((vCategory == 9 || vCategory == 12) && (ELTime == sessEnd_ELTime && _isLastDayOfMonth_f(myDOW, myDate))));
                            break;

                        default:
                            // default, not defined
                            HTF_OnCloseTick = false;
                            break;

                    }

                    HTF_OnCloseTicks[idx].Value = HTF_OnCloseTick;

                    if (HTF_BarCnts[idx] > 0)
                    {
                        if (HTF_OnCloseTicks[idx][1])
                        {
                            // HTF Opening tick
                            // Shift stack if need to keep track of historical value of HTF OHLC
                            for (int j = StdDevLen2U - 1; j > 0; j--)
                            {
                                Closes_Arr[idx, j] = Closes_Arr[idx, j - 1];
                            }

                            // Keep track of PvtLowPrice and PvtLowTime in PivotDuration
                            for (int j = PivotDuration - 1; j > 0; j--)
                            {
                                HighPrices_Arr[idx, j] = HighPrices_Arr[idx, j - 1];
                                HighTimes_Arr[idx, j] = HighTimes_Arr[idx, j - 1];

                                LowPrices_Arr[idx, j] = LowPrices_Arr[idx, j - 1];
                                LowTimes_Arr[idx, j] = LowTimes_Arr[idx, j - 1];
                            }

                            // construct OHLC of HTF from Data1
                            HTF_Opens[idx].Value = Bars.Open[0];
                            HTF_Highs[idx].Value = Bars.High[0];
                            HTF_Lows[idx].Value = Bars.Low[0];

                            HighPrices_Arr[idx, 0] = Bars.High[0];
                            HighTimes_Arr[idx, 0] = Bars.Time[0];

                            LowPrices_Arr[idx, 0] = Bars.Low[0];
                            LowTimes_Arr[idx, 0] = Bars.Time[0];
                        }
                        else
                        {
                            // Keep track of High/Low
                            if (Bars.High[0] > HTF_Highs[idx].Value)
                            {
                                HTF_Highs[idx].Value = Bars.High[0];

                                // Keep track of HighPrices and HighTimes for this HTF bar
                                HighPrices_Arr[idx, 0] = Bars.High[0];
                                HighTimes_Arr[idx, 0] = Bars.Time[0];
                            }

                            if (Bars.Low[0] < HTF_Lows[idx].Value)
                            {
                                HTF_Lows[idx].Value = Bars.Low[0];

                                // Keep track of LowPrices and LowTimes for this HTF bar
                                LowPrices_Arr[idx, 0] = Bars.Low[0];
                                LowTimes_Arr[idx, 0] = Bars.Time[0];
                            }

                        }

                        HTF_Closes[idx].Value = Bars.Close[0];
                        // the latest close
                        Closes_Arr[idx, 0] = Bars.Close[0];

                        if (HTF_OnCloseTicks[idx][0])
                        {
                            // 0.5 * (Open + Close)
                            HTF_OpenCloseAvgs[idx].Value = 0.5 * (HTF_Opens[idx][0] + HTF_Closes[idx][0]);

                            // 0.25 * (O + H + L + C)
                            HTF_OHLCAvgs[idx].Value = 0.25 * (HTF_Highs[idx][0] + HTF_Lows[idx][0]
                                + HTF_Opens[idx][0] + HTF_Closes[idx][0]);

                            //------------------------------- Speed && Length Calculations -------------------------------
                            switch (Price_Input)
                            {
                                case 2:
                                    HTF_Prices[idx].Value = HTF_OpenCloseAvgs[idx][0];
                                    break;

                                case 3:
                                    HTF_Prices[idx].Value = HTF_OHLCAvgs[idx][0];
                                    break;

                                case 1:
                                default:
                                    // default & case 1 are the same
                                    HTF_Prices[idx].Value = HTF_Closes[idx][0];
                                    break;
                            }

                            //------------------------------- HMA Calculations -------------------------------
                            // Assume computation is done on Closed Bar!!!
                            vHMA1s[idx].Value = vHMAs[idx].Value;
                            //vHMA1s_Temp[idx].Value = vHMAs_Temp[idx].Value;

                            if (PriceLists[idx].Count > HMA_LenM1)
                            {
                                PriceLists[idx].RemoveAt(0);
                            }
                            PriceLists[idx].Add(HTF_Prices[idx][0]);

                            if (PriceLists[idx].Count == HMA_Length)
                            {
                                // Note use HMA value only when it is > 0
                                vHMAs_Temp[idx].Value = mHMALists[idx].Value;

                                // vHMAs[idx].Value = mHMALists[idx].Value;
                                // legacy code
                                //vHMAs[idx].Value = alpha * (vHMAs_Temp[idx].Value + gain * (HTF_Closes[idx].Value - vHMA1s[idx].Value))
                                //                + (1.0 - alpha) * vHMA1s[idx].Value;


                                if (HTF_Prices[idx].Value - vHMA1s[idx][0] > 0)
                                {
                                    vHMAs[idx].Value 
                                        = alpha * (vHMAs_Temp[idx].Value
                                        + (-gain) * (vHMAs_Temp[idx].Value - vHMA1s[idx][0]))
                                        + (1.0 - alpha) * vHMA1s[idx].Value;
                                }
                                else if (HTF_Prices[idx].Value - vHMA1s[idx][0] < 0)
                                {
                                    vHMAs[idx].Value
                                       = alpha * (vHMAs_Temp[idx].Value
                                       + (gain) * (vHMAs_Temp[idx].Value - vHMA1s[idx][0]))
                                       + (1.0 - alpha) * vHMA1s[idx].Value;
                                }
                                else
                                {
                                    vHMAs[idx].Value
                                       = alpha * (vHMAs_Temp[idx].Value
                                       + (0.0) * (vHMAs_Temp[idx].Value - vHMA1s[idx][0]))
                                       + (1.0 - alpha) * vHMA1s[idx].Value;
                                }

                            }

                            // No pointBuffers for HMA
                            // if (Math.Abs(vHMAs[idx].Value - vHMA1s[idx].Value) < pointBuffers[idx])
                            // vHMAs[idx].Value = vHMA1s[idx].Value;

                            //------------------------------- HMA Plot Color -------------------------------
                            if (vHMAs[idx][0] > vHMA1s[idx].Value)
                                vActiveClrNum[idx].Value = Upper_Active_ClrNum;
                            else if (vHMAs[idx][0] < vHMA1s[idx].Value)
                                vActiveClrNum[idx].Value = Lower_Active_ClrNum;

                            //--- PivotDuration High Price and Time for resistnace -----------------------------
                            // On Change to Dn color from Up color
                            // Look for PvtHighPrice and PvtHighTime in the latest PivotDuration HTF bars 
                            if (vActiveClrNum[idx][0] == Lower_Active_ClrNum
                                && vActiveClrNum[idx][1] == Upper_Active_ClrNum)
                            {
                                double PvtHighPrice = HighPrices_Arr[idx, 0];
                                DateTime PvtHighTime = HighTimes_Arr[idx, 0];
                                for (int j = 1; j < PivotDuration; j++)
                                {
                                    if (HighPrices_Arr[idx, j] > PvtHighPrice)
                                    {
                                        PvtHighPrice = HighPrices_Arr[idx, j];
                                        PvtHighTime = HighTimes_Arr[idx, j];
                                    }
                                }

                                // Return the PivotDuration PvtHighPrice and PvtHighTime for this HTF (idx)
                                vPvtHighPrices2[idx].Value = vPvtHighPrices1[idx].Value;
                                vPvtHighPrices1[idx].Value = vPvtHighPrices[idx].Value;
                                vPvtHighPrices[idx].Value = PvtHighPrice;
                                vPvtHighTimes[idx].Value = PvtHighTime;
                            }

                            //--- PivotDuration Low Price and Time for suppprt -----------------------------
                            // On Change to Up color from down color
                            // Look for PvtLowPrice and PvtLowTime in the latest PivotDuration HTF bars 
                            if (vActiveClrNum[idx][0] == Upper_Active_ClrNum
                                && vActiveClrNum[idx][1] == Lower_Active_ClrNum)
                            {
                                double PvtLowPrice = LowPrices_Arr[idx, 0];
                                DateTime PvtLowTime = LowTimes_Arr[idx, 0];
                                for (int j = 1; j < PivotDuration; j++)
                                {
                                    if (LowPrices_Arr[idx, j] < PvtLowPrice)
                                    {
                                        PvtLowPrice = LowPrices_Arr[idx, j];
                                        PvtLowTime = LowTimes_Arr[idx, j];
                                    }
                                }

                                // Return the PivotDuration PvtLowPrice and PvtLowTime for this HTF (idx)
                                vPvtLowPrices2[idx].Value = vPvtLowPrices1[idx].Value;
                                vPvtLowPrices1[idx].Value = vPvtLowPrices[idx].Value;
                                vPvtLowPrices[idx].Value = PvtLowPrice;
                                vPvtLowTimes[idx].Value = PvtLowTime;
                            }

                            // Calc StdDev on HTF bar close only
                            closeSum = 0.0;
                            diffSqSum = 0.0;
                            // Use > to prevent the use of the first data point on left side of chart
                            // as it may not be a complete HTF bar
                            if (HTF_BarCnts[idx] > StdDevLen2U)
                            {
                                // Have enough data, start computation
                                // Calc Avg
                                for (int j = 0; j < StdDevLen2U; j++)
                                {
                                    closeSum = closeSum + Closes_Arr[idx, j];
                                }
                                closeAvg = closeSum * StdDevInvLen2U;

                                // Calc Avg of ((Close - CloseAvg)^2)
                                for (int j = 0; j < StdDevLen2U; j++)
                                {
                                    diff = Closes_Arr[idx, j] - closeAvg;
                                    diffSqSum = diffSqSum + diff * diff;
                                }
                                diffSqAvg = diffSqSum * StdDevInvLen2U;

                                // Calc Std Dev
                                vStDevs[idx].Value = Math.Sqrt(diffSqAvg);

                            }

                        }

                    }
                    // EO: if (HTF_BarCnts[idx] > 0)

                    if (HTF_OnCloseTicks[idx][0])
                    {
                        HTF_BarCnts[idx] = HTF_BarCnts[idx] + 1;
                    }
                }

            }
            // EO: if (OKToRun && Bars.Status == EBarState.Close)

            return 1.0;
        }

        private int Mod(int x, int y)
        {
            return (x % y);
        }

        private bool HP_SessionStartBarMnt_f()
        {
            bool fRet = false;

            if (Bars.Status == EBarState.Close)
            {
                TimeSpan posOpenTime = Bars.TimeValue.TimeOfDay
                    - TimeSpan.FromMinutes(Bars.Info.Resolution.Size);

                if (posOpenTime == Bars.Sessions[0].StartTime)
                {
                    fRet = true;
                }
            }

            return fRet;
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

        private bool _isLastDayOfMonth_f(DayOfWeek thisDOW, DateTime thisDate)
        {
            bool isLastDayOfMonth;
            DateTime nextBusinessDay;

            if (thisDOW == DayOfWeek.Friday)
            {
                nextBusinessDay = thisDate.AddDays(3.0);
            }
            else
            {
                nextBusinessDay = thisDate.AddDays(1.0);
            }

            if (thisDate.Month != nextBusinessDay.Month)
            {
                isLastDayOfMonth = true;
            }
            else
            {
                isLastDayOfMonth = false;
            }

            return isLastDayOfMonth;
        }

    }


}
