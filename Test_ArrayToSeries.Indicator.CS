using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using System.Collections;
using System.Collections.Generic;

namespace PowerLanguage.Indicator
{
    public class DataContainer<T>
    {
        protected List<T> m_Data;
        public DataContainer()
        {
            m_Data = new List<T>();
        }

        public void Set(T _Value)
        {
            m_Data.Add(_Value);
        }

        public T Get(int _index)
        {
            int _idx;
            if (_index == 0)
                _idx = m_Data.Count - 1;
            else
                _idx = m_Data.Count - _index;

            if (_idx < m_Data.Count && _idx >= 0)
                return m_Data[_idx];
            else
                throw new IndexOutOfRangeException();
        }

        public void Clear()
        {
            m_Data.Clear();
        }
    }

    public class Test_ArrayToSeries : IndicatorObject
    {
        public Test_ArrayToSeries(object _ctx) : base(_ctx) { }
        private IPlotObject plot1;

        DataContainer<Double> m_myContainer;

        ISeries<double> m_SeriesVar;

        protected override void Create()
        {
            m_myContainer = new DataContainer<Double>();
            plot1 = AddPlot(new PlotAttributes("Average(Sin(Close), 3)", EPlotShapes.Line, Color.Red));
        }

        int MaxContaimerData;
        protected override void StartCalc()
        {
            MaxContaimerData = ExecInfo.MaxBarsBack > 1 ? ExecInfo.MaxBarsBack : 10;
            m_myContainer.Clear();
            m_SeriesVar = new Lambda<double>(_bb => m_myContainer.Get(_bb));
        }

        protected override void CalcBar()
        {
            try
            {
                m_myContainer.Set(Math.Sin(Bars.CloseValue));
                plot1.Set(m_SeriesVar.Average(3));
            }
            catch (System.IndexOutOfRangeException ex)
            {
                return;
            }
        }
    }
}