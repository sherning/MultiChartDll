/*
_SN_HMAwithStdDev_162   20190831

Continue from _SN_PALwithStdDev_161
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PowerLanguage.Function;
using System.Windows.Forms;

namespace PowerLanguage.Indicator
{
    // [SameAsSymbol(true)]
    [MouseEvents(true)]
    public class _SN_HMAwithStdDev_162 : IndicatorObject
    {
        // Inputs for the PAL function to work 
        [Input]
        public string HMA_Function { get; set; }
        [Input]
        public int HMA_Length { get; set; }
        [Input]
        public int Price_Input { get; set; }
        [Input]
        public string Trend_Function { get; set; }
        [Input]
        public int Price_Trend_On { get; set; }

        [Input]
        public string Display_Inputs { get; set; }
        [Input]
        public int HMA_X01_Switch { get; set; }
        [Input]
        public int HMA_X04_Switch { get; set; }
        [Input]
        public int HMA_X16_Switch { get; set; }
        [Input]
        public int HMA_X64_Switch { get; set; }

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
        public string End_Inputs { get; set; }

        public _SN_HMAwithStdDev_162(object _ctx) : base(_ctx)
        {
            // define input default values

            HMA_Function = "HMA Settings";
            HMA_Length = 9;
            // Price_Input: 1-close only; 2-open && close; 3- HLOC 
            Price_Input = 1;

            // Inputs for Trend Function to Work 
            Trend_Function = "Trend Settings";
            Price_Trend_On = 0;

            Display_Inputs = "Display Inputs";
            // on / off switch: 1 to plot, 0 or negative value to turn display off
            // can have multiple ones on
            HMA_X01_Switch = 1;
            HMA_X04_Switch = 1;
            HMA_X16_Switch = 1;
            HMA_X64_Switch = 1;

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
        private VariableObject<int> vActiveClrNum_5;

        private List<VariableSeries<double>> StdDevBands = new List<VariableSeries<double>>();

        private List<IPlotObject> thePlots = new List<IPlotObject>();

        private List<IPlotObject> StdDevPlots = new List<IPlotObject>();
        private VariableObject<int> Value40, PAL_Width;

        private string[] thePlotNames;
        private string[] StdDevPlotNames;
        private int[] thePlotWidth;
        private EPlotStyle[] thePlotstyles;
        private EPlotStyle[] StdDevPlotStyles;
        private int[] thePlotDisplays;
        private int[] StdDevPlotDisplays = new int[6];
        private Color[] ActiveColorFrActiveClrNum;
        private Color[] StdDevPlotColors;

        private _SN_Trend_Tool_Price_Input trend_Tool_Price_Input1;
        private _SN_HMAnStdDev_162_f mHMAnStdDev;

        private VariableSeries<double> vHMA2U4StdDev, StdDevValue2U;

        protected override void Create()
        {
            // define Plot Names
            thePlotNames = new string[] { "HMA_X01", "HMA_X04", "HMA_X16", "HMA_X64" };

            thePlotstyles = new EPlotStyle[] {EPlotStyle.Solid, EPlotStyle.Solid, EPlotStyle.Solid, EPlotStyle.DashDot };

            thePlotWidth = new int[] { 1, 3, 5, 1 };

            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfCalcTFs; j++)
            {
                vHMAs.Add(new VariableSeries<double>(this));
                vHMA1s.Add(new VariableObject<double>(this));
                vActiveClrNum.Add(new VariableObject<int>(this));
                vStDevs.Add(new VariableObject<double>(this));

                thePlots.Add(AddPlot(new PlotAttributes(thePlotNames[j], EPlotShapes.Line, Color.Yellow,
                    Color.Black, thePlotWidth[j], thePlotstyles[j], true)));
            }


            Value40 = new VariableObject<int>(this);
            PAL_Width = new VariableObject<int>(this);

            trend_Tool_Price_Input1 = new _SN_Trend_Tool_Price_Input(this);
            mHMAnStdDev = new _SN_HMAnStdDev_162_f(this);

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
            ActiveColorFrActiveClrNum = new Color[]
               { Color.White, Color.FromName(Upper_Active_Color_Name), Color.FromName(Lower_Active_Color_Name)};

            // display settings
            thePlotDisplays = new int[] { HMA_X01_Switch, HMA_X04_Switch, HMA_X16_Switch, HMA_X64_Switch };

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

            mHMAnStdDev.HMA_Length = this.HMA_Length;
            mHMAnStdDev.Price_Input = this.Price_Input;
            mHMAnStdDev.StdDev_Length = this.StdDev_Length;
            mHMAnStdDev.StdDev_DataType = this.StdDev_DataType;

            trend_Tool_Price_Input1.Price_Input = this.Price_Input;
        }


        protected override void CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                mHMAnStdDev.Call();

                vHMAs[0].Value = mHMAnStdDev.HMA_X01[0];
                vHMAs[1].Value = mHMAnStdDev.HMA_X04[0];
                vHMAs[2].Value = mHMAnStdDev.HMA_X16[0];
                vHMAs[3].Value = mHMAnStdDev.HMA_X64[0];

                vHMA1s[0].Value = mHMAnStdDev.HMA1_X01[0];
                vHMA1s[1].Value = mHMAnStdDev.HMA1_X04[0];
                vHMA1s[2].Value = mHMAnStdDev.HMA1_X16[0];
                vHMA1s[3].Value = mHMAnStdDev.HMA1_X64[0];

                vStDevs[0].Value = mHMAnStdDev.StdDev_X01[0];
                vStDevs[1].Value = mHMAnStdDev.StdDev_X04[0];
                vStDevs[2].Value = mHMAnStdDev.StdDev_X16[0];
                vStDevs[3].Value = mHMAnStdDev.StdDev_X64[0];

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

            // Plots
            for (int plotIdx1 = 0; plotIdx1 < numOfCalcTFs; plotIdx1++)
            {
                if (thePlotDisplays[plotIdx1] == 1 && vHMAs[plotIdx1][1] > 0)
                {
                    thePlots[plotIdx1].Set(1, vHMAs[plotIdx1][1], ActiveColorFrActiveClrNum[vActiveClrNum[plotIdx1].Value]);
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
