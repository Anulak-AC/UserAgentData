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
        If opens IsNot Nothing Then
            For i As Integer = 0 To opens.Count - 1
                Dim timestamp As Integer = timestamps(i)
                ' Convert Unix timestamp to DateTime
                Dim dateTime As DateTime = New DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp)

                ' Adjust by adding 7 hours
                dateTime = dateTime.AddHours(7)

                Dim haData As New HeikinAshiData() With {
                .Open = opens(i),
                .Close = closes(i),
                .High = highs(i),
                .Low = lows(i),
                .Timestamp = dateTime
            }

                heikinAshiData.Add(haData)
            Next

        End If

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
    Public Function LearnAndTrade(heikinAshiData As List(Of HeikinAshiData), signalData As List(Of SignalData)) As Integer
        ' Trading strategy parameters
        Dim initialCapital As Double = 100 ' Initial capital (replace with your actual initial capital)
        Dim positionSize As Double = 2 ' Percentage of capital to allocate per trade (adjust as needed)
        Dim stopLossPercentage As Double = 1 ' Stop-loss percentage for risk management
        Dim portfolio As Double = initialCapital ' Current portfolio value

        ' Variables to store summary information
        Dim totalTrades As Integer = 0
        Dim totalBuyAmount As Double = 0
        Dim totalSellAmount As Double = 0

        ' Iterate through signals and execute trading decisions
        For i As Integer = 0 To signalData.Count - 1
            Dim signal As SignalData = signalData(i)

            ' Calculate trend strength based on Heikin Ashi data
            Dim trendStrength As Double = CalculateTrendStrength(heikinAshiData, i)



            ' Calculate moving averages
            Dim shortTermMA As Double = CalculateMovingAverage(heikinAshiData, i, 5)
            Dim longTermMA As Double = CalculateMovingAverage(heikinAshiData, i, 20)

            ' Calculate MACD
            Dim macd As Double = CalculateMACD(heikinAshiData, i, 12, 26, 9)

            ' Calculate RSI
            Dim rsi As Double = CalculateRSI(heikinAshiData, i, 14)

            If i = signalData.Count - 1 Then
                ' Display Heikin Ashi data and signals
                Console.WriteLine($"Timestamp: {heikinAshiData(i).Timestamp}, HA Open: {heikinAshiData(i).Open}, HA Close: {heikinAshiData(i).Close}")
                Console.WriteLine($"Signal: {signal.Action}, Trend Strength: {trendStrength}")
                ' Display additional technical indicators
                Console.WriteLine($"Short-term MA: {shortTermMA}, Long-term MA: {longTermMA}, MACD: {macd}, RSI: {rsi}")
            End If


            ' Assess current price opportunity
            Dim currentPrice As Double = heikinAshiData(i).Close

            ' Check if it's worth trading based on the latest price
            If IsWorthTrading(currentPrice, shortTermMA, longTermMA, macd, rsi, signal.Action) Then
                ' Execute trading decisions based on signals and trend analysis
                If signal.Action = "Up" AndAlso trendStrength > 0.5 AndAlso shortTermMA > longTermMA AndAlso macd > 0 AndAlso rsi < 30 Then
                    ' Example: Buy only in a strong uptrend, when short-term MA is above long-term MA, MACD is positive, and RSI is below 30
                    Dim buyAmount As Double = portfolio * positionSize
                    portfolio -= buyAmount ' Deduct the allocated capital
                    ' Accumulate buy information for summary
                    totalTrades += 1
                    totalBuyAmount += buyAmount
                    ' Implement your buy execution logic here
                    ' For example, record the buy order, update portfolio, etc.
                    If i = signalData.Count - 1 Then
                        Console.WriteLine($"Buy at {signal.Timestamp}, Amount: {buyAmount}, Remaining Portfolio: {portfolio}")
                    End If

                ElseIf signal.Action = "Down" AndAlso trendStrength < -0.5 AndAlso shortTermMA < longTermMA AndAlso macd < 0 AndAlso rsi > 70 Then
                    ' Example: Sell only in a strong downtrend, when short-term MA is below long-term MA, MACD is negative, and RSI is above 70
                    Dim sellAmount As Double = portfolio * positionSize
                    portfolio += sellAmount ' Add the sold amount to portfolio

                    ' Accumulate sell information for summary
                    totalTrades += 1
                    totalSellAmount += sellAmount
                    ' Implement your sell execution logic here
                    ' For example, record the sell order, update portfolio, etc.

                    If i = signalData.Count - 1 Then
                        Console.WriteLine($"Sell at {signal.Timestamp}, Amount: {sellAmount}, Remaining Portfolio: {portfolio}")
                    End If
                End If

                ' Additional logic for profit calculation and risk management can be added here
                ' For example, track portfolio value, calculate profit/loss, apply stop-loss, etc.
                If i = signalData.Count - 1 Then
                    ' Separator for better readability
                    Console.WriteLine(" worth trading at the current price.")
                    Console.WriteLine("----------------------------------------------------")
                End If

            Else
                If i = signalData.Count - 1 Then
                    ' Print a message indicating that it's not worth trading at the current price
                    Console.WriteLine("Not worth trading at the current price.")
                    ' Separator for better readability
                    Console.WriteLine("----------------------------------------------------")
                End If

            End If
        Next

        ' Print summary at the end
        Console.WriteLine("----------------------------------------------------")
        Console.WriteLine("Summary:")
        Console.WriteLine($"Total Trades: {totalTrades}")
        Console.WriteLine($"Total Buy Amount: {totalBuyAmount}")
        Console.WriteLine($"Total Sell Amount: {totalSellAmount}")
        Console.WriteLine("----------------------------------------------------")

        ' Return the total number of trades
        Return totalTrades
    End Function
    Private Function IsWorthTrading(currentPrice As Double, shortTermMA As Double, longTermMA As Double, macd As Double, rsi As Double, action As String) As Boolean
        ' Check if it's worth trading based on the latest price and technical indicators
        ' Customize this logic based on your trading strategy for both Buy and Sell

        If action = "Buy" Then
            ' Example: Check conditions for a Buy
            Return currentPrice > shortTermMA AndAlso currentPrice > longTermMA
        ElseIf action = "Sell" Then
            ' Example: Check conditions for a Sell
            Return currentPrice < shortTermMA AndAlso currentPrice < longTermMA
        Else
            ' No specific action, return false
            Return False
        End If
    End Function

    'Private Function IsWorthTrading(currentPrice As Double, shortTermMA As Double, longTermMA As Double, macd As Double, rsi As Double) As Boolean
    '    ' Example: Check if it's worth trading based on the latest price and technical indicators
    '    ' You can customize this logic based on your trading strategy
    '    Return currentPrice > shortTermMA AndAlso currentPrice > longTermMA
    'End Function




    Public Function LearnAndTrade2(heikinAshiData As List(Of HeikinAshiData), signalData As List(Of SignalData)) As Double
        ' Trading strategy parameters
        Dim initialCapital As Double = 10 ' Initial capital (replace with your actual initial capital)
        Dim positionSize As Double = 2 ' Percentage of capital to allocate per trade (adjust as needed)
        Dim stopLossPercentage As Double = 1 ' Stop-loss percentage for risk management
        Dim portfolio As Double = initialCapital ' Current portfolio value

        ' Variables to store summary information
        Dim totalTrades As Integer = 0
        Dim totalBuyAmount As Double = 0
        Dim totalSellAmount As Double = 0

        ' Iterate through signals and execute trading decisions
        For i As Integer = 0 To signalData.Count - 1
            Dim signal As SignalData = signalData(i)

            ' Calculate trend strength based on Heikin Ashi data
            Dim trendStrength As Double = CalculateTrendStrength(heikinAshiData, i)

            ' Calculate moving averages
            Dim shortTermMA As Double = CalculateMovingAverage(heikinAshiData, i, 5)
            Dim longTermMA As Double = CalculateMovingAverage(heikinAshiData, i, 20)

            ' Calculate MACD
            Dim macd As Double = CalculateMACD(heikinAshiData, i, 12, 26, 9)

            ' Calculate RSI
            Dim rsi As Double = CalculateRSI(heikinAshiData, i, 14)

            ' Execute trading decisions based on signals and trend analysis
            If signal.Action = "Up" AndAlso trendStrength > 0.5 AndAlso shortTermMA > longTermMA AndAlso macd > 0 AndAlso rsi < 30 Then
                ' Example: Buy only in a strong uptrend, when short-term MA is above long-term MA, MACD is positive, and RSI is below 30
                Dim buyAmount As Double = portfolio * positionSize
                portfolio -= buyAmount ' Deduct the allocated capital
                ' Accumulate buy information for summary
                totalTrades += 1
                totalBuyAmount += buyAmount
                ' Implement your buy execution logic here
                ' For example, record the buy order, update portfolio, etc.
                Console.WriteLine($"Buy at {signal.Timestamp}, Amount: {buyAmount}, Remaining Portfolio: {portfolio}")
            ElseIf signal.Action = "Down" AndAlso trendStrength < -0.5 AndAlso shortTermMA < longTermMA AndAlso macd < 0 AndAlso rsi > 70 Then
                ' Example: Sell only in a strong downtrend, when short-term MA is below long-term MA, MACD is negative, and RSI is above 70
                Dim sellAmount As Double = portfolio * positionSize
                portfolio += sellAmount ' Add the sold amount to portfolio

                ' Accumulate sell information for summary
                totalTrades += 1
                totalSellAmount += sellAmount
                ' Implement your sell execution logic here
                ' For example, record the sell order, update portfolio, etc.
                Console.WriteLine($"Sell at {signal.Timestamp}, Amount: {sellAmount}, Remaining Portfolio: {portfolio}")
            End If

            ' Additional logic for profit calculation and risk management can be added here
            ' For example, track portfolio value, calculate profit/loss, apply stop-loss, etc.

            ' Separator for better readability
            Console.WriteLine("----------------------------------------------------")
        Next

        ' Print summary at the end
        Console.WriteLine("----------------------------------------------------")
        Console.WriteLine("Summary:")
        Console.WriteLine($"Total Trades: {totalTrades}")
        Console.WriteLine($"Total Buy Amount: {totalBuyAmount}")
        Console.WriteLine($"Total Sell Amount: {totalSellAmount}")
        Console.WriteLine($"portfolio: {portfolio}")
        Console.WriteLine("----------------------------------------------------")

        ' Return the remaining portfolio value
        Return totalTrades
    End Function


    Private Function CalculateTrendStrength(heikinAshiData As List(Of HeikinAshiData), currentIndex As Integer) As Double
        ' Calculate short-term (fast) moving average
        Dim shortTermMA As Double = CalculateMovingAverage(heikinAshiData, currentIndex, 5)

        ' Calculate long-term (slow) moving average
        Dim longTermMA As Double = CalculateMovingAverage(heikinAshiData, currentIndex, 20)

        ' Calculate MACD (12, 26, 9)
        Dim macd As Double = CalculateMACD(heikinAshiData, currentIndex, 12, 26, 9)

        ' Calculate RSI (14 periods)
        Dim rsi As Double = CalculateRSI(heikinAshiData, currentIndex, 14)

        ' Combine indicators to determine trend strength
        Dim trendStrength As Double = shortTermMA - longTermMA + macd + rsi

        Return trendStrength
    End Function

    Private Function CalculateMovingAverage(heikinAshiData As List(Of HeikinAshiData), currentIndex As Integer, period As Integer) As Double
        ' Calculate the moving average of closing prices over the specified period
        Dim sum As Double = 0

        For i As Integer = Math.Max(0, currentIndex - period + 1) To currentIndex
            sum += heikinAshiData(i).Close
        Next

        Return sum / Math.Min(period, currentIndex + 1)
    End Function

    Private Function CalculateMACD(heikinAshiData As List(Of HeikinAshiData), currentIndex As Integer, shortPeriod As Integer, longPeriod As Integer, signalPeriod As Integer) As Double
        ' Calculate MACD (Moving Average Convergence Divergence)
        Dim shortTermEMA As Double = CalculateExponentialMovingAverage(heikinAshiData, currentIndex, shortPeriod)
        Dim longTermEMA As Double = CalculateExponentialMovingAverage(heikinAshiData, currentIndex, longPeriod)
        Dim macd As Double = shortTermEMA - longTermEMA

        ' Calculate Signal Line (9-day EMA of MACD)
        Dim signalLine As Double = CalculateExponentialMovingAverage(heikinAshiData, currentIndex, signalPeriod, macd)

        ' MACD Histogram (the difference between MACD and Signal Line)
        Dim macdHistogram As Double = macd - signalLine

        Return macdHistogram
    End Function

    Private Function CalculateRSI(heikinAshiData As List(Of HeikinAshiData), currentIndex As Integer, period As Integer) As Double
        ' Calculate RSI (Relative Strength Index)
        Dim gainSum As Double = 0
        Dim lossSum As Double = 0

        For i As Integer = Math.Max(1, currentIndex - period + 1) To currentIndex
            Dim priceDifference As Double = heikinAshiData(i).Close - heikinAshiData(i - 1).Close

            If priceDifference > 0 Then
                gainSum += priceDifference
            ElseIf priceDifference < 0 Then
                lossSum -= priceDifference
            End If
        Next

        Dim averageGain As Double = gainSum / period
        Dim averageLoss As Double = lossSum / period

        If averageLoss = 0 Then
            Return 100 ' Avoid division by zero
        Else
            Dim relativeStrength As Double = averageGain / averageLoss
            Dim rsi As Double = 100 - (100 / (1 + relativeStrength))
            Return rsi
        End If
    End Function

    Private Function CalculateExponentialMovingAverage(heikinAshiData As List(Of HeikinAshiData), currentIndex As Integer, period As Integer, Optional previousEMA As Double = 0) As Double
        ' Calculate Exponential Moving Average (EMA)
        Dim smoothingFactor As Double = 2 / (period + 1)
        Dim closePrice As Double = heikinAshiData(currentIndex).Close

        If currentIndex = 0 Then
            Return closePrice ' Initial EMA is the closing price
        Else
            Return (closePrice - previousEMA) * smoothingFactor + previousEMA
        End If
    End Function



End Class

' Define a class for trading history data
Public Class TradingViewHistory
    Public Property c As JArray
    Public Property h As JArray
    Public Property l As JArray
    Public Property o As JArray
    Public Property s As String
    Public Property t As JArray
    Public Property v As JArray
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