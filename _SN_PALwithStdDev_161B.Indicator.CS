/*
_SN_PALwithStdDev_161B	20191003
* Special version to test PAL_5 to follow IB timing
  similar to PAL_15 in _SN_PALwithStdDev_161
* Must run on 5 min chart, just for PAL 5, 60, 240 and daily only

_SN_PALwithStdDev_161   20190831
* Add Monthly
* New algorithm for bar end time for Minutes

_SN_PALwithStdDev_160
* Use PAL160 to calc 4 TF's

_SN_PALwithStdDev_142
* Add Std Dev Band for PAL 60, 240, Day
    Standard Deviation
    StdDev for 1H, 4H, Daily.
    Up to 3rd standard deviation, both direction negative and positive.
    On / off switch for each StdDev.

Continue from _SN_PAL141
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    // [SameAsSymbol(true)]
    public class _SN_PALwithStdDev_161B : IndicatorObject
    {
        // Inputs for the PAL function to work 
        [Input]
        public string PAL_Function { get; set; }
        [Input]
        public int Speed_Input { get; set; }
        [Input]
        public int Length_Input { get; set; }
        [Input]
        public int TickBuffer { get; set; }
        [Input]
        public int Price_Input { get; set; }
        [Input]
        public int SettingOriginalOrNew { get; set; }
        [Input]
        public string Trend_Function { get; set; }
        [Input]
        public int Price_Trend_On { get; set; }

        [Input]
        public string Display_Inputs { get; set; }
        [Input]
        public int PAL_X01_Switch { get; set; }
        [Input]
        public int PAL_X04_Switch { get; set; }
        [Input]
        public int PAL_X16_Switch { get; set; }
        [Input]
        public int PAL_X64_Switch { get; set; }

        [Input]
        public string Upper_Active_Color_Name { get; set; }
        [Input]
        public string Lower_Active_Color_Name { get; set; }

        [Input]
        public int StdDev_Length { get; set; }
        [Input]
        public int StdDev_DataType { get; set; }

        [Input]
        public int StdDev_X01_Switch { get; set; }
        [Input]
        public int StdDev_X04_Switch { get; set; }
        [Input]
        public int StdDev_X16_Switch { get; set; }
        [Input]
        public int StdDev_X64_Switch { get; set; }

        [Input]
        public double NumDevs1Value { get; set; }
        [Input]
        public double NumDevs2Value { get; set; }
        [Input]
        public double NumDevs3Value { get; set; }
        [Input]
        public string NumDevs1ColorName { get; set; }
        [Input]
        public string NumDevs2ColorName { get; set; }
        [Input]
        public string NumDevs3ColorName { get; set; }
        [Input]
        public bool isDebugMode { get; set; }

        [Input]
        public string End_Inputs { get; set; }

        public _SN_PALwithStdDev_161B(object _ctx) : base(_ctx)
        {
            // define input default values

            PAL_Function = "PAL Settings";
            Speed_Input = 100;
            Length_Input = 6;
            TickBuffer = 0;
            // Price_Input: 1-close only; 2-open && close; 3- HLOC 
            Price_Input = 1;
            // SettingOriginalOrNew: 1 Original or 2 New
            SettingOriginalOrNew = 1;

            // Inputs for Trend Function to Work 
            Trend_Function = "Trend Settings";
            Price_Trend_On = 0;

            Display_Inputs = "Display Inputs";
            // on / off switch: 1 to plot, 0 or negative value to turn display off
            // can have multiple ones on
            PAL_X01_Switch = 0;
            PAL_X04_Switch = 1;
            PAL_X16_Switch = 1;
            PAL_X64_Switch = 1;

            Upper_Active_Color_Name = "LawnGreen";
            Lower_Active_Color_Name = "Crimson";

            StdDev_Length = 20;
            // StdDev_DataType 1 for population, 2 for sample
            StdDev_DataType = 1;

            // on / off switch for Std Dev: 1 to plot, 0 or negative value to turn display off
            // To avoid too many plots on charts (and in program), just plot StdDev's for one TF only
            // Set one of them to 1, all the others are set to 0 or -1
            // The last one switch that is set to 1 is used;
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

            isDebugMode = false;

            End_Inputs = "End";
        }

        // Number of Time Frames
        private const int numOfCalcTFs = 4;

        // Number of StdDev plots
        private const int numOfStdDev = 6;

        // int numbers for each Upper_Active_Color_Name and Lower_Active_Color_Name
        private const int Upper_Active_ClrNum = 1;
        private const int Lower_Active_ClrNum = 2;

        private List<VariableSeries<double>> vPALs = new List<VariableSeries<double>>();
        private List<VariableObject<double>> vPAL1s = new List<VariableObject<double>>();
        private List<VariableObject<int>> vActiveClrNum = new List<VariableObject<int>>();
        private List<VariableObject<double>> vStDevs = new List<VariableObject<double>>();
        private VariableObject<int> vActiveClrNum_5;

        private List<VariableSeries<double>> StdDevBands = new List<VariableSeries<double>>();

        private List<IPlotObject> PALPlots = new List<IPlotObject>();

        private List<IPlotObject> StdDevPlots = new List<IPlotObject>();
        private VariableObject<int> Value40, PAL_Width;

        private string[] PALPlotNames;
        private string[] StdDevPlotNames;
        private EPlotStyle[] PALPlotStyles;
        private EPlotStyle[] StdDevPlotStyles;
        private int[] PALPlotDisplays;
        private int[] StdDevPlotDisplays = new int[6];
        private Color[] ActiveColorFrActiveClrNum;
        private Color[] StdDevPlotColors;

        //private _SN_Trend_Tool_Price_Input trend_Tool_Price_Input1;
        private _SN_PALnStdDev_161B_f mPAL;

        private VariableSeries<double> vPAL2U4StdDev, StdDevValue2U;

        protected override void Create()
        {
            // define Plot Names
            PALPlotNames = new string[] { "PAL_X01", "PAL_X04", "PAL_X16", "PAL_X64" };

            PALPlotStyles = new EPlotStyle[] { EPlotStyle.Solid, EPlotStyle.Dot, EPlotStyle.Solid, EPlotStyle.Dash };

            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfCalcTFs; j++)
            {
                vPALs.Add(new VariableSeries<double>(this));
                vPAL1s.Add(new VariableObject<double>(this));
                vActiveClrNum.Add(new VariableObject<int>(this));
                vStDevs.Add(new VariableObject<double>(this));

                PALPlots.Add(AddPlot(new PlotAttributes(PALPlotNames[j], EPlotShapes.Line, Color.Yellow,
                    Color.Black, 3, PALPlotStyles[j], true)));
            }


            Value40 = new VariableObject<int>(this);
            PAL_Width = new VariableObject<int>(this);

            // trend_Tool_Price_Input1 = new _SN_Trend_Tool_Price_Input(this);
            mPAL = new _SN_PALnStdDev_161B_f(this);

            // StdDev plots
            StdDevPlotNames = new string[] { "+StdDev1", "-StdDev1",
                                "+StdDev2", "-StdDev2", "+StdDev3", "-StdDev3" };

            vPAL2U4StdDev = new VariableSeries<double>(this);
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
            ActiveColorFrActiveClrNum = new Color[]
               { Color.White, Color.FromName(Upper_Active_Color_Name), Color.FromName(Lower_Active_Color_Name)};

            // display settings
            PALPlotDisplays = new int[] { PAL_X01_Switch, PAL_X04_Switch, PAL_X16_Switch, PAL_X64_Switch };

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

            mPAL.Speed_Input = this.Speed_Input;
            mPAL.Length_Input = this.Length_Input;
            mPAL.TickBuffer = this.TickBuffer;
            mPAL.Price_Input = this.Price_Input;
            mPAL.SettingOriginalOrNew = this.SettingOriginalOrNew;
            mPAL.StdDev_Length = this.StdDev_Length;
            mPAL.StdDev_DataType = this.StdDev_DataType;
            mPAL.isDebugMode = isDebugMode;

            // trend_Tool_Price_Input1.Price_Input = this.Price_Input;
        }


        protected override void CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                mPAL.Call();

                vPALs[0].Value = mPAL.PAL_X01[0];
                vPALs[1].Value = mPAL.PAL_X04[0];
                vPALs[2].Value = mPAL.PAL_X16[0];
                vPALs[3].Value = mPAL.PAL_X64[0];

                vPAL1s[0].Value = mPAL.PAL1_X01[0];
                vPAL1s[1].Value = mPAL.PAL1_X04[0];
                vPAL1s[2].Value = mPAL.PAL1_X16[0];
                vPAL1s[3].Value = mPAL.PAL1_X64[0];

                vStDevs[0].Value = mPAL.StdDev_X01[0];
                vStDevs[1].Value = mPAL.StdDev_X04[0];
                vStDevs[2].Value = mPAL.StdDev_X16[0];
                vStDevs[3].Value = mPAL.StdDev_X64[0];

                for (int idxTF = 0; idxTF < numOfCalcTFs; idxTF++)
                {
                    if (vPALs[idxTF][0] > vPAL1s[idxTF].Value)
                        vActiveClrNum[idxTF].Value = Upper_Active_ClrNum;
                    else if (vPALs[idxTF][0] < vPAL1s[idxTF].Value)
                        vActiveClrNum[idxTF].Value = Lower_Active_ClrNum;
                }

                // ++++++++++++++++++++++++++++++++++++++  SN_Trend_Tool function  ++++++++++++++++++++++++++++++ 
                //if (Price_Trend_On == 1)
                //{
                //    Value40.Value = trend_Tool_Price_Input1.Value;

                //    // LONG PAL && DOWN TREND 
                //    if (vActiveClrNum[0].Value == Upper_Active_ClrNum && Value40[0] < 0)
                //        PAL_Width.Value = 0;
                //    else
                //    // SHORT PAL && UP TREND 
                //    if (vActiveClrNum[0].Value == Lower_Active_ClrNum && Value40[0] > 0)
                //        PAL_Width.Value = 0;
                //    else
                //    // LONG PAL && UP TREND 
                //    if (vActiveClrNum[0].Value == Upper_Active_ClrNum && Value40[0] > 0)
                //        PAL_Width.Value = Value40[0];
                //    else
                //    // SHORT PAL && DOWN TREND 
                //    if (vActiveClrNum[0].Value == Lower_Active_ClrNum && Value40[0] < 0)
                //        PAL_Width.Value = Math.Abs(Value40[0]);
                //}

                //if (Price_Trend_On != 1)
                //    PAL_Width.Value = 2;

                // ++++++++++++++++++++++++++++++++++++  end of SN_Trend_Tool function ++++++++++++++++++++++++++ 

                if (StdDev_X01_Switch == 1)
                {
                    stdDevTFidx = 0;
                    vPAL2U4StdDev.Value = vPALs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                if (StdDev_X04_Switch == 1)
                {
                    stdDevTFidx = 1;
                    vPAL2U4StdDev.Value = vPALs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                if (StdDev_X16_Switch == 1)
                {
                    stdDevTFidx = 2;
                    vPAL2U4StdDev.Value = vPALs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                if (StdDev_X64_Switch == 1)
                {
                    stdDevTFidx = 3;
                    vPAL2U4StdDev.Value = vPALs[stdDevTFidx].Value;
                    StdDevValue2U.Value = vStDevs[stdDevTFidx].Value;
                }

                // +/-, +/-, +/-
                StdDevBands[0].Value = vPAL2U4StdDev[0] + NumDevs1Value * StdDevValue2U[0];
                StdDevBands[1].Value = vPAL2U4StdDev[0] - NumDevs1Value * StdDevValue2U[0];
                StdDevBands[2].Value = vPAL2U4StdDev[0] + NumDevs2Value * StdDevValue2U[0];
                StdDevBands[3].Value = vPAL2U4StdDev[0] - NumDevs2Value * StdDevValue2U[0];
                StdDevBands[4].Value = vPAL2U4StdDev[0] + NumDevs3Value * StdDevValue2U[0];
                StdDevBands[5].Value = vPAL2U4StdDev[0] - NumDevs3Value * StdDevValue2U[0];
            }

            // PAL Plots
            for (int plotIdx1 = 0; plotIdx1 < numOfCalcTFs; plotIdx1++)
            {
                if (PALPlotDisplays[plotIdx1] == 1 && vPALs[plotIdx1][1] > 0)
                {
                    PALPlots[plotIdx1].Set(1, vPALs[plotIdx1][1], ActiveColorFrActiveClrNum[vActiveClrNum[plotIdx1].Value]);
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
        }
    }
}
