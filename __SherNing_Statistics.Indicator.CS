using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class __SherNing_Statistics : IndicatorObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Statistics Test Framework
        // Requirements : 
        // Type         : Indicator / Function
        // Version      : v1.0
        // Date         : 24 - Sep - 2020
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
         *  1.0.1 - Develop
         * 
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  1
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        public __SherNing_Statistics(object _ctx) : base(_ctx) { }
        StatisticalAnalysis Stats;

        protected override void Create()
        {
            Stats = new StatisticalAnalysis(this);

            // debug clear
            Output.Clear();
        }
        protected override void StartCalc()
        {

        }
        protected override void CalcBar()
        {
            if (Bars.CurrentBar == 1)
            {
                // assign inputs 
                double[] arr = new double[]
                {
                    3, 4, 15, 20, 1, 2, 8, 12
                };

                double[] arr2 = new double[]
                {
                    3,5,9,1,7,26,24,18,32,15
                };


                Stats.MergeSort(arr2);
                foreach (var item in arr2)
                    Print("Num: " + item);
            }
        }

        #region Print
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false || Environment.Optimizing) return;

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

namespace PowerLanguage.Function
{
    public sealed class StatisticalAnalysis : FunctionSimple<string>
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Statistical Analysis
        // Requirements : 
        // Type         : Function
        // Version      : v1.0
        // Date         : 24 - Sep - 2020
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
         *  1.0.1 - Test Sort Functions
         *        - Merge Sort
         *        - Bubble Sort
         *        - Quick Sort
         *        - Radix Sort
         *        - Speed for trading
         * 
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  1
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Main
        public StatisticalAnalysis(CStudyControl m) : base(m)
        {

        }
        protected override void StartCalc()
        {
            // startcalc() required even if not in use
        }
        protected override string CalcBar()
        {
            // for function simple, if .Value or Call() is not invoked, CalcBar() will not be called.
            // rather, it would similar to a normal class but with access to PowerLanguage
            return "Calc Bar Not Implemented";
        }
        #endregion

        #region Merge Sort
        public void MergeSort(double[] arr)
        {
            MergeSort(arr, 0, arr.Length - 1);
        }
        private void MergeSort(double[] arr, int start, int end)
        {
            // O(nLogn)
            if (start < end)
            {
                // divide and conquer 
                int middle = (start + end) / 2;
                MergeSort(arr, start, middle);
                MergeSort(arr, middle + 1, end);

                Merge(arr, start, middle, end);
            }
        }
        private void Merge(double[] arr, int left, int middle, int right)
        {
            // length of temp arrays
            int leftLen = middle - left + 1;
            int rightLen = right - middle;

            double[] leftSet = new double[leftLen];

            for (int i = 0; i < leftLen; i++)
                leftSet[i] = arr[left + i];

            double[] rightSet = new double[rightLen];

            // i++ vs ++i
            for (int i = 0; i < rightLen; i++)
                rightSet[i] = arr[i + 1 + middle];

            // initialize pointers x,y,z
            int x, y, z;
            x = y = 0;
            z = left;

            // exit loop after one set has completed
            while (x < leftLen && y < rightLen)
            {
                if (leftSet[x] < rightSet[y])
                    arr[z++] = leftSet[x++];
                else
                    arr[z++] = rightSet[y++];
            }

            // this is a little unconventional
            for (; x< leftLen; x++)
                arr[z++] = leftSet[x];

            for (;  y < rightLen; y++)
                arr[z++] = rightSet[y];

            //// left side completed
            //if (x == leftLen)
            //{
            //    for (int i = y; i < rightLen; i++)
            //        arr[z++] = rightSet[i];
            //}

            //// right side completed
            //if (y == rightLen)
            //{
            //    for (int i = x; i < leftLen; i++)
            //        arr[z++] = leftSet[i];
            //}
        }

        #endregion

        #region Print
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false || Environment.Optimizing) return;

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
