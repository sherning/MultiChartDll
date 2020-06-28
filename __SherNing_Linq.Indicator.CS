using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Threading;

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
        }
        protected override void CalcBar()
        {
            if (Bars.LastBarOnChart)
            {
                Print("Close: " + GetSymbolLiveQuote("EUR.USD", ESymbolCategory.Cash));
            }
        }

        private Dictionary<string, double> LastSymbolPrices;
        private DateTime LastSessionTime;
        private double GetSymbolLiveQuote(string symbolName, ESymbolCategory category)
        {
            ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

            double forexData = 0;

            var benchmarkInfo = SymbolStorage
                .GetDataFeeds()
                .SelectMany
                (
                    data => SymbolStorage.GetSymbols(data, category)
                                         .Select(data2 => new { dataFeed = data, symInfo = data2 })
                )
                .Where(data => data.symInfo.SymbolName == symbolName)
                .First();

            InstrumentDataRequest request = Bars.Request;
            request.Range.From = Bars.Time[2];
            request.Range.To = Bars.Time[0];
            request.Category = (ESymbolCategory)benchmarkInfo.symInfo.SymbolCategory;
            request.Symbol = benchmarkInfo.symInfo.SymbolName;
            request.DataFeed = benchmarkInfo.dataFeed;
            request.Exchange = benchmarkInfo.symInfo.SymbolExchange;
            request.RTSymbol = request.Symbol;
            request.RTDataFeed = request.DataFeed;
            request.RTExchange = request.Exchange;

            AutoResetEvent autoEvent = new AutoResetEvent(false);

            IDataLoaderResult results = DataLoader.BeginLoadData(request, result =>
            {
                if (result.IsCompleted)
                {
                    try
                    {
                        rwLock.EnterWriteLock();
                        foreach (var quote in result.Data)
                        {
                            forexData = quote.Close;
                        }
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }
                }

                DataLoader.EndLoadData(result);
                autoEvent.Set();
            }
            , null);

            autoEvent.WaitOne();
            return forexData;
        }


        #region Linq Tests
        private void LinkTest()
        {
            // get symbols last closing prices and store them in a dictionary
            IEnumerable<string> brokers = SymbolStorage
               .GetDataFeeds()
               .OrderByDescending(broker => broker);

            // this is an anonymous type, must use var
            var symbols = SymbolStorage
                .GetSymbols("Interactive Brokers", ESymbolCategory.Cash)
                .SelectMy(symbol => new { Name = symbol.SymbolName, Exchange = symbol.SymbolExchange });


            foreach (var item in symbols)
            {
                Print("Name: " + item.Name);
                Print("Exchange: " + item.Exchange);
            }

            // old school
            IEnumerator<string> rator = brokers.GetEnumerator();
            while (rator.MoveNext())
            {
                Print("Broker: " + rator.Current);
            }

            // what are the information that I will need to pull data
            InstrumentDataRequest request = Bars.Request;

        }
        private IEnumerable<int> YieldReturnTest()
        {
            // you can return multiple numbers with yield return
            yield return 1;
            yield return 2;

            // get out of method, and get back in
            yield return 3;
            yield return 4;
        }
        #endregion

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

    static class MyLinq
    { 
        // yield return is causing the deferred execution
        public static IEnumerable<T> WhereMy<T> (this IEnumerable<T> query, Func<T,bool> gauntlet)
        {
            foreach (T item in query)
            {
                if (gauntlet(item) == true)
                    yield return item;
            }
        }

        public static IEnumerable<Ret> SelectMy<Source, Ret> 
            (this IEnumerable<Source> query, Func<Source, Ret> transform)
        {
            // takes in one type and spits out another or the same type
            foreach (var item in query)
                yield return transform(item);
        }
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