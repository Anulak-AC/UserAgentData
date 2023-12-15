Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class Form1
    Dim hk As New XHeikinAshiCalculator
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim apiUrl As String = $"https://api.bitkub.com/api/market/symbols"
        DateTimePicker1.Value = DateTime.Today
        Dim result = hk.GetBitkubData(apiUrl)
        Dim jsonObject As JObject = JObject.Parse(result)
        Dim resultArray As JArray = jsonObject("result")
        ' Loop through the resultArray and add items to ComboBox1
        For Each symbol In resultArray
            ComboBox1.Items.Add(symbol("symbol").ToString())
        Next

        If ComboBox1.Items.Count > 0 Then
            ComboBox1.SelectedIndex = 1
        End If
        ComboBox2.SelectedText = "15"

    End Sub
    Public Function ConvertTM(DateTimePicke As String) As Long
        ' Get the selected date and time from DateTimePicker1
        Dim selectedDateTime As DateTime = DateTimePicke

        ' Convert the DateTime to a Unix timestamp (seconds since 1970-01-01 00:00:00 UTC)
        Dim unixTimestamp As Long = (selectedDateTime - New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds

        Return unixTimestamp
        ' Display the resulting timestamp (you can use it as needed)
        MessageBox.Show($"Selected DateTime: {selectedDateTime}{Environment.NewLine}Unix Timestamp: {unixTimestamp}")

    End Function

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim dt As DataTable
        Dim symbol As String = Replace(ComboBox1.Text, "THB_", "") & "_THB" '"BTC_THB"
        Dim resolution As String = ComboBox2.Text
        Dim fromTimestamp As Integer = ConvertTM(DateTimePicker1.Value)
        Dim toTimestamp As Integer = ConvertTM(DateTimePicker2.Value)
        DateTimePicker2.Value = DateTime.Now
        Dim apiUrl As String = $"https://api.bitkub.com/tradingview/history?symbol={symbol}&resolution={resolution}&from={fromTimestamp}&to={toTimestamp}"

        Dim result = hk.GetBitkubData(apiUrl)
        ' Your JSON data as a string
        Dim jsonData As String = result ' Replace with your actual JSON data 

        ' Parse JSON data
        Dim jsonObject As JObject = JObject.Parse(jsonData)

        ' Extract relevant arrays from JSON
        Dim closes As JArray = jsonObject("c")
        Dim highs As JArray = jsonObject("h")
        Dim lows As JArray = jsonObject("l")
        Dim opens As JArray = jsonObject("o")
        Dim timestamps As JArray = jsonObject("t")
        Dim volunm As JArray = jsonObject("v")


        ' Convert to Heikin-Ashi
        Dim heikinAshiData As List(Of HeikinAshiData) = hk.ConvertToHeikinAshi(opens, closes, highs, lows, timestamps)

        ' Add Buy/Sell signals
        Dim signalData As List(Of SignalData) = hk.AddBuySellSignals(heikinAshiData)

        ' Create DataTable
        dt = hk.CreateDataTable(heikinAshiData, signalData)
        DataGridView1.DataSource = dt

        'Dim signalData As List(Of SignalData) = AddBuySellSignals(heikinAshiData)
        SummarizeTrendProbabilities(heikinAshiData, signalData)
        hk.LearnAndTrade(heikinAshiData, signalData)

        ' Create an instance of the XHeikinAshiCalculator


        ' Now, you have a DataTable (dt) containing Heikin-Ashi data and buy/sell signals.

    End Sub

    Public Sub SummarizeTrendProbabilities(heikinAshiData As List(Of HeikinAshiData), signalData As List(Of SignalData))
        ' Replace the following logic with your trend analysis strategy
        ' For example, you might use moving averages, technical indicators, etc.
        ListBox1.Items.Clear()
        Dim totalDataPoints As Integer = heikinAshiData.Count
        Dim trendUpCount As Integer = 0
        Dim trendDownCount As Integer = 0
        Dim noChangeCount As Integer = 0

        For i As Integer = 1 To heikinAshiData.Count - 1
            ' Example: Count occurrences of Up, Down, and No Change trends
            Select Case signalData(i - 1).Action
                Case "Up"
                    trendUpCount += 1
                Case "Down"
                    trendDownCount += 1
                Case "No Change"
                    noChangeCount += 1
                Case "None"
                    noChangeCount += 1
            End Select
        Next

        ' Calculate trend probabilities
        Dim trendUpProbability As Double = (trendUpCount / totalDataPoints) * 100
        Dim trendDownProbability As Double = (trendDownCount / totalDataPoints) * 100
        Dim noChangeProbability As Double = (noChangeCount / totalDataPoints) * 100
        ListBox1.Items.Add($"Trend Up Probability: {trendUpProbability}%")
        ListBox1.Items.Add($"Trend Down Probability: {trendDownProbability}%")
        ListBox1.Items.Add($"No Change Probability: {noChangeProbability}%")

        ' Display or use the trend probabilities as needed
        Console.WriteLine($"Trend Up Probability: {trendUpProbability}%")
        Console.WriteLine($"Trend Down Probability: {trendDownProbability}%")
        Console.WriteLine($"No Change Probability: {noChangeProbability}%")



    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ListBox1.Items.Clear()

        For i As Integer = 0 To ComboBox1.Items.Count() - 1
            ComboBox1.SelectedIndex = i

            Dim symbol As String = Replace(ComboBox1.Text, "THB_", "") & "_THB" '"BTC_THB"
            Dim resolution As String = ComboBox2.Text
            Dim fromTimestamp As Integer = ConvertTM(DateTimePicker1.Value)
            Dim toTimestamp As Integer = ConvertTM(DateTimePicker2.Value)
            DateTimePicker2.Value = DateTime.Now
            Dim apiUrl As String = $"https://api.bitkub.com/tradingview/history?symbol={symbol}&resolution={resolution}&from={fromTimestamp}&to={toTimestamp}"

            Try
                Dim result = hk.GetBitkubData(apiUrl)
                ' Your JSON data as a string
                Dim jsonData As String = result ' Replace with your actual JSON data 

                ' Parse JSON data
                Dim jsonObject As JObject = JObject.Parse(jsonData)
                If jsonObject.Count > 0 Then
                    ' Extract relevant arrays from JSON
                    Dim closes As JArray = jsonObject("c")

                    If closes.Count >= 200 Then ' Ensure there are enough data points for SMA200
                        ' Calculate MACD
                        Dim macdValues() As Double = CalculateMACD(closes, 10, 50)

                        ' Calculate SMA
                        Dim sma10 As Double = CalculateSMA(closes, 10)
                        Dim sma50 As Double = CalculateSMA(closes, 50)
                        Dim sma200 As Double = CalculateSMA(closes, 200)

                        ' Compare current close with SMAs to determine trend
                        Dim currentClose As Double = closes(closes.Count - 1)
                        Dim trend As String = DetermineTrend(currentClose, sma10, sma50, sma200)

                        ' Output results (or log them)
                        Console.WriteLine($"Results for {symbol}:")
                        Console.WriteLine("MACD (12,26): " & macdValues(0))
                        Console.WriteLine("Signal (9): " & macdValues(1))
                        Console.WriteLine("MACD Histogram: " & macdValues(2))
                        Console.WriteLine("SMA10: " & sma10)
                        Console.WriteLine("SMA50: " & sma50)
                        Console.WriteLine("SMA200: " & sma200)
                        Console.WriteLine("Current Close: " & currentClose)
                        Console.WriteLine("Trend: " & trend)
                        ListBox1.Items.Add($"Results for {symbol}:")
                        ListBox1.Items.Add("Trend: " & trend)
                    Else
                        Console.WriteLine($"Insufficient data points for {symbol}.")
                    End If
                End If
            Catch ex As Exception
                ' Handle exceptions (e.g., API request failed, JSON parsing error)
                Console.WriteLine($"Error processing data for {symbol}: {ex.Message}")
            End Try
        Next

    End Sub

    Function CalculateSMA(closes As JArray, period As Integer) As Double
        Dim sum As Double = 0

        ' Ensure that the array has enough elements for the specified period
        Dim count As Integer = Math.Min(closes.Count, period)

        ' Sum the closing prices for the specified period
        For i As Integer = 0 To count - 1
            sum += Convert.ToDouble(closes(closes.Count - 1 - i))
        Next

        ' Calculate the SMA
        Return sum / count
    End Function

    Function CalculateMACD(closes As JArray, shortPeriod As Integer, longPeriod As Integer) As Double()
        ' Calculate short-term EMA
        Dim shortEMA As Double = CalculateEMA(closes, shortPeriod)

        ' Calculate long-term EMA
        Dim longEMA As Double = CalculateEMA(closes, longPeriod)

        ' Calculate MACD line
        Dim macd As Double = shortEMA - longEMA

        ' Calculate signal line (9-day EMA of MACD)
        Dim signal As Double = CalculateEMA(macd, 9)

        ' Calculate MACD histogram
        Dim histogram As Double = macd - signal

        Return New Double() {macd, signal, histogram}
    End Function

    Function CalculateEMA(values As JArray, period As Integer) As Double
        Dim multiplier As Double = 2.0 / (period + 1)
        Dim ema As Double = Convert.ToDouble(values(0)) ' Initialize EMA with the first value

        For i As Integer = 1 To values.Count - 1
            Dim currentClose As Double = Convert.ToDouble(values(values.Count - 1 - i))
            ema = (currentClose - ema) * multiplier + ema
        Next

        Return ema
    End Function

    Function DetermineTrend(currentClose As Double, sma10 As Double, sma50 As Double, sma200 As Double) As String
        If currentClose > sma10 AndAlso currentClose > sma50 AndAlso currentClose > sma200 Then
            Return "Upward Trend"
        ElseIf currentClose < sma10 AndAlso currentClose < sma50 AndAlso currentClose < sma200 Then
            Return "Downward Trend"
        Else
            Return "Sideways Trend"
        End If
    End Function


End Class
