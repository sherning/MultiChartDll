using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_StopLimitStrategyTest : SignalObject
    {
        //=========================================================================================
        // Project Name : Stop Limit Strategy Test
        // Description  : Test Stop Limit Orders
        // Type         : Strategy
        // Version      : v1.0
        // Date         : 13 - Aug - 2020
        // Time Taken   : 
        // Remarks      : 
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *--------------------------------------- Objectives --------------------------------------
         * 1.0.1 - Test StopLimit as an Entry Order
         * 1.0.2 - Test StopLimit as an Exit Order
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *---------------------------------------- Versions ---------------------------------------
        /*/
        public __SherNing_StopLimitStrategyTest(object _ctx) : base(_ctx)
        {

        }

        VariableObject<double> Vo;
        VariableSeries<double> Vs;
        protected override void Create()
        {
            Vo = new VariableObject<double>(this);
            Vs = new VariableSeries<double>(this);
        }
        double OnePip;
        double Vv;

        protected override void StartCalc()
        {
            Output.Clear(); // Clear PowerLanguage Editor output tab
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
        }

        protected override void CalcBar()
        {
            Vv = Vs.Value = Vo.Value = Bars.Close[0];
            
            if (Bars.CurrentBar < 10)
            {
                
            }
        }

        private void Display(double price, Color color)
        {
            ChartPoint p = new ChartPoint(Bars.Time[0], price);
            ITextObject txt = DrwText.Create(p, "-");
            txt.Color = color;
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

