/*
_SN_PAC_203_f	20180228	2000	

_SN_PAC_201_f	20180225	1600	
* For TS Forex, it is Forex, the Category number is 12.
* For IB Forex, it is Cash, the Category number is 9.

_SN_PAC_##_f	20180201	1351	

*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PowerLanguage
{
    namespace Function
    {
        public sealed class _SN_PAC_203_f : FunctionSeries<bool>
        {
            public _SN_PAC_203_f(CStudyControl _master) : base(_master) { }

            /*
            // Could not use _SN_VolatilityVariable101_f for PAC 20180131
            // _SN_VolatilityVariable101_f input
            public int Vol_Var_Mode { get; set; }
            public int ATR_Length { get; set; }
            public int ATR_Speed { get; set; }
            public int Mode_3_Days_Back { get; set; }
            public int Mode_4_Weeks_Back { get; set; }
            */
            public int ATR_Type { get; set; }
            public int ATR_Length { get; set; }

            // PAC Inputs
            public int Speed { get; set; }
            public int Length { get; set; }
            public int TickBuffer { get; set; }
            public int PriceInput { get; set; }
            public double RangeMultiple { get; set; }
            public double DistanceMultiple { get; set; }
            public double NewStepMultiple { get; set; }
            public int PivotLength { get; set; }
            public int MinMovesPerPips { get; set; }

            // Define ISeries output, return data from VariableSeries,  for function

            public IVar<int> haveValidPAC_5 { get; set; }
            public IVar<double> PAC_5_LowerVal { get; set; }
            public IVar<double> PAC_5_UpperVal { get; set; }
            public IVar<bool> PAC_5_LowerAct { get; set; }
            public IVar<bool> PAC_5_UpperAct { get; set; }


            public IVar<int> haveValidPAC_10 { get; set; }
            public IVar<double> PAC_10_LowerVal { get; set; }
            public IVar<double> PAC_10_UpperVal { get; set; }
            public IVar<bool> PAC_10_LowerAct { get; set; }
            public IVar<bool> PAC_10_UpperAct { get; set; }

            public IVar<int> haveValidPAC_15 { get; set; }
            public IVar<double> PAC_15_LowerVal { get; set; }
            public IVar<double> PAC_15_UpperVal { get; set; }
            public IVar<bool> PAC_15_LowerAct { get; set; }
            public IVar<bool> PAC_15_UpperAct { get; set; }

            public IVar<int> haveValidPAC_20 { get; set; }
            public IVar<double> PAC_20_LowerVal { get; set; }
            public IVar<double> PAC_20_UpperVal { get; set; }
            public IVar<bool> PAC_20_LowerAct { get; set; }
            public IVar<bool> PAC_20_UpperAct { get; set; }

            public IVar<int> haveValidPAC_30 { get; set; }
            public IVar<double> PAC_30_LowerVal { get; set; }
            public IVar<double> PAC_30_UpperVal { get; set; }
            public IVar<bool> PAC_30_LowerAct { get; set; }
            public IVar<bool> PAC_30_UpperAct { get; set; }

            public IVar<int> haveValidPAC_60 { get; set; }
            public IVar<double> PAC_60_LowerVal { get; set; }
            public IVar<double> PAC_60_UpperVal { get; set; }
            public IVar<bool> PAC_60_LowerAct { get; set; }
            public IVar<bool> PAC_60_UpperAct { get; set; }

            public IVar<int> haveValidPAC_120 { get; set; }
            public IVar<double> PAC_120_LowerVal { get; set; }
            public IVar<double> PAC_120_UpperVal { get; set; }
            public IVar<bool> PAC_120_LowerAct { get; set; }
            public IVar<bool> PAC_120_UpperAct { get; set; }

            public IVar<int> haveValidPAC_240 { get; set; }
            public IVar<double> PAC_240_LowerVal { get; set; }
            public IVar<double> PAC_240_UpperVal { get; set; }
            public IVar<bool> PAC_240_LowerAct { get; set; }
            public IVar<bool> PAC_240_UpperAct { get; set; }

            public IVar<int> haveValidPAC_Day { get; set; }
            public IVar<double> PAC_Day_LowerVal { get; set; }
            public IVar<double> PAC_Day_UpperVal { get; set; }
            public IVar<bool> PAC_Day_LowerAct { get; set; }
            public IVar<bool> PAC_Day_UpperAct { get; set; }

            // Internal switches, must set to true to get dat
            // set for 5 and 10 min for now
            // (for Indicator, these mean Yes/No to display
            // for function, these mean Yes/No to calc T and B)
            private bool Display_5 = true;
            private bool Display_10 = true;
            private bool Display_15 = true;
            private bool Display_20 = true;
            private bool Display_30 = true;
            private bool Display_60 = true;
            private bool Display_120 = true;
            private bool Display_240 = true;
            private bool Display_Day = true;

            /*
            IApplicationInfo appInfo;
            private Color[] Up_Color;
            private Color[] Dn_Color;
            private Color[] InActive_Color;
            */
            // The color values are defined in StartCalc()

            private string Width_Inputs = "Width Inputs";
            private int Width_Up_5 = 0;
            private int Width_Dn_5 = 0;

            //	ATR_Settings("ATR Settings")
            // ATR_Multiples for                           5 min, 10 min, 15 min, 20 min, 30 min, 60 min, 120 min, 240 min, Daily    
            private double[] ATR_Multiple = new double[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            private double PtsPerTick, PtsPerPips, OnePip, OneTick;
            private int vCategory, End_Time;

            private double Speed2User, Length2User, PACLengthXPACSpeed, pointBuffer;

            // Number of Time Frames, total 9, for function
            private const int numOfTFs = 9;
            // List index from 0 to 8
            private const int TF_5M = 0;
            private const int TF_10M = 1;
            private const int TF_15M = 2;
            private const int TF_20M = 3;
            private const int TF_30M = 4;
            private const int TF_60M = 5;
            private const int TF_120M = 6;
            private const int TF_240M = 7;
            private const int TF_Day = 8;

            private List<VariableObject<int>> haveValidPAC = new List<VariableObject<int>>();

            // R=Range (in indicator)
            private List<VariableObject<double>> vRs = new List<VariableObject<double>>();

            private List<VariableObject<double>> vATRs = new List<VariableObject<double>>();
            private List<Queue<double>> qRanges = new List<Queue<double>>();

            private int HHLL5_Length = 5;
            private int HHLL6_Length = 6;
            private List<Queue<double>> qH5s = new List<Queue<double>>();
            private List<Queue<double>> qH6s = new List<Queue<double>>();
            private List<Queue<double>> qL5s = new List<Queue<double>>();
            private List<Queue<double>> qL6s = new List<Queue<double>>();
            private List<VariableObject<double>> vHighest5 = new List<VariableObject<double>>();
            private List<VariableObject<double>> vHighest6 = new List<VariableObject<double>>();
            private List<VariableObject<double>> vLowest5 = new List<VariableObject<double>>();
            private List<VariableObject<double>> vLowest6 = new List<VariableObject<double>>();

            private List<VariableObject<bool>> vNewSupports = new List<VariableObject<bool>>();
            private List<VariableObject<bool>> vNewResistances = new List<VariableObject<bool>>();
            private List<VariableObject<double>> vMinimum_Ranges = new List<VariableObject<double>>();
            private List<VariableObject<double>> vDistance_Buffers = new List<VariableObject<double>>();
            private List<VariableObject<double>> vNew_Step_Buffers = new List<VariableObject<double>>();

            private List<VariableSeries<double>> vPALs = new List<VariableSeries<double>>();
            private List<VariableObject<double>> vPAL_1s = new List<VariableObject<double>>();
            private List<VariableSeries<int>> vColor1s = new List<VariableSeries<int>>();

            private string[] upperPlotNames;
            private string[] lowerPlotNames;
            private bool[] displays;


            private int[] priceCalcBars;
            private List<VariableSeries<double>> vPrices = new List<VariableSeries<double>>();
            private List<VariableSeries<double>> vOpens = new List<VariableSeries<double>>();
            private List<VariableSeries<double>> vCloses = new List<VariableSeries<double>>();
            private List<VariableSeries<double>> vHighs = new List<VariableSeries<double>>();
            private List<VariableSeries<double>> vLows = new List<VariableSeries<double>>();
            private List<VariableSeries<double>> vRanges = new List<VariableSeries<double>>();
            private List<VariableSeries<double>> vOCs = new List<VariableSeries<double>>();
            private List<VariableSeries<double>> vHLOCs = new List<VariableSeries<double>>();

            private VariableSeries<double> Value30, Value31;


            // BD=Bias Direction
            private List<VariableSeries<int>> vBDs = new List<VariableSeries<int>>();
            // B=Bottom PAC
            private List<VariableSeries<double>> vB_1s = new List<VariableSeries<double>>();
            private List<VariableObject<double>> vB_2s = new List<VariableObject<double>>();
            private List<VariableObject<double>> vB_3s = new List<VariableObject<double>>();
            // T=Top PAC
            private List<VariableSeries<double>> vT_1s = new List<VariableSeries<double>>();
            private List<VariableObject<double>> vT_2s = new List<VariableObject<double>>();
            private List<VariableObject<double>> vT_3s = new List<VariableObject<double>>();

            // New ATR calc method
            private List<VariableObject<int>> barNums = new List<VariableObject<int>>();
            private List<VariableSeries<double>> ATR2s = new List<VariableSeries<double>>();
            // separate ATR for 5 min for possible comparison
            private VariableSeries<double> ATR5;


            protected override void Create()
            {

                // No Plots for function
                // define Plot Names
                /*
                upperPlotNames = new string[] { "5-PA Upper", "10-PA Upper", "15-PA Upper", "20-PA Upper", "30-PA Upper",
                                     "60-PA Upper", "120-PA Upper", "240-PA Upper", "DAY-PA Upper" };
                lowerPlotNames = new string[] { "5-PA Lower", "10-PA Lower", "15-PA Lower", "20-PA Lower", "30-PA Lower",
                                     "60-PA Lower", "120-PA Lower", "240-PA Lower", "DAY-PA Lower" };

                plotStyles = new EPlotStyle[] {EPlotStyle.Solid, EPlotStyle.Dot, EPlotStyle.DashDot, EPlotStyle.Dash, EPlotStyle.Solid,
                                    EPlotStyle.Dot, EPlotStyle.DashDot, EPlotStyle.Dash, EPlotStyle.Solid};
                */

                // create variable objects, function objects, plot objects etc.
                for (int j = 0; j < numOfTFs; j++)
                {
                    haveValidPAC.Add(new VariableObject<int>(this));

                    vATRs.Add(new VariableObject<double>(this));
                    qRanges.Add(new Queue<double>());
                    vRs.Add(new VariableObject<double>(this));

                    qH5s.Add(new Queue<double>());
                    qH6s.Add(new Queue<double>());
                    qL5s.Add(new Queue<double>());
                    qL6s.Add(new Queue<double>());
                    vHighest5.Add(new VariableObject<double>(this));
                    vHighest6.Add(new VariableObject<double>(this));
                    vLowest5.Add(new VariableObject<double>(this));
                    vLowest6.Add(new VariableObject<double>(this));

                    vNewSupports.Add(new VariableObject<bool>(this));
                    vNewResistances.Add(new VariableObject<bool>(this));
                    vMinimum_Ranges.Add(new VariableObject<double>(this));
                    vDistance_Buffers.Add(new VariableObject<double>(this));
                    vNew_Step_Buffers.Add(new VariableObject<double>(this));

                    vPALs.Add(new VariableSeries<double>(this));
                    vPAL_1s.Add(new VariableObject<double>(this));
                    vColor1s.Add(new VariableSeries<int>(this));

                    vBDs.Add(new VariableSeries<int>(this));
                    vB_1s.Add(new VariableSeries<double>(this));
                    vB_2s.Add(new VariableObject<double>(this));
                    vB_3s.Add(new VariableObject<double>(this));
                    vT_1s.Add(new VariableSeries<double>(this));
                    vT_2s.Add(new VariableObject<double>(this));
                    vT_3s.Add(new VariableObject<double>(this));

                    // No Plot for function
                    /*
                    upperPlots.Add(AddPlot(new PlotAttributes(upperPlotNames[j], EPlotShapes.Line, Color.Empty,
                        Color.Empty, 0, plotStyles[j], true)));
                    lowerPlots.Add(AddPlot(new PlotAttributes(lowerPlotNames[j], EPlotShapes.Line, Color.Empty,
                        Color.Empty, 0, plotStyles[j], true)));
                    */

                    barNums.Add(new VariableObject<int>(this));
                    ATR2s.Add(new VariableSeries<double>(this));
                }


                ATR5 = new VariableSeries<double>(this);

                // for SN_ATRnPAC_MTF_FN
                priceCalcBars = new int[] { 1, 2, 3, 4, 6, 12, 24, 48, 288 };

                for (int j = 0; j < numOfTFs; j++)
                {
                    vPrices.Add(new VariableSeries<double>(this));
                    vOpens.Add(new VariableSeries<double>(this));
                    vCloses.Add(new VariableSeries<double>(this));
                    vHighs.Add(new VariableSeries<double>(this));
                    vLows.Add(new VariableSeries<double>(this));
                    vRanges.Add(new VariableSeries<double>(this));
                    vOCs.Add(new VariableSeries<double>(this));
                    vHLOCs.Add(new VariableSeries<double>(this));
                }

                Value30 = new VariableSeries<double>(this);
                Value31 = new VariableSeries<double>(this);

            }

            protected override void StartCalc()
            {
                var instrument1 = Bars;
                // IApplicationInfo appInfo = Environment;

                // display settings
                displays = new bool[] { Display_5, Display_10, Display_15, Display_20, Display_30,
                                        Display_60, Display_120, Display_240, Display_Day };

                /*
                //Active_Color_Inputs("Active Color Inputs"),
                // Up Color            5 min           10 min      15 min      20 min      30 min    
                // Up Color            60 min          120 min      240 min     Daily    
                Up_Color = new Color[]{Color.Green, Color.Green, Color.Green, Color.Green, Color.Green,
                                            Color.Green, Color.Green, Color.Green, Color.Green};

                // Dn Color           5 min       10 min     15 min     20 min     30 min    
                // Dn Color           60 min      120 min    240 min    Daily    
                Dn_Color = new Color[]{Color.Red, Color.Red, Color.Red, Color.Red, Color.Red,
                                            Color.Red, Color.Red, Color.Red, Color.Red};

                //InActive_Color_Inputs("InActive Color Inputs"),
                // InActive_Color                   5 min       10 min     15 min     20 min     30 min    
                // InActive_Color                   60 min      120 min    240 min    Daily   
                InActive_Color = new Color[]{Color.DarkGray, appInfo.BGColor, appInfo.BGColor, appInfo.BGColor, appInfo.BGColor,
                                                 appInfo.BGColor, appInfo.BGColor, appInfo.BGColor, appInfo.BGColor };
                // Use MC.Net BGColor to replace EL Transparent (not ideal, but that is fix for now, 20170807)
                */


                PtsPerTick = (Bars.Info.MinMove / Bars.Info.PriceScale);
                PtsPerPips = MinMovesPerPips * PtsPerTick;

                OneTick = PtsPerTick;
                OnePip = PtsPerPips;

                vCategory = (int)Bars.Info.Category;
                // Need to include 9
                if (vCategory != 0 && vCategory != 2 && vCategory != 9 && vCategory != 12)
                    vCategory = 4;
                Speed2User = ((int)Speed) / 100.0;
                Length2User = 2.0 / (((double)Length) + 1.0);
                PACLengthXPACSpeed = Length2User * Speed2User;
                pointBuffer = ((double)TickBuffer) * OnePip;
                //Output.WriteLine("StartCalc: 900: pointBuffer {0} TickBuffer {1} OnePip {2}", pointBuffer, TickBuffer, OnePip);

                // The original EL code uses SessionEndTime(1,1); which means regular Session.
                // Make sure to use right session settings in MultiCharts.Net
                End_Time = 100 * (Bars.Sessions[0].EndTime.Hours) + Bars.Sessions[0].EndTime.Minutes;

                for (int idxTF = 0; idxTF < numOfTFs; idxTF++)
                {
                    // no valid results until proven otherwise
                    haveValidPAC[idxTF].Value = 0;
                }

                // Per "Auto Trend Trader Documents - Version 1.06C", define HHLL6_Length according to PivotLength
                HHLL6_Length = PivotLength;
                HHLL5_Length = HHLL6_Length - 1;

            }

            double fRet;

            protected override bool CalcBar()
            {
                // ++++++++++++++++++++++++++++++++++++++  SN_PAC_MTF  ++++++++++++++++++++++++++++++++++++++++++
                if (Bars.Status == EBarState.Close)
                {

                    // Combine ATR and PAC functions into one for computation efficiency
                    // Just share the variable with main code, no need to pass variable
                    fRet = SN_ATR_and_PAC_MTF_FN();
                }
                // ++++++++++++++++++++++++++++++++++++++  end of SN_PAC_MTF_Function  ++++++++++++++++++++++++++

                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 
                // ++++++++++++++++++++++++++++++++++++++++++++  PLOTS  +++++++++++++++++++++++++++++++++++++++++ 
                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 

                for (int plotIdx = 0; plotIdx < numOfTFs; plotIdx++)
                {
                    // 5 Min PLOTS
                    // -----------------------------------------5 Min PLOTS ----------------------------------
                    if (displays[plotIdx] == true && plotIdx == TF_5M)
                    {

                        PAC_5_UpperVal.Value = 0.0;
                        PAC_5_LowerVal.Value = 0.0;

                        if (vBDs[plotIdx][0] == 1)
                        {
                            // Forces InActive Side of PriceAction Channel to expand
                            vT_1s[plotIdx].Value = Math.Max(Bars.High[0], vT_1s[plotIdx][1]);

                            // Plots
                            if (vBDs[plotIdx][1] == -1)
                            {
                                // Comment out plot in function
                                // upperPlots[plotIdx].Set(1, vT_1s[plotIdx][1], InActive_Color[plotIdx], InActive_Width);

                                // for _SNPriceActionChannelMTF023f
                                // Cannot set PAC_5_UpperVal[1], use [0], need to check plot
                                // Use SN solution: Highest(high,6), *** Important, for 5 min only
                                // PAC_5_UpperVal.Value = Bars.High.Highest(6);

                                // for PAC_025
                                PAC_5_UpperVal.Value = vT_1s[plotIdx][0];
                                PAC_5_UpperAct.Value = false;
                            }
                            else
                            {
                                // Comment out plot in function
                                // upperPlots[plotIdx].Set(0, vT_1s[plotIdx][0], InActive_Color[plotIdx], InActive_Width);
                                PAC_5_UpperVal.Value = vT_1s[plotIdx][0];
                                PAC_5_UpperAct.Value = false;
                            }

                            // Comment out plot in function
                            ; // lowerPlots[plotIdx].Set(0, vB_1s[plotIdx][0], Up_Color[plotIdx], Active_Width);
                            PAC_5_LowerVal.Value = vB_1s[plotIdx][0];
                            PAC_5_LowerAct.Value = true;
                        }
                        else if (vBDs[plotIdx][0] == -1)
                        {
                            // Forces InActive Side of PriceAction Channel to expand
                            vB_1s[plotIdx].Value = Math.Min(Bars.Low[0], vB_1s[plotIdx][1]);

                            // Plots
                            // Comment out plot in function
                            // upperPlots[plotIdx].Set(0, vT_1s[plotIdx][0], Dn_Color[plotIdx], Active_Width);
                            PAC_5_UpperVal.Value = vT_1s[plotIdx][0];
                            PAC_5_UpperAct.Value = true;

                            if (vBDs[plotIdx][1] == 1)
                            {
                                // Comment out plot in function
                                // lowerPlots[plotIdx].Set(1, vB_1s[plotIdx][1], InActive_Color[plotIdx], InActive_Width);

                                // for _SNPriceActionChannelMTF023f
                                // Cannot set PAC_5_LowerVal[1], use [0], need to check plot
                                // Use SN solution: Lowest(Low,6), *** Important, for 5 min only
                                // PAC_5_LowerVal.Value = Bars.Low.Lowest(6);

                                // for PAC_025
                                PAC_5_LowerVal.Value = vB_1s[plotIdx][0];
                                PAC_5_LowerAct.Value = false;
                            }
                            else
                            {
                                // Comment out plot in function
                                // lowerPlots[plotIdx].Set(0, vB_1s[plotIdx][0], InActive_Color[plotIdx], InActive_Width);
                                PAC_5_LowerVal.Value = vB_1s[plotIdx][0];
                                PAC_5_LowerAct.Value = false;
                            }

                        }

                    }

                    // Other PLOTS (not 5 Min)
                    // -----------------------------------------60 Min PLOTS ----------------------------------
                    if (displays[plotIdx] == true && plotIdx != TF_5M)
                    {

                        if (vBDs[plotIdx][0] == 1)
                        {
                            // Forces InActive Side of PriceAction Channel to expand
                            vT_1s[plotIdx].Value = Math.Max(Bars.High[0], vT_1s[plotIdx][1]);

                            // Plots
                            if (vBDs[plotIdx][1] == -1)
                            {
                                // Comment out plot in function
                                ;// upperPlots[plotIdx].Set(1, vT_1s[plotIdx][1], InActive_Color[plotIdx]);
                            }
                            else
                            {
                                // Comment out plot in function
                                ;// upperPlots[plotIdx].Set(0, vT_1s[plotIdx][0], InActive_Color[plotIdx]);
                            }

                            switch (plotIdx)
                            {
                                case TF_10M:
                                    PAC_10_UpperAct.Value = false;
                                    PAC_10_LowerAct.Value = true;
                                    break;

                                case TF_15M:
                                    PAC_15_UpperAct.Value = false;
                                    PAC_15_LowerAct.Value = true;
                                    break;

                                case TF_20M:
                                    PAC_20_UpperAct.Value = false;
                                    PAC_20_LowerAct.Value = true;
                                    break;

                                case TF_30M:
                                    PAC_30_UpperAct.Value = false;
                                    PAC_30_LowerAct.Value = true;
                                    break;

                                case TF_60M:
                                    PAC_60_UpperAct.Value = false;
                                    PAC_60_LowerAct.Value = true;
                                    break;

                                case TF_120M:
                                    PAC_120_UpperAct.Value = false;
                                    PAC_120_LowerAct.Value = true;
                                    break;

                                case TF_240M:
                                    PAC_240_UpperAct.Value = false;
                                    PAC_240_LowerAct.Value = true;
                                    break;

                                case TF_Day:
                                    PAC_Day_UpperAct.Value = false;
                                    PAC_Day_LowerAct.Value = true;
                                    break;
                            }

                            // Comment out plot in function
                            ;// lowerPlots[plotIdx].Set(0, vB_1s[plotIdx][0], Up_Color[plotIdx]);
                        }
                        else if (vBDs[plotIdx][0] == -1)
                        {
                            // Forces InActive Side of PriceAction Channel to expand
                            vB_1s[plotIdx].Value = Math.Min(Bars.Low[0], vB_1s[plotIdx][1]);

                            // Plots
                            // Comment out plot in function
                            ;// upperPlots[plotIdx].Set(0, vT_1s[plotIdx][0], Dn_Color[plotIdx]);

                            if (vBDs[plotIdx][1] == 1)
                            {
                                // Comment out plot in function
                                // lowerPlots[plotIdx].Set(1, vB_1s[plotIdx][1], InActive_Color[plotIdx]);
                            }
                            else
                            {
                                // Comment out plot in function
                                ;// lowerPlots[plotIdx].Set(0, vB_1s[plotIdx][0], InActive_Color[plotIdx]);
                            }

                            switch (plotIdx)
                            {
                                case TF_10M:
                                    PAC_10_UpperAct.Value = true;
                                    PAC_10_LowerAct.Value = false;
                                    break;

                                case TF_15M:
                                    PAC_15_UpperAct.Value = true;
                                    PAC_15_LowerAct.Value = false;
                                    break;

                                case TF_20M:
                                    PAC_20_UpperAct.Value = true;
                                    PAC_20_LowerAct.Value = false;
                                    break;

                                case TF_30M:
                                    PAC_30_UpperAct.Value = true;
                                    PAC_30_LowerAct.Value = false;
                                    break;

                                case TF_60M:
                                    PAC_60_UpperAct.Value = true;
                                    PAC_60_LowerAct.Value = false;
                                    break;

                                case TF_120M:
                                    PAC_120_UpperAct.Value = true;
                                    PAC_120_LowerAct.Value = false;
                                    break;

                                case TF_240M:
                                    PAC_240_UpperAct.Value = true;
                                    PAC_240_LowerAct.Value = false;
                                    break;

                                case TF_Day:
                                    PAC_Day_UpperAct.Value = true;
                                    PAC_Day_LowerAct.Value = false;
                                    break;
                            }
                        }

                        switch (plotIdx)
                        {
                            case TF_10M:
                                PAC_10_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_10_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;

                            case TF_15M:
                                PAC_15_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_15_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;

                            case TF_20M:
                                PAC_20_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_20_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;

                            case TF_30M:
                                PAC_30_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_30_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;

                            case TF_60M:
                                PAC_60_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_60_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;

                            case TF_120M:
                                PAC_120_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_120_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;

                            case TF_240M:
                                PAC_240_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_240_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;

                            case TF_Day:
                                PAC_Day_UpperVal.Value = vT_1s[plotIdx].Value;
                                PAC_Day_LowerVal.Value = vB_1s[plotIdx].Value;
                                break;
                        }

                    }
                }

                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 
                // +++++++++++++++++++++++++++++++++++++  end of PLOTS  +++++++++++++++++++++++++++++++++++++++++ 
                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 

                haveValidPAC_5.Value = haveValidPAC[TF_5M][0];
                haveValidPAC_10.Value = haveValidPAC[TF_10M][0];
                haveValidPAC_15.Value = haveValidPAC[TF_15M][0];
                haveValidPAC_20.Value = haveValidPAC[TF_20M][0];
                haveValidPAC_30.Value = haveValidPAC[TF_30M][0];
                haveValidPAC_60.Value = haveValidPAC[TF_60M][0];
                haveValidPAC_120.Value = haveValidPAC[TF_120M][0];
                haveValidPAC_240.Value = haveValidPAC[TF_240M][0];
                haveValidPAC_Day.Value = haveValidPAC[TF_Day][0];


                return true;
            }

            // Faster code and lower overhead to keep the function with the indicator
            // ATR and PAC Function, combined together for faster computation
            private double SN_ATR_and_PAC_MTF_FN()
            {
                // Just call this function on EBarState.Close
                // EL Time
                int elT = 100 * (Bars.Time[0].Hour) + Bars.Time[0].Minute;
                // Bar Closed for this TF
                bool barClosedTF;

                // Loop through each TF
                for (int idxTF = 0; idxTF < numOfTFs; idxTF++)
                {
                    // initialize barNums
                    if (Bars.CurrentBar == 1)
                    {
                        barNums[idxTF].Value = 0;
                    }

                    // Define Bar Closed Condition for each TF
                    // Note that the bar end times defined do NOT match with the MC.Net IB EUR.USD chart bar end time
                    barClosedTF = false;
                    switch (idxTF)
                    {
                        case TF_10M:
                            barClosedTF = ((vCategory == 2 && (elT == 1600 || Mod(elT, 100) == 0 || Mod(elT, 100) == 10 || Mod(elT, 100) == 20 || Mod(elT, 100) == 30 || Mod(elT, 100) == 40 || Mod(elT, 100) == 50))
                                || (vCategory == 4 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 10 || Mod(elT, 100) == 20 || Mod(elT, 100) == 30 || Mod(elT, 100) == 40 || Mod(elT, 100) == 50))
                                || (vCategory == 0 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 10 || Mod(elT, 100) == 20 || Mod(elT, 100) == 30 || Mod(elT, 100) == 40 || Mod(elT, 100) == 50))
                                || ((vCategory == 9 || vCategory == 12) && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 10 || Mod(elT, 100) == 20 || Mod(elT, 100) == 30 || Mod(elT, 100) == 40 || Mod(elT, 100) == 50)));
                            break;

                        case TF_15M:
                            barClosedTF = ((vCategory == 2 && (elT == 1600 || Mod(elT, 100) == 0 || Mod(elT, 100) == 15 || Mod(elT, 100) == 30 || Mod(elT, 100) == 45))
                                || (vCategory == 4 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 15 || Mod(elT, 100) == 30 || Mod(elT, 100) == 45))
                                || (vCategory == 0 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 15 || Mod(elT, 100) == 30 || Mod(elT, 100) == 45))
                                || ((vCategory == 9 || vCategory == 12) && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 15 || Mod(elT, 100) == 30 || Mod(elT, 100) == 45)));
                            break;

                        case TF_20M:
                            barClosedTF = ((vCategory == 2 && (elT == 1600 || Mod(elT, 100) == 10 || Mod(elT, 100) == 30 || Mod(elT, 100) == 50))
                                || (vCategory == 4 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 20 || Mod(elT, 100) == 40))
                                || (vCategory == 0 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 20 || Mod(elT, 100) == 40))
                                || ((vCategory == 9 || vCategory == 12) && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 20 || Mod(elT, 100) == 40)));
                            break;

                        case TF_30M:
                            barClosedTF = ((vCategory == 2 && (elT == 1600 || Mod(elT, 100) == 0 || Mod(elT, 100) == 30))
                                || (vCategory == 4 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 30))
                                || (vCategory == 0 && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 30))
                                || ((vCategory == 9 || vCategory == 12) && (elT == End_Time || Mod(elT, 100) == 0 || Mod(elT, 100) == 30)));
                            break;

                        case TF_60M:
                            barClosedTF = ((vCategory == 2 && (elT == 1600 || Mod(elT, 100) == 30))
                                || (vCategory == 4 && (elT == End_Time || Mod(elT, 100) == 0))
                                || (vCategory == 0 && (elT == End_Time || Mod(elT, 100) == 0))
                                // Make 1800 as 60 min Bar end time for IB
                                || ((vCategory == 9 || vCategory == 12) && (elT == End_Time || elT == 1800 || Mod(elT, 100) == 0)));
                            break;

                        case TF_120M:
                            barClosedTF = ((vCategory == 2 && (elT == 1130 || elT == 1330 || elT == 1530 || elT == 1600 || elT == End_Time))
                                || (vCategory == 4 && (elT == 0 || elT == 200 || elT == 400 || elT == 600 || elT == 800 || elT == 1000 || elT == 1200 || elT == 1400 || elT == 1600 || elT == End_Time || elT == 2000 || elT == 2200))
                                || (vCategory == 0 && (elT == 0 || elT == 200 || elT == 400 || elT == 600 || elT == 800 || elT == 1000 || elT == 1200 || elT == 1400 || elT == 1600 || elT == End_Time || elT == 2000 || elT == 2200))
                                // Make x00 as 120 min end time instead of x30 for IB
                                || ((vCategory == 9 || vCategory == 12) && (elT == 100 || elT == 300 || elT == 500 || elT == 700 || elT == 900 || elT == 1100 || elT == 1300 || elT == 1500 || elT == End_Time || elT == 1900 || elT == 2100 || elT == 2300)));
                            break;

                        case TF_240M:
                            barClosedTF = ((vCategory == 2 && (elT == 1330 || elT == 1600 || elT == End_Time))
                                || (vCategory == 4 && (elT == 200 || elT == 600 || elT == 1000 || elT == 1400 || elT == End_Time || elT == 2200))
                                || (vCategory == 0 && (elT == 200 || elT == 600 || elT == 1000 || elT == 1400 || elT == End_Time || elT == 2200))
                                // IB Cash (which is IB Forx), vCategory == 9; TS Forex, vCategory == 12
                                // Make x00 as 240 min end time instead of x30 for IB
                                || ((vCategory == 9 || vCategory == 12) && (elT == 100 || elT == 500 || elT == 900 || elT == 1300 || elT == End_Time || elT == 2100)));
                            break;

                        case TF_Day:
                            barClosedTF = ((vCategory == 2 && (elT == 1600 || elT == End_Time))
                                || (vCategory == 4 && elT == End_Time)
                                || (vCategory == 0 && elT == End_Time)
                                || ((vCategory == 9 || vCategory == 12) && elT == End_Time));
                            break;

                        case TF_5M:
                        default:
                            // default & case 1 are the same
                            barClosedTF = (Bars.CurrentBar > 5);
                            break;
                    }

                    if (idxTF != TF_Day && barClosedTF)
                    {
                        barNums[idxTF].Value += 1;
                        // For intraday TF
                        // Price Calculation Bars
                        int pcb = priceCalcBars[idxTF];

                        // +++++++++++++++++++++ Store Price Values ++++++++++++++++++++++++ 	
                        // Open
                        vOpens[idxTF].Value = Bars.Open[pcb - 1];

                        // Close 	
                        vCloses[idxTF].Value = Bars.Close[0];

                        // High 
                        vHighs[idxTF].Value = Bars.High.Highest(pcb);

                        // Low 
                        vLows[idxTF].Value = Bars.Low.Lowest(pcb);

                        // Open + Close 
                        vOCs[idxTF].Value = 0.5 * (vOpens[idxTF][0] + vCloses[idxTF][0]);

                        // High Low Open Close 
                        vHLOCs[idxTF].Value = 0.25 * (vHighs[idxTF][0] + vLows[idxTF][0]
                            + vOpens[idxTF][0] + vCloses[idxTF][0]);
                    }

                    if (idxTF == TF_Day)
                    {
                        // for Day TF
                        // +++++++++++++++++++++ Store Price Values ++++++++++++++++++++++++ 	
                        // Daily Open 
                        if (HP_SessionStartBarMnt_f())
                        {
                            // Open 
                            vOpens[idxTF].Value = Bars.Open[0];

                            Value30.Value = Bars.High[0];
                            Value31.Value = Bars.Low[0];
                        }
                        else
                        // Daily Close 
                        if (elT == End_Time)
                        {
                            barNums[idxTF].Value += 1;
                            // Close 	
                            vCloses[idxTF].Value = Bars.Close[0];
                            // High 
                            vHighs[idxTF].Value = Math.Max(Bars.High[0], Value30[1]);

                            // Low 
                            vLows[idxTF].Value = Math.Min(Bars.Low[0], Value31[1]);

                            // Open Close 
                            vOCs[idxTF].Value = 0.5 * (vOpens[idxTF][0] + vCloses[idxTF][0]);

                            // High Low Open Close 
                            vHLOCs[idxTF].Value = 0.25 * (vHighs[idxTF][0] + vLows[idxTF][0]
                                + vOpens[idxTF][0] + vCloses[idxTF][0]);
                        }
                        else
                        // Daily High && Daily Low from 5 min bars 
                        {
                            Value30.Value = Math.Max(Bars.High[0], Value30[1]);
                            Value31.Value = Math.Min(Bars.Low[0], Value31[1]);
                        }
                    }

                    if (barClosedTF)
                    {
                        /*
                        // maintain trialing HHLL6_Length value
                        // _HP_TestLog6_f(20170401, 20170828, 1, (double)qH5s[idxTF].Count, (double)qH6s[idxTF].Count, (double)HHLL5_Length, (double)HHLL6_Length, 5.0, 6.0);
                        while (qH5s[idxTF].Count >= HHLL5_Length)
                        {
                            qH5s[idxTF].Dequeue();
                        }
                        while (qH6s[idxTF].Count >= HHLL6_Length)
                        {
                            qH6s[idxTF].Dequeue();
                        }
                        // Add the latest High
                        qH5s[idxTF].Enqueue(vHighs[idxTF][0]);
                        qH6s[idxTF].Enqueue(vHighs[idxTF][0]);

                        while (qL5s[idxTF].Count >= HHLL6_Length)
                        {
                            qL5s[idxTF].Dequeue();
                        }
                        while (qL6s[idxTF].Count >= HHLL6_Length)
                        {
                            qL6s[idxTF].Dequeue();
                        }
                        // Add the latest Low
                        qL5s[idxTF].Enqueue(vLows[idxTF][0]);
                        qL6s[idxTF].Enqueue(vLows[idxTF][0]);
                        */
                        /*
                        // Calc Highest_?_5, Highest_?_6
                        vHighest5[idxTF].Value = qH5s[idxTF].Max();
                        vHighest6[idxTF].Value = qH6s[idxTF].Max();
                        // Calc Lowest_?_5, Lowest_?_6
                        vLowest5[idxTF].Value = qL5s[idxTF].Min();
                        vLowest6[idxTF].Value = qL6s[idxTF].Min();
                        */
                        vHighest5[idxTF].Value = vHighs[idxTF].Highest(HHLL5_Length);
                        vHighest6[idxTF].Value = vHighs[idxTF].Highest(HHLL6_Length);

                        vLowest5[idxTF].Value = vLows[idxTF].Lowest(HHLL5_Length);
                        vLowest6[idxTF].Value = vLows[idxTF].Lowest(HHLL6_Length);

                        // for SN_MTF_ATR
                        //------------------------- PAL Calculations Required -------------------------
                        /*

                        while (qRanges[idxTF].Count >= ATR_Length)
                        {
                            qRanges[idxTF].Dequeue();
                        }
                        qRanges[idxTF].Enqueue(vHighs[idxTF][0] - vLows[idxTF][0]);
                        // ATR_
                        vATRs[idxTF].Value = qRanges[idxTF].Average();
                        */

                        if (barNums[idxTF][0] >= ATR_Length)
                            haveValidPAC[idxTF].Value = 1;

                        vRanges[idxTF].Value = (vHighs[idxTF][0] - vLows[idxTF][0]);
                        vATRs[idxTF].Value = vRanges[idxTF].Average(ATR_Length);

                        // New ATR
                        if (barNums[idxTF][0] > 1)
                        {
                            ATR2s[idxTF].Value = (PACLengthXPACSpeed * (vHighs[idxTF][0] - vLows[idxTF][0]))
                                    + ((1.0 - PACLengthXPACSpeed) * ATR2s[idxTF][1]);
                        }
                        else if (barNums[idxTF][0] == 1)
                        {
                            ATR2s[idxTF].Value = (vHighs[idxTF][0] - vLows[idxTF][0]);
                        }

                        // R_
                        if (ATR_Type == 2)
                        {
                            vRs[idxTF].Value = ATR2s[idxTF][0] * ATR_Multiple[idxTF];
                        }
                        else // if (ATR_Type == 1)
                        {
                            vRs[idxTF].Value = vATRs[idxTF][0] * ATR_Multiple[idxTF];
                        }

                        if (barNums[idxTF][0] >= ATR_Length)
                        {

                            // +++++++++++++++++++++ Reset Flags ++++++++++++++++++++++++
                            vNewSupports[idxTF].Value = false;
                            vNewResistances[idxTF].Value = false;

                            //---------------------------- PIP CALCULATIONS -------------------------------
                            // Use Range
                            // replace with PIP boost
                            vMinimum_Ranges[idxTF].Value = RangeMultiple * vRs[idxTF][0];

                            // Used for extra price distanced add to PriceAction Channel lines
                            // replace with PA_Buffer
                            vDistance_Buffers[idxTF].Value = DistanceMultiple * vRs[idxTF][0];

                            vNew_Step_Buffers[idxTF].Value = NewStepMultiple * vRs[idxTF][0];

                            // ------------------------------- Speed and Length Calculations -------------------------------
                            switch (PriceInput)
                            {
                                case 2:
                                    vPrices[idxTF].Value = vOCs[idxTF][0];
                                    break;

                                case 3:
                                    vPrices[idxTF].Value = vHLOCs[idxTF][0];
                                    break;

                                case 1:
                                default:
                                    // default & case 1 are the same
                                    vPrices[idxTF].Value = vCloses[idxTF][0];
                                    break;
                            }

                            //------------------------------- PAL Calculations -------------------------------
                            vPAL_1s[idxTF].Value = vPALs[idxTF][0];
                            vPALs[idxTF].Value = (PACLengthXPACSpeed * vPrices[idxTF][0])
                                + ((1 - PACLengthXPACSpeed) * vPAL_1s[idxTF][0]);

                            if (Math.Abs(vPALs[idxTF][0] - vPAL_1s[idxTF][0]) < pointBuffer)
                                vPALs[idxTF].Value = vPAL_1s[idxTF][0];

                            //------------------------------- PAL Plot Color -------------------------------
                            if (vPALs[idxTF][0] > vPAL_1s[idxTF][0])
                                vColor1s[idxTF].Value = 1;
                            else if (vPALs[idxTF][0] < vPAL_1s[idxTF][0])
                                vColor1s[idxTF].Value = -1;

                            //-------------------------------UPPER PAC SETUP-------------------------------
                            // New Resistance
                            if (vColor1s[idxTF][1] == 1 && vColor1s[idxTF][0] == -1
                                && vT_1s[idxTF][0] - vB_1s[idxTF][0] > vMinimum_Ranges[idxTF][0]
                                && vHighest5[idxTF][0] - vLowest5[idxTF][0] > vMinimum_Ranges[idxTF][0])
                            // Remove this condition 20170829
                            // vCloses[idxTF][0] < vCloses[idxTF][1]
                            {
                                vNewResistances[idxTF].Value = true;
                            }

                            //---------------------------LOWER PAC SETUP-------------------------------
                            if (vColor1s[idxTF][1] == -1 && vColor1s[idxTF][0] == 1
                                && vT_1s[idxTF][0] - vB_1s[idxTF][0] > vMinimum_Ranges[idxTF][0]
                                && vHighest5[idxTF][0] - vLowest5[idxTF][0] > vMinimum_Ranges[idxTF][0])
                            // Remove this condition 20170829
                            // && vCloses[idxTF][0] > vCloses[idxTF][1]
                            {
                                vNewSupports[idxTF].Value = true;
                            }

                            //----------------------------------BIAS DIRECTION----------------------------------
                            if (vCloses[idxTF][0] > vT_1s[idxTF][0])
                            {
                                vBDs[idxTF].Value = 1;
                            }
                            else
                            //-------------------------------{ UP Bias Direction }------------------------------
                            if (vCloses[idxTF][0] < vB_1s[idxTF][0])
                            {
                                vBDs[idxTF].Value = -1;
                            }

                            //-------------------------------{ DOWN Bias Direction }-------------------------------

                            //----------------------------------New Support and Resistance ----------------------------------
                            // New Support Occurred
                            if ((vNewSupports[idxTF][0] == true && vBDs[idxTF][0] == -1)
                                || (vNewSupports[idxTF][0] == true && vBDs[idxTF][0] == 1
                                    && vLows[idxTF][0] - vNew_Step_Buffers[idxTF][0] > vB_1s[idxTF][0]))
                            {
                                vB_3s[idxTF].Value = vB_2s[idxTF][0];
                                vB_2s[idxTF].Value = vB_1s[idxTF][0];
                                vB_1s[idxTF].Value = vLowest6[idxTF][0] - vDistance_Buffers[idxTF][0];
                            }
                            // New Resistance Occurred
                            if ((vNewResistances[idxTF][0] == true && vBDs[idxTF][0] == 1)
                                 || (vNewResistances[idxTF][0] == true && vBDs[idxTF][0] == -1
                                    && vHighs[idxTF][0] + vNew_Step_Buffers[idxTF][0] < vT_1s[idxTF][0]))
                            {
                                vT_3s[idxTF].Value = vT_2s[idxTF][0];
                                vT_2s[idxTF].Value = vT_1s[idxTF][0];
                                vT_1s[idxTF].Value = vHighest6[idxTF][0] + vDistance_Buffers[idxTF][0];
                            }

                            //---------------------------------- Step Reversal Override ----------------------------------
                            if (vBDs[idxTF][0] == 1 && vB_1s[idxTF][0] < vB_2s[idxTF][0])
                            {
                                vB_1s[idxTF].Value = vB_2s[idxTF][0];
                            }

                            if (vBDs[idxTF][0] == -1 && vT_1s[idxTF][0] > vT_2s[idxTF][0])
                            {
                                vT_1s[idxTF].Value = vT_2s[idxTF][0];
                            }


                        }

                    }
                }

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

        }
    }
}
