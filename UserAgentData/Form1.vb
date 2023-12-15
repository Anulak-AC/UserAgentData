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

        ' Convert to Heikin-Ashi
        Dim heikinAshiData As List(Of HeikinAshiData) = hk.ConvertToHeikinAshi(opens, closes, highs, lows, timestamps)

        ' Add Buy/Sell signals
        Dim signalData As List(Of SignalData) = hk.AddBuySellSignals(heikinAshiData)

        ' Create DataTable
        dt = hk.CreateDataTable(heikinAshiData, signalData)
        DataGridView1.DataSource = dt

        'Dim signalData As List(Of SignalData) = AddBuySellSignals(heikinAshiData)
        SummarizeTrendProbabilities(heikinAshiData, signalData)


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

End Class
