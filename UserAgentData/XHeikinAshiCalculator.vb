Imports System.Net
Imports System.Net.Http
Imports System.Threading.Tasks
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class XHeikinAshiCalculator
    Private Shared ReadOnly httpClient As New HttpClient()

    Public Function GetBitkubData(apiUrl As String) As String
        Dim jsonResult As String = Nothing
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
        Task.Run(Async Function()
                     jsonResult = Await FetchDataAsync(apiUrl)
                 End Function).Wait()

        Return jsonResult
    End Function



    Async Function FetchDataAsync(apiUrl As String) As Task(Of String)
        Using client As New HttpClient()
            Try
                Dim response As HttpResponseMessage = Await client.GetAsync(apiUrl)

                If response.IsSuccessStatusCode Then
                    Return Await response.Content.ReadAsStringAsync()
                Else
                    Console.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}")
                    Return Nothing
                End If
            Catch ex As Exception
                Console.WriteLine($"An error occurred: {ex.Message}")
                Return Nothing
            End Try
        End Using
    End Function

    Public Function ConvertToHeikinAshi(opens As JArray, closes As JArray, highs As JArray, lows As JArray, timestamps As JArray) As List(Of HeikinAshiData)
        Dim heikinAshiData As New List(Of HeikinAshiData)

        For i As Integer = 0 To opens.Count - 1
            Dim timestamp As Integer = timestamps(i)
            Dim dateTime As DateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime

            Dim haData As New HeikinAshiData() With {
                .Open = opens(i),
                .Close = closes(i),
                .High = highs(i),
                .Low = lows(i),
                .Timestamp = dateTime
            }

            heikinAshiData.Add(haData)
        Next

        Return heikinAshiData
    End Function
    Public Function AddBuySellSignals(heikinAshiData As List(Of HeikinAshiData)) As List(Of SignalData)
        Dim signalData As New List(Of SignalData)

        For i As Integer = 1 To heikinAshiData.Count - 1
            Dim signal As New SignalData() With {
            .Timestamp = heikinAshiData(i).Timestamp
        }

            ' Example: Buy when HA Close crosses above HA Open
            If heikinAshiData(i).Close > heikinAshiData(i).Open AndAlso heikinAshiData(i - 1).Close <= heikinAshiData(i - 1).Open Then
                signal.Action = "Buy"
            End If

            ' Example: Sell when HA Close crosses below HA Open
            If heikinAshiData(i).Close < heikinAshiData(i).Open AndAlso heikinAshiData(i - 1).Close >= heikinAshiData(i - 1).Open Then
                signal.Action = "Sell"
            End If

            ' Additional function to check trading trend (replace with your logic)
            Dim trend As String = CheckTrend(heikinAshiData, i)
            If Not String.IsNullOrEmpty(trend) Then
                signal.Action = trend
            End If

            ' Add the signal only if there is a valid action
            If Not String.IsNullOrEmpty(signal.Action) Then
                signalData.Add(signal)
            End If
        Next

        Return signalData
    End Function

    ' Function to check trading trend based on Heikin-Ashi data
    Private Function CheckTrend(heikinAshiData As List(Of HeikinAshiData), currentIndex As Integer) As String
        ' Replace the following logic with your trend-checking strategy
        ' For example, you might check moving averages, trend lines, etc.

        ' Here, a simple example is provided where the trend is considered "Up" if the current Close is higher than the previous Close
        If heikinAshiData(currentIndex).Close > heikinAshiData(currentIndex - 1).Close Then
            Return "Up"
        ElseIf heikinAshiData(currentIndex).Close < heikinAshiData(currentIndex - 1).Close Then
            Return "Down"
        Else
            Return "No Change"
        End If
    End Function


    Public Function GetActionLabel(signalData As List(Of SignalData), timestamp As DateTime) As String
        Dim matchingSignal = signalData.FirstOrDefault(Function(signal) signal.Timestamp = timestamp)
        Return If(matchingSignal IsNot Nothing, matchingSignal.Action, "None")
    End Function

    Public Function CreateDataTable(heikinAshiData As List(Of HeikinAshiData), signalData As List(Of SignalData)) As DataTable
        Dim dataTable As New DataTable()

        ' Add columns to the DataTable
        dataTable.Columns.Add("Timestamp", GetType(DateTime))
        dataTable.Columns.Add("HA_Open", GetType(Double))
        dataTable.Columns.Add("HA_Close", GetType(Double))
        dataTable.Columns.Add("Action", GetType(String))

        ' Add rows to the DataTable based on Heikin-Ashi and signals
        For i As Integer = 0 To heikinAshiData.Count - 1
            Dim row As DataRow = dataTable.NewRow()
            row("Timestamp") = heikinAshiData(i).Timestamp
            row("HA_Open") = heikinAshiData(i).Open
            row("HA_Close") = heikinAshiData(i).Close
            row("Action") = GetActionLabel(signalData, heikinAshiData(i).Timestamp)
            dataTable.Rows.Add(row)
        Next

        Return dataTable
    End Function









End Class

Public Class HeikinAshiData
    Public Property Open As Double
    Public Property Close As Double
    Public Property High As Double
    Public Property Low As Double
    Public Property Timestamp As DateTime
End Class

Public Class SignalData
    Public Property Timestamp As DateTime
    Public Property Action As String
End Class


Public Class Symbol
    Public Property id As Integer
    Public Property symbol As String
    Public Property info As SymbolInfo
End Class

Public Class SymbolInfo
    Public Property info As String
End Class