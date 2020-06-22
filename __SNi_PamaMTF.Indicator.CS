using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    //==============================================================================
    // Name           : Prepare for disposal
    // Description    : Price Action Moving Average with 10 Timeframes
    // Version        : v.1.0.0
    // Date Created   : 09 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==============================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==============================================================================

    /* ------------------------------- Version 1.0.0 -------------------------------
     * Create PamaMTF with toolbar support and Powerlanguage Functions.
     */


    public class __SNi_PamaMTF : IndicatorObject
    {
        // class properties
        [Input] public int PriceType { get; set; }

        // class fields
        private PriceActionMovingAverageMTF PamaMtf;
        private int Length, Smooth;
        private int[] PamaMtfCalc;

        public __SNi_PamaMTF(object _ctx) : base(_ctx) 
        {
            // set default for toolstrip.
            Length = 6;
            Smooth = 10;
            PriceType = 1;
        }
        protected override void Create()
        {
            PamaMtf = new PriceActionMovingAverageMTF(this);
            PamaMtfCalc = new int[PamaMtf._Timeframes];
        }
        protected override void StartCalc()
        {
            PamaMtf._Length = Length;
            PamaMtf._Smooth = Smooth;
            PamaMtf._PriceType = PriceType;

            PamaMtf.Calc_5 = 1;
            PamaMtf.Calc_10 = 1;
            PamaMtf.Calc_15 = 1;
            PamaMtf.Calc_20 = 1;
            PamaMtf.Calc_30 = 1;
            PamaMtf.Calc_60 = 1;
            PamaMtf.Calc_120 = 1;
            PamaMtf.Calc_240 = 1;
            PamaMtf.Calc_Day = 1;
            PamaMtf.Calc_Week = 1;
            PamaMtf.Calc_Mth = 1;
        }
        protected override void CalcBar()
        {
        }
    }
}