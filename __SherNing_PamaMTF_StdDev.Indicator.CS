using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    //==========================================================================================
    // Name           : Price Action Multitime Frame Standard Deviation
    // Description    : Returns the Std Dev on a separate subchart
    // Version        : v1.0
    // Date Created   : 04 - Jul - 2020
    // Time Taken     : 
    // Remarks        :
    // Reference      : PamaMTF_StdDev
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  
     *  ----------------------------------- Version History -----------------------------------
     

     *  ---------------------------------------- Notes ----------------------------------------
     *  ---------------------------------------- Notes ----------------------------------------
     */

    [SameAsSymbol(false), UpdateOnEveryTick(false)]
    public class __SherNing_PamaMTF_StdDev : IndicatorObject
    {
        [Input] public int Length { get; set; }
        [Input] public int Smooth { get; set; }
        [Input] public int StdDevLength { get; set; }
        [Input] public int PriceType { get; set; }
        [Input] public bool DisplayTF1 { get; set; }
        [Input] public bool DisplayTF2 { get; set; }
        [Input] public bool DisplayTF3 { get; set; }

        public __SherNing_PamaMTF_StdDev(object _ctx) : base(_ctx)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //

            Length = 6;
            Smooth = 10;
            StdDevLength = 6;
            PriceType = 1;

            // default to true
            DisplayTF1 = true;
            DisplayTF2 = true;
            DisplayTF3 = true;
        }

        // fields
        PamaMTF_StdDev PamaStdDevFunc;
        IPlotObject StdDevPlot1, StdDevPlot2, StdDevPlot3;

        protected override void Create()
        {
            PamaStdDevFunc = new PamaMTF_StdDev(this);

            StdDevPlot1 = AddPlot(
                new PlotAttributes("TF 1", EPlotShapes.Line, Color.Yellow, Color.Black, 1, EPlotStyle.Solid, true));

            StdDevPlot2 = AddPlot(
               new PlotAttributes("TF 2", EPlotShapes.Line, Color.Cyan, Color.Black, 1, EPlotStyle.Dash, true));

            StdDevPlot3 = AddPlot(
               new PlotAttributes("TF 3", EPlotShapes.Line, Color.LightSkyBlue, Color.Black, 1, EPlotStyle.DashDot, true));
        }
        protected override void StartCalc()
        {
            PamaStdDevFunc._Length = Length;
            PamaStdDevFunc._Smooth = Smooth;
            PamaStdDevFunc._StdDevLength = StdDevLength;
            PamaStdDevFunc._PriceType = PriceType;
        }
        protected override void CalcBar()
        {
            PamaStdDevFunc.Call();

            if (Bars.Status == EBarState.Close)
            {
                if (DisplayTF1) StdDevPlot1.Set(PamaStdDevFunc.StdDev1[0]);
                if (DisplayTF2) StdDevPlot2.Set(PamaStdDevFunc.StdDev2[0]);
                if (DisplayTF3) StdDevPlot3.Set(PamaStdDevFunc.StdDev3[0]);
            }
        }
    }
}
