/*
_SN_PAL130_f 20181003	2000
* Add 10 min

_SN_PAL120_f    20180529	1400
* For TS Forex, it is Forex, the Category number is 12.
* For IB Forex, it is Cash, the Category number is 9.

_SN_PAL110_f    20180117
PAL changes
    // for 5 min, no changes required
    If absvalue( PAL_5 - PAL_5_1 ) < ( TickBuffer * OneTick ) then 
		    PAL_5 = PAL_5_1 ;

    // changes for 15 min
    If absvalue( PAL_15 - PAL_15_1 ) < ( TickBuffer * OneTick* 3) then 
		    PAL_15 = PAL_15_1 ;

    // changes for 20 min OneTick * 4
    // changes for 30 min OneTick * 6
    // changes for 60 min OneTick * 12
    // changes for 120 min OneTick * 24
    // changes for 240 min OneTick * 48
    // changes for daily OneTick * 100
    One Option to switch between the new settings above and original settings [SettingOriginalOrNew = 1 or 2 ]

_SN_PAL100_f    20180115
* Original PAL Function

After temp dev code _SN_PAL_VMA_FN_Indicator_091
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PowerLanguage.Function
{
    public sealed class _SN_PAL130_f : FunctionSimple<System.Double>
    {
        // Data1 only
        public _SN_PAL130_f(CStudyControl _master) : base(_master) { }

        public int Speed_Input { get; set; }
        public int Length_Input { get; set; }
        public int TickBuffer { get; set; }
        public int Price_Input { get; set; }
        // public int Price_Trend_On { get; set; }
        public int SettingOriginalOrNew { get; set; }
        
        // Number of Time Frames
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

        // int numbers for each Upper_Active_Color_Name and Lower_Active_Color_Name
        private const int Upper_Active_ClrNum = 1;
        private const int Lower_Active_ClrNum = 2;

        // private List<VariableSeries<double>> vPALs = new List<VariableSeries<double>>();
        // Sufficient to use VariableObject for vPALs
        private List<VariableObject<double>> vPALs = new List<VariableObject<double>>();
        private List<VariableObject<double>> vPAL1s = new List<VariableObject<double>>();
        private List<VariableSeries<int>> vActiveClrNum = new List<VariableSeries<int>>();
        private VariableObject<int> Value40, PAL_Width;

        private int[] priceCalcBars;
        private List<VariableSeries<double>> vPrices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vOpens = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vCloses = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vHighs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vLows = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vOCs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vHLOCs = new List<VariableSeries<double>>();

        private VariableSeries<double> Value30, Value31;

        private double OnePip, OneTick;
        private int vCategory, End_Time;
        private double Speed, Length, LengthXSpeed, pointBuffer;
        private double[] pointBuffers;

        public ISeries<double> PAL_5 { get { return vPALs[0]; } }
        public ISeries<double> PAL_10 { get { return vPALs[1]; } }
        public ISeries<double> PAL_15 { get { return vPALs[2]; } }
        public ISeries<double> PAL_20 { get { return vPALs[3]; } }
        public ISeries<double> PAL_30 { get { return vPALs[4]; } }
        public ISeries<double> PAL_60 { get { return vPALs[5]; } }
        public ISeries<double> PAL_120 { get { return vPALs[6]; } }
        public ISeries<double> PAL_240 { get { return vPALs[7]; } }
        public ISeries<double> PAL_Day { get { return vPALs[8]; } }

        // public ISeries<int> vActiveClrNum_5 { get { return vActiveClrNum[0]; } }

        public ISeries<double> PAL1_5 { get { return vPAL1s[0]; } }
        public ISeries<double> PAL1_10 { get { return vPAL1s[1]; } }
        public ISeries<double> PAL1_15 { get { return vPAL1s[2]; } }
        public ISeries<double> PAL1_20 { get { return vPAL1s[3]; } }
        public ISeries<double> PAL1_30 { get { return vPAL1s[4]; } }
        public ISeries<double> PAL1_60 { get { return vPAL1s[5]; } }
        public ISeries<double> PAL1_120 { get { return vPAL1s[6]; } }
        public ISeries<double> PAL1_240 { get { return vPAL1s[7]; } }
        public ISeries<double> PAL1_Day { get { return vPAL1s[8]; } }

        protected override void Create()
        {
            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfTFs; j++)
            {
                // vPALs.Add(new VariableSeries<double>(this));
                vPALs.Add(new VariableObject<double>(this));
                vPAL1s.Add(new VariableObject<double>(this));
                vActiveClrNum.Add(new VariableSeries<int>(this));
            }

            Value40 = new VariableObject<int>(this);
            PAL_Width = new VariableObject<int>(this);

            // trend_Tool_Price_Input1 = new _SN_Trend_Tool_Price_Input(this);

            // for SN_PAL_VMA_MTF_FN
            priceCalcBars = new int[] { 1, 2, 3, 4, 6, 12, 24, 48, 288 };

            for (int j = 0; j < numOfTFs; j++)
            {
                vPrices.Add(new VariableSeries<double>(this));
                vOpens.Add(new VariableSeries<double>(this));
                vCloses.Add(new VariableSeries<double>(this));
                vHighs.Add(new VariableSeries<double>(this));
                vLows.Add(new VariableSeries<double>(this));
                vOCs.Add(new VariableSeries<double>(this));
                vHLOCs.Add(new VariableSeries<double>(this));
            }

            Value30 = new VariableSeries<double>(this);
            Value31 = new VariableSeries<double>(this);
        }

        string filePath;

        protected override void StartCalc()
        {
            // trend_Tool_Price_Input1.Price_Input = this.Price_Input;

            OnePip = (10.0 * Bars.Info.MinMove / Bars.Info.PriceScale);
            OneTick = (Bars.Info.MinMove / Bars.Info.PriceScale);

            vCategory = (int)Bars.Info.Category;
            // Need to include 9
            if (vCategory != 0 && vCategory != 2 && vCategory != 9 && vCategory != 12)
                vCategory = 4;

            Speed = ((int)Speed_Input) / 100.0;
            Length = 2.0 / (((double)Length_Input) + 1.0);
            LengthXSpeed = Length * Speed;
            pointBuffer = ((double)TickBuffer) * OneTick;

            if (SettingOriginalOrNew == 1)
            {
                pointBuffers = new double[] { OneTick, OneTick, OneTick, OneTick, OneTick,
                    OneTick, OneTick, OneTick, OneTick };
            }
            else
            {
                pointBuffers = new double[] { OneTick, OneTick * 2, OneTick * 3, OneTick * 4, OneTick * 6,
                    OneTick * 12, OneTick * 24, OneTick * 48, OneTick * 100 };
            }

            Output.WriteLine("StartCalc: 900: pointBuffer {0} TickBuffer {1} OneTick {2}", pointBuffer, TickBuffer, OneTick);

            End_Time = 100 * (Bars.Sessions[0].EndTime.Hours) + Bars.Sessions[0].EndTime.Minutes;
        }

        protected override double CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                // EL Time
                int elT = 100 * (Bars.Time[0].Hour) + Bars.Time[0].Minute;

                // Bar Closed for this TF
                bool barClosedTF;
                List<double> highTmp;
                List<double> lowTmp;

                // Loop through each TF
                for (int idxTF = 0; idxTF < numOfTFs; idxTF++)
                {

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
                        // For intraday TF
                        // Price Calculation Bars
                        int pcb = priceCalcBars[idxTF];

                        // +++++++++++++++++++++ Store Price Values ++++++++++++++++++++++++ 	
                        // Open
                        vOpens[idxTF].Value = Bars.Open[pcb - 1];

                        // Close 	
                        vCloses[idxTF].Value = Bars.Close[0];

                        // High 
                        highTmp = new List<double>(pcb);
                        for (int j = 0; j < pcb; j++)
                        {
                            highTmp.Add(Bars.High[j]);
                        }
                        vHighs[idxTF].Value = highTmp.Max();
                        highTmp.Clear();

                        // Low 
                        lowTmp = new List<double>(pcb);
                        for (int j = 0; j < pcb; j++)
                        {
                            lowTmp.Add(Bars.Low[j]);
                        }
                        vLows[idxTF].Value = lowTmp.Min();
                        lowTmp.Clear();

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
                        //------------------------------- Speed && Length Calculations -------------------------------
                        switch (Price_Input)
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
                        // Assume computation is done on Closed Bar!!!
                        vPAL1s[idxTF].Value = vPALs[idxTF].Value;

                        vPALs[idxTF].Value = (LengthXSpeed * vPrices[idxTF][0])
                            + ((1 - LengthXSpeed) * vPAL1s[idxTF].Value);

                        if (Math.Abs(vPALs[idxTF].Value - vPAL1s[idxTF].Value) < pointBuffers[idxTF])
                            vPALs[idxTF].Value = vPAL1s[idxTF].Value;

                        //------------------------------- PAL Plot Color -------------------------------
                        if (vPALs[idxTF][0] > vPAL1s[idxTF].Value)
                            vActiveClrNum[idxTF].Value = Upper_Active_ClrNum;
                        else if (vPALs[idxTF][0] < vPAL1s[idxTF].Value)
                            vActiveClrNum[idxTF].Value = Lower_Active_ClrNum;
                    }
                }

                /*
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
                */
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
