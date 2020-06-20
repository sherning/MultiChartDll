using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using ATCenterProxy.interop;

namespace PowerLanguage
{
    namespace Function
    {
        /// <summary>
        /// Returns a datastream based on daily data for AutoPositionSizing Calculation.
        /// </summary>
        public sealed class _SN_F_AddDataStream : FunctionSeries<System.Double>
        {
            [Input] public string ForexSymbol { get; set; }
            [Input] public ESymbolCategory SymbolCategory { get; set; }

            public _SN_F_AddDataStream (CStudyControl _master) : base(_master) { }

            /// <summary>
            /// Exist once only in memory, static is used for sharing among class instances, improving performance
            /// </summary>
            private static Dictionary<string, double> ForexData = ForexData = new Dictionary<string, double>();
            /// <summary>
            /// System.Threading to prevent concurrency by using a Lock
            /// </summary>
            private static ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
            /// <summary>
            /// Check for initalvalue == null
            /// </summary>
            private double? _InitialValue;
            public double InitialValue { get { return _InitialValue != null ? (double)_InitialValue : 0.0; } }

            private double GetBenchmarkValue()
            {
                string key = String.Format("{0:MM/dd/yyyy}", Bars.Time[0]);
                string key1 = String.Format("{0:MM/dd/yyyy}", Bars.Time[1]);

                bool hasKey;
                try
                {
                    Lock.EnterReadLock();
                    // Need to check for key, to prevent run-time error
                    hasKey = ForexData.ContainsKey(key);
                    if (hasKey && Bars.Time.Value < DateTime.Now.Date)
                        return ForexData[key];
                }
                finally
                {
                    // Regardless of above outcome, unlock
                    Lock.ExitReadLock();
                }

                // find our index symbol in our database
                var benchmarkInfo = SymbolStorage.GetDataFeeds()
                    .SelectMany(it =>
                        SymbolStorage.GetSymbols(it, SymbolCategory)
                        .Select(it2 => new
                        {
                            feed = it,
                            syminfo = it2
                        }))
                    .Where(it => it.syminfo.SymbolName == ForexSymbol)
                    .First();

              
                InstrumentDataRequest Req = Bars.Request;
                Req.From = Bars.Time[2];
                Req.To = Bars.Time[0] + TimeSpan.FromDays(365);
                Req.Category = (ESymbolCategory)benchmarkInfo.syminfo.SymbolCategory; // can we cast MCPA_MCSymbolCategories => ESymbolCategory ???
                Req.Symbol = benchmarkInfo.syminfo.SymbolName;
                Req.DataFeed = benchmarkInfo.feed;
                Req.Exchange = benchmarkInfo.syminfo.SymbolExchange;
                Req.RTSymbol = Req.Symbol;
                Req.RTDataFeed = Req.DataFeed;
                Req.RTExchange = Req.Exchange;

                AutoResetEvent autoEvent = new AutoResetEvent(false);

                IDataLoaderResult iRes = DataLoader.BeginLoadData(Req, Result =>
                {   // public void OnData(IDataLoaderResult Result){ }
                    if (Result.IsCompleted)
                    {
                        try
                        {
                            Lock.EnterWriteLock();
                            foreach (var quote in Result.Data)
                            {
                                string k = String.Format("{0:MM/dd/yyyy}", quote.Time);
                                ForexData[k] = quote.Close;
                            }
                        }
                        finally
                        {
                            Lock.ExitWriteLock();
                        }
                    }

                    DataLoader.EndLoadData(Result);
                    autoEvent.Set();
                },
                    null);

                autoEvent.WaitOne();

                try
                {
                    Lock.EnterReadLock();
                    hasKey = ForexData.ContainsKey(key);
                    bool hasKey1 = ForexData.ContainsKey(key1);

                    // we might not be able to load today's value
                    // if that happens, we revert to yesterday's value

                    return hasKey
                        ? ForexData[key]
                        : (hasKey1 ? ForexData[key1] : 0.0);
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            protected override void Create()
            {
                ForexSymbol = "EUR.USD";
            }

            protected override void StartCalc()
            {
                // Clear the data inside the dictionary
                ForexData.Clear();
            }

            protected override System.Double CalcBar()
            {
                double benchmark = GetBenchmarkValue();

                if (_InitialValue == null)
                    _InitialValue = benchmark;

                return benchmark;
            }
        }
    }
}
