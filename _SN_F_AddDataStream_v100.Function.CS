
//==============================================================================
// Name:          _SN_F_AddDataStream_v100
// Description:   Add secondary data stream without plotting on chart.
// Version:       v.1.0.0
// Date Created:  20 - Nov - 2019
//==============================================================================
// Copyright:     2019, Sher Ning Technologies
//             
// License:     
//==============================================================================

//------------------------------------------------------------------------------


#define CLEAR_ON_STARTCALC

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
        public sealed class _SN_F_AddDataStream_v100 : FunctionSeries<System.Double>
        {
            [Input] public string BenchmarkTicker { get; set; }
            [Input] public ESymbolCategory SymbolCategory { get; set; }

            public _SN_F_AddDataStream_v100 (CStudyControl _master) : base(_master) { }
            public _SN_F_AddDataStream_v100 (CStudyControl _master, int _ds) : base(_master, _ds) { }

            private static Dictionary<string, double> BenchmarkData = BenchmarkData = new Dictionary<string, double>();
            private static ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
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
                    hasKey = BenchmarkData.ContainsKey(key);
                    if (hasKey && Bars.Time.Value < DateTime.Now.Date)
                        return BenchmarkData[key];
                }
                finally
                {
                    Lock.ExitReadLock();
                }

                // when we get here, one of 2 things has happened
                // 1) the date is not available in the cache
                // 2) the date equals today, and we should update the cache

                // find our index symbol in the database
                var benchmarkInfo = SymbolStorage.GetDataFeeds()
                    .SelectMany(it =>
                        SymbolStorage.GetSymbols(it, SymbolCategory)
                        .Select(it2 => new
                        {
                            feed = it,
                            syminfo = it2
                        }))
                    .Where(it => it.syminfo.SymbolName == BenchmarkTicker)
                    .First();

                // populate data request structure
                // see https://www.multicharts.com/trading-software/index.php/4.7.4_Receiving_the_data_for_any_symbol_and_any_resolution._DataLoader
                InstrumentDataRequest Req = Bars.Request;
                Req.From = Bars.Time[2];
                Req.To = Bars.Time[0] + TimeSpan.FromDays(365);
                // alternative way of populating data range:
                //	DataRequest _DataRequest = new DataRequest();
                //	_DataRequest.RequestType = DataRequestType.BarsBack;
                //	_DataRequest.Count = Bars.FullSymbolData.Count;
                //	Req.Range = _DataRequest;
                //Req.Resolution = ... // FIXME: should force this to daily bars
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
                                BenchmarkData[k] = quote.Close;
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
                    hasKey = BenchmarkData.ContainsKey(key);
                    bool hasKey1 = BenchmarkData.ContainsKey(key1);

                    // we might not be able to load today's value
                    // if that happens, we revert to yesterday's value

                    return hasKey
                        ? BenchmarkData[key]
                        : (hasKey1 ? BenchmarkData[key1] : 0.0);
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            protected override void Create()
            {
                BenchmarkTicker = "EUR.USD";
            }

            protected override void StartCalc()
            {
#if CLEAR_ON_STARTCALC
                BenchmarkData.Clear();
#endif
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

//==============================================================================
// end of file