using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class __SherNing_Linq : IndicatorObject
    {
        //==========================================================================================
        // Name           : Mastering Linq 
        // Description    : Understand, practice and master LINQ
        // Version        : v.1.0
        // Date Created   : 27 - Jun - 2020
        // Time Taken     : 
        // Remarks        :
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  v.1.0
         *  - 
         *  ----------------------------------- Version History -----------------------------------

         *  ---------------------------------------- Notes ----------------------------------------
         *  - this keyword refers to the current instance variable which new keyword created on
         *  - the heap. In the context of extension methods, this refers to the reference which the 
         *  - current instance is pointing to on the heap.
         *  
         *  -
         *  ---------------------------------------- Notes ----------------------------------------
         */

        public __SherNing_Linq(object _ctx) : base(_ctx) { }

        protected override void StartCalc()
        {
            Output.Clear();
            TradeList<int> tradeList = new TradeList<int>();
            tradeList.Add(1);
            tradeList.Add(1);
            tradeList.Add(2);
            tradeList.Add(3);
            tradeList.Add(4);
            tradeList.Add(15);
            tradeList.Add(16);
            tradeList.Add(17);

            foreach (var item in tradeList)
            {
                Print("Item: " + item);
            }

            foreach (var item in TestYield())
            {
                Print("Item: " + item);
            }
        }
        protected override void CalcBar()
        {
        }

        private IEnumerable<int> TestYield()
        {
            // you can return multiple numbers with yield return
            yield return 1;
            yield return 2;

            // get out of method, and get back in
            yield return 3;
            yield return 4;
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

    class TradeList<T>
    {
        // properties
        public int Count { get; private set; }
        public int Capacity { get; private set; }

        // fields
        private T[] Items = new T[2];

        public void Add(T item)
        {
            if (Count == Items.Length)
            {
                // resize
                Capacity = Count * 2;
                T[] temp = new T[Capacity];

                for (int i = 0; i < Items.Length; i++)
                    temp[i] = Items[i];

                Items = temp;
            }

            Items[Count++] = item;
        }

        // needs to be public
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return Items[i];
        }

    }

}