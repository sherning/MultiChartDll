Imports PowerLanguage
Imports PowerLanguage.Indicator
Imports System
Imports System.Drawing
Imports System.Runtime.CompilerServices

Namespace PowerLanguage.Indicator
    Public Class vb_Block_Trades_per_Bar
        Inherits IndicatorObject
        ' Methods
        Public Sub New(ByVal ctx As Object)
            MyBase.New(ctx)
            Me.datanum = 1
            Me.blocksize = 5000
			Me.m_barnum = 0
			Me.m_blocktrades = 0
        End Sub

        Protected Overrides Sub Create()
            Me.Plot1 = MyBase.AddPlot(New PlotAttributes("BlockTrds", EPlotShapes.Histogram, Color.Blue, Color.Empty, 1, 0, True))
        End Sub

        Protected Overrides Sub StartCalc()
			Me.m_barnum = 0
			Me.m_blocktrades = 0
        End Sub

        Protected Overrides Sub CalcBar()
            If (MyBase.Bars.LastBarOnChart AndAlso (MyBase.Bars.Status <> EBarState.Close)) Then
                If (Not Me.m_barnum = MyBase.Bars.CurrentBar) Then
                    Me.m_blocktrades = 0
                    Me.m_barnum = MyBase.Bars.CurrentBar
                End If
                If PublicFunctions.DoubleGreaterEquals(MyBase.Bars.StatusLine.LastVolume, Me.blocksize) Then
                    Me.m_blocktrades = (Me.m_blocktrades + 1)
                End If
                Me.Plot1.Set(0, Me.m_blocktrades)
            End If
        End Sub


        ' Properties
        <Input> _
        Public Property blocksize As Double

        <Input()> _
        Public Property datanum As Double


        ' Fields
        Private m_barnum As Integer
        Private m_blocktrades As Integer
        Private Plot1 As IPlotObject
    End Class
End Namespace
