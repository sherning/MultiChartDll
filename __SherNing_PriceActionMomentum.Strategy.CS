using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    public class __SherNing_PriceActionMomentum : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Price Action Momentum
        // Requirements : 1. Scale-in 3 Entries (NextBar Mkt Entries)
        //
        // Type         : Strategy (IOG.Disabled)
        // Version      : v1.0
        // Date         : 06 - Sep - 2020
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
         *  1.0.2 - Research
         *        - Frequency of pullbacks on different timeframes, symbols and intervals
         *        
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  1.0.1 - Requirements
         *        - Import and check key functions
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Inputs
        [Input] public string PamaSettings { get; set; }
        [Input] public int PamaLength { get; set; }
        [Input] public int PamaSmooth { get; set; }
        [Input] public string StrategySettings { get; set; }
        [Input] public int ChannelLength { get; set; }

        public __SherNing_PriceActionMomentum(object _ctx) : base(_ctx)
        {
            // default settings
            PamaSettings = "Pama Setting";
            PamaLength = 6;
            PamaSmooth = 10;

            StrategySettings = "Strategy Settings";
            ChannelLength = 20;
        }

        #endregion

        #region References
        PriceActionMovingAverageFunction PamaFunction;
        VariableSeries<double>[] Pama;
        protected override void Create()
        {
            PamaFunction = new PriceActionMovingAverageFunction(this);

            Pama = new VariableSeries<double>[4];
            for (int i = 0; i < Pama.Length; i++)
                Pama[i] = new VariableSeries<double>(this);

            Output.Clear();
        }
        #endregion

        #region Fields

        // research fields
        int PivotFrequency;
        int PositivePivotFreq;
        int NegativePivotFreq;
        int LongStopHit, ShortStopHit;
        List<Data> PositivePivotList, NegativePivotList;


        protected override void StartCalc()
        {
            PamaFunction._Length = PamaLength;
            PamaFunction._Smooth = PamaSmooth;
            PamaFunction._PriceType = 1;

            // reseach fields
            PivotFrequency = 0;
            PositivePivotFreq = NegativePivotFreq = 0;

            PositivePivotList = new List<Data>();
            NegativePivotList = new List<Data>();

            LongStopHit = ShortStopHit = 0;
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            if (Bars.CurrentBar == 1)
            {
                Pama[1] = PamaFunction.Pama1;
                Pama[2] = PamaFunction.Pama2;
                Pama[3] = PamaFunction.Pama3;
            }

            PamaFunction.Call();

            // research
            //FrequencyOfPivots();
            FreqOfStopLossHit(5, 50);

            PlotStrategyInfo();
        }
        #endregion

        #region Reseach
        private void FrequencyOfPivots()
        {
            if (PositivePivot() || NegativePivot())
            {
                PivotFrequency++;

                if (PositivePivot())
                {
                    DrwArrow.Create(new ChartPoint(Bars.Time[1], Bars.Low[1]), false);
                    PositivePivotFreq++;
                }

                if (NegativePivot())
                {
                    DrwArrow.Create(new ChartPoint(Bars.Time[1], Bars.High[1]), true);
                    NegativePivotFreq++;
                }
            }

            if (Bars.LastBarOnChart)
            {
                Print("Pivot Frequency: " + PivotFrequency);
                Print("Positive Pivot Freq: " + PositivePivotFreq);
                Print("Negative Pivot Freq: " + NegativePivotFreq);
            }
        }
        private void FreqOfStopLossHit(int length, int period)
        {
            //if (Bars.Time[0] < new DateTime(2020,08,31)) return;
            if (PositivePivot() || NegativePivot())
            {
                PivotFrequency++;
            }

            if (PositivePivot())
            {
                PositivePivotList.Add(new Data(LowestLowCurrent(length), period));
                //DrwArrow.Create(new ChartPoint(Bars.Time[1], Bars.Low[1]), false);
                PositivePivotFreq++;
            }

            if (NegativePivot())
            {
                NegativePivotList.Add(new Data(HighestHighCurrent(length), period));
                //DrwArrow.Create(new ChartPoint(Bars.Time[1], Bars.High[1]), true);
                NegativePivotFreq++;
            }

            for (int i = 0; i < PositivePivotList.Count; i++)
            {
                // count down
                PositivePivotList[i].BarRemaining--;

                if (PositivePivotList[i].BarRemaining > 0)
                {
                    // check the past bars low
                    if (Bars.Low[0] < PositivePivotList[i].Price
                        && PositivePivotList[i].BarRemaining < (period - 1))
                    {
                        LongStopHit++;
                        //Print("Long Stop Hit!");
                        //Print("Stop Loss Price: " + PositivePivotList[i].Price);
                        //Print("Bars Remaining: " + PositivePivotList[i].BarRemaining);
                        //Output.WriteLine("");
                        PositivePivotList.RemoveAt(i);
                        continue;
                    }
                }

                if (PositivePivotList[i].BarRemaining == 0)
                {
                    PositivePivotList.RemoveAt(i);
                }
            }

            for (int i = 0; i < NegativePivotList.Count; i++)
            {
                // count down
                NegativePivotList[i].BarRemaining--;

                if (NegativePivotList[i].BarRemaining > 0)
                {
                    // check the past bars low
                    if (Bars.High[0] > NegativePivotList[i].Price
                        && NegativePivotList[i].BarRemaining < (period - 1))
                    {
                        ShortStopHit++;
                        //Print("Short Stop Hit!");
                        //Print("Stop Loss Price: " + NegativePivotList[i].Price);
                        //Print("Bars Remaining: " + NegativePivotList[i].BarRemaining);
                        //Output.WriteLine("");
                        NegativePivotList.RemoveAt(i);
                        continue;
                    }
                }

                if (NegativePivotList[i].BarRemaining == 0)
                {
                    NegativePivotList.RemoveAt(i);
                }
            }

            if (Bars.LastBarOnChart)
            {
                Print("Long Stop Hit: " + LongStopHit);
                Print("Short Stop Hit: " + ShortStopHit);
                Print("Positive Pivot Freq: " + PositivePivotFreq);
                Print("Negative Pivot Freq: " + NegativePivotFreq);
                Print("Long Hit Rate: " + (LongStopHit * 100 / PositivePivotFreq));
                Print("Short Hit Rate: " + (ShortStopHit * 100 / NegativePivotFreq));
                Print("Pivot Frequency: " + PivotFrequency);
            }
        }
        private void FreqOfStopLossHitv10(int length, int period)
        {
            if (Bars.Time[0] < new DateTime(2020, 08, 31)) return;

            if (PositivePivot())
            {
                PositivePivotList.Add(new Data(LowestLow(length), period));
                DrwArrow.Create(new ChartPoint(Bars.Time[1], Bars.Low[1]), false);

                PositivePivotFreq++;
            }

            if (NegativePivot())
            {
                NegativePivotList.Add(new Data(HighestHigh(length), period));
                DrwArrow.Create(new ChartPoint(Bars.Time[1], Bars.High[1]), true);

                NegativePivotFreq++;
            }

            for (int i = 0; i < PositivePivotList.Count; i++)
            {
                // count down
                PositivePivotList[i].BarRemaining--;

                if (PositivePivotList[i].BarRemaining == 0)
                {
                    // check the past bars low
                    if (LowestLow(period) < PositivePivotList[i].Price)
                        LongStopHit++;

                    PositivePivotList.RemoveAt(i);
                }
            }

            for (int i = 0; i < NegativePivotList.Count; i++)
            {
                NegativePivotList[i].BarRemaining--;

                if (NegativePivotList[i].BarRemaining == 0)
                {
                    // check the past bars low
                    if (HighestHigh(period) > NegativePivotList[i].Price)
                        ShortStopHit++;

                    NegativePivotList.RemoveAt(i);
                }
            }

            if (Bars.LastBarOnChart)
            {
                Print("Long Stop Hit: " + LongStopHit);
                Print("Short Stop Hit: " + ShortStopHit);
                Print("Positive Pivot Freq: " + PositivePivotFreq);
                Print("Negative Pivot Freq: " + NegativePivotFreq);
            }
        }

        private bool PositivePivot()
        {
            // [2] > [1] < [0]
            return Pama[1][2] > Pama[1][1] && Pama[1][1] < Pama[1][0];
        }

        private bool NegativePivot()
        {
            // [2] < [1] > [0]
            return Pama[1][2] < Pama[1][1] && Pama[1][1] > Pama[1][0];
        }
        #endregion

        #region Algorithm
        private double HighestHigh(int length)
        {
            double ret = 0;
            for (int i = 1; i < length; i++)
                ret = Math.Max(ret, Bars.High[i]);

            return ret;
        }

        private double LowestLow(int length)
        {
            double ret = double.MaxValue;
            for (int i = 1; i < length; i++)
                ret = Math.Min(ret, Bars.Low[i]);

            return ret;
        }

        private double HighestHighCurrent(int length)
        {
            double ret = 0;
            for (int i = 0; i <= length; i++)
                ret = Math.Max(ret, Bars.High[i]);

            return ret;
        }

        private double LowestLowCurrent(int length)
        {
            double ret = double.MaxValue;
            for (int i = 0; i <= length; i++)
                ret = Math.Min(ret, Bars.Low[i]);

            return ret;
        }
        #endregion

        #region Plots
        private void PlotStrategyInfo()
        {
            // pama plots
            //StrategyInfo.SetPlotValue(1, Pama[1][0]);
            //StrategyInfo.SetPlotValue(2, Pama[2][0]);
            //StrategyInfo.SetPlotValue(3, Pama[3][0]);

            // channel plots
            StrategyInfo.SetPlotValue(11, HighestHigh(ChannelLength));
            StrategyInfo.SetPlotValue(12, LowestLow(ChannelLength));
        }
        #endregion

        #region Print
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

        #region Data
        class Data
        {
            public double Price { get; set; }
            public int BarRemaining { get; set; }

            public Data(double price, int barsLeft)
            {
                Price = price;
                BarRemaining = barsLeft;
            }
        }
        #endregion
    }
}