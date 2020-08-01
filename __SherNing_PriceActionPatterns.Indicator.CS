using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class __SherNing_PriceActionPatterns : IndicatorObject
    {
        //==========================================================================================
        // Name           : __SherNing_PriceActionPatterns
        // Description    : Price Action Patterns
        // Version        : v1.0
        // Date Created   : 01 - Aug - 2020
        // Time Taken     : 
        // Remarks        : Protocol for starting and completing a project
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  
         *  ----------------------------------- Version History -----------------------------------
         *  
         *  ------------------------------------ Project Notes ------------------------------------
         *  - Inside Bar
         *  ------------------------------------ Project Notes ------------------------------------
         */


        public __SherNing_PriceActionPatterns(object _ctx) : base(_ctx) 
        { 
        
        }
        protected override void Create()
        {
            Output.Clear();
        }

        double OnePip;
        protected override void StartCalc()
        {
            // 10 ticks = 1 pip
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
        }
        protected override void CalcBar()
        {
            CurrentInsideBar();
        }

        private void CurrentInsideBar()
        {
            double prevHigh = Bars.High[1];
            double currHigh = Bars.High[0];
            double prevLow = Bars.Low[1];
            double currLow = Bars.Low[0];
            bool insideBar = false;
            bool greenBar = Bars.Close[0] > Bars.Open[0];

            // current bar is an inside bar
            if (currHigh < prevHigh && currLow > prevLow) 
                insideBar = true;
            
            // if current bar is an inside bar
            if (insideBar)
            {
                IArrowObject arrow;

                // chart point requires a time and price
                ChartPoint arrowPt;
                string message = "Inside Bar";
                Color color;

                if (greenBar)
                {
                    arrowPt = new ChartPoint(Bars.Time[0], Bars.Low[0]);
                    arrow = DrwArrow.Create(arrowPt, false);
                    color = Color.Cyan;
                }
                else
                {
                    arrowPt = new ChartPoint(Bars.Time[0], Bars.High[0]);
                    arrow = DrwArrow.Create(arrowPt, true);
                    color = Color.Magenta;
                }

                arrow.TextBGColor = Color.White;
                arrow.Color = color;
                arrow.Text = message;
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