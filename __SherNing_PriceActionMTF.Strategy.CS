//==============================================================================
// Name           : __SherNing_PriceActionMTF
// Description    : Multi-Time Frame Price Action Strategy
// Version        : v.1.0.0
// Date Created   : 20 - May - 2020
// Time Taken     : 
// Remarks        :
//==============================================================================
// Copyright      : 2020, Sher Ning Technologies           
// License        :      
//==============================================================================

/* ------------------------------- Version 1.0.0 -------------------------------
 * 22/5/2020 - Created and tested class HullMovingAverage class.
 */

using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    public class __SherNing_PriceActionMTF : SignalObject
    {
        private bool DebugMode = true;
        [Input] public int Length { get; set; }
        [Input] public int Smoothness { get; set; }

        public __SherNing_PriceActionMTF(object _ctx) : base(_ctx) 
        {
            Length = 6;
            Smoothness = 10;
        }
        protected override void Create()
        {
            PriceList = new List<double>();
            PAMA = new PriceActionMovingAverage();
        }

        private List<double> PriceList;
        private PriceActionMovingAverage PAMA;
        protected override void StartCalc()
        {
            Output.Clear();
            PAMA.Bars = Bars;
            PAMA.PriceList = PriceList;
            PAMA.Length = Length;
            PAMA.Smoothness = Smoothness;
        }
        protected override void CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                PriceList.Add(Bars.CloseValue);
                //Print("PAMA: " + PAMA.Value + " \t Bar Number: " + Bars.FullSymbolData.Current);
                //Print(PAMA.ToString());
            }

        }

        #region Price Action Moving Average
        // Single time frame price action moving average
        class PriceActionMovingAverage
        {
            // Public Properties
            public int Length { get; set; }
            public int Smoothness { get; set; }
            public IInstrument Bars { get; set; }
            public double Value { get { return CalcBar(); } }
            public List<double> PriceList { get; set; }

            // Fields
            private bool FinishedStartCalc;
            private double Alpha, Gain;
            private HullMovingAverage HMA;
            private void StartCalc()
            {
                // calculate start calc only once.
                FinishedStartCalc = true;

                // if you are expecting value < 1 then add .0
                Alpha = 2.0 / (Length + 1.0);
                Gain = (Smoothness - 1.0) / 100.0;
                HMA = new HullMovingAverage();
                PriceList = new List<double>();

                HMA.Bars = Bars;
                HMA.Length = Length;
                HMA.PriceList = PriceList;
            }
            private double PrevPAMA_Value, PAMA_Value;
            private double CalcBar()
            {
                if (FinishedStartCalc == false) StartCalc();

                PrevPAMA_Value = PAMA_Value;

                if (PriceList.Count > Length - 1) PriceList.RemoveAt(0);
                PriceList.Add(Bars.CloseValue);

                if (PriceList.Count == Length)
                {
                    PAMA_Value = Alpha * (HMA.Value + Gain * (Bars.CloseValue - PrevPAMA_Value))
                        + (1.0 - Alpha) * PrevPAMA_Value;
                }

                return PAMA_Value;
            }

            public override string ToString()
            {
                string currentBar = string.Format("Current Bar: " + Bars.FullSymbolData.Current);
                string output = string.Format("\tAlpha: {0}, Gain: {1}", Alpha, Gain);
                string startCalculated = string.Format("\tStartCalc: " + FinishedStartCalc);

                return currentBar + output + startCalculated;
            }

        }

        #endregion

        #region Hull Moving Average Calculations
        class HullMovingAverage
        {
            public int Length { get; set; }
            public List<double> PriceList { get; set; }
            public IInstrument Bars { get; set; }
            public double Value { get { return CalcBar(); } }

            WeightedAverage WMAOne, WMATwo, WMAThree;
            List<double> PriceList1, PriceList3;

            private bool HasCalculated;
            private int Length1, Length2, Length3;
            private void StartCalc()
            {
                WMAOne = new WeightedAverage();
                WMATwo = new WeightedAverage();
                WMAThree = new WeightedAverage();
                PriceList3 = new List<double>();
                HasCalculated = true;

                if (Length < 1) Length = 1;

                // Calculate ONCE for faster computation
                int halvedLength;
                if ((Math.Ceiling((double)(Length / 2)) - (Length / 2)) <= 0.5)
                {
                    halvedLength = (int)Math.Ceiling((double)(Length / 2));
                }
                else
                {
                    halvedLength = (int)Math.Floor((double)(Length / 2));
                }

                int sqrRootLength;
                double sqLength = Math.Sqrt(Length);

                if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
                {
                    sqrRootLength = (int)Math.Ceiling(sqLength);
                }
                else
                {
                    sqrRootLength = (int)Math.Floor(sqLength);
                }

                Length1 = halvedLength;
                Length2 = Length;
                Length3 = sqrRootLength;

                PriceList1 = new List<double>(Length1);
                for (int i = 0; i < Length1; i++) PriceList1.Add(0);
                PriceList3.Clear();

                WMAOne.Length = Length1;
                WMAOne.PriceList = PriceList1;

                WMATwo.Length = Length2;
                WMATwo.PriceList = PriceList;

                WMAThree.Length = Length3;
                WMAThree.PriceList = PriceList3;
            }

            private bool OnOpenTick;
            private int PrevCurrentBarValue, LastPrice3Index;
            private double CalcBar()
            {
                if (HasCalculated == false) StartCalc();

                // on bar opening tick
                OnOpenTick = (Bars.CurrentBar > PrevCurrentBarValue);
                PrevCurrentBarValue = Bars.CurrentBar;

                if (PriceList.Count > Length2 - 1)
                {
                    for (int i = Length1 - 1; i >= 0; i--)
                        PriceList1[i] = PriceList[i + Length2 - Length1];
                }

                double wma1 = 2.0 * WMAOne.Value;
                double wma2 = WMATwo.Value;

                // on bar open tick, to debug set to true if testing on startcalc
                if (OnOpenTick)
                {
                    if (PriceList3.Count > Length3 - 1) PriceList3.RemoveAt(0);
                    PriceList3.Add(0);
                    LastPrice3Index = PriceList3.Count - 1;
                }
                PriceList3[LastPrice3Index] = wma1 - wma2;

                if (PriceList3.Count == Length3)
                    return WMAThree.Value;
                else
                    return -1;
            }

            // Debug inside a nested class of MC.NET
            public override string ToString()
            {
                string output = string.Format("Length1: {0}, Length2: {1}, Length3: {2}", 
                    Length1, Length2, Length3);
                string output2 = string.Format(" LastIndex: {0}",
                    LastPrice3Index);
                string output3 = string.Format(" PriceList: {0}, PriceList1: {1}, PriceList3: {2}",
                    PriceList.Count,
                    PriceList1.Count,
                    PriceList3.Count);
                string output4 = string.Format("Current Bar: " + Bars.FullSymbolData.Current);
                return output4;
            }
        }
        class WeightedAverage
        {
            // Public properties
            public int Length { private get; set; }
            public List<double> PriceList { get; set; }
            public double Value { get { return CalcBar(); } }

            // Fields.
            private bool HasCalculated = false;
            private double Denominator;
            private void StartCalc()
            {
                if (Length < 1) Length = 1;

                Denominator = 1 / ((Length + 1) * Length * 0.5);

                HasCalculated = true;
            }

            private double CalcBar()
            {
                if (HasCalculated == false) StartCalc();

                double weightedSum = 0;
                for (int i = 0; i < Length; i++)
                    weightedSum += (Length - i) * PriceList[Length - 1 - i];

                return weightedSum * Denominator;
            }
        }
        #endregion

        #region Print Functions
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            Output.WriteLine("Project Name:  {0}   |   Bar Time:  {1}   |   Message:  {2}",
                Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
}