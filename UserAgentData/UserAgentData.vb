Imports Newtonsoft.Json
Imports System.IO

Public Class UserAgentData
    Public Property ua As String
    Public Property pct As Double
End Class

Module Module1
    Sub xMain()
        ' Specify the path to your JSON file
        Dim jsonFilePath As String = "ua/AgentData.json"

        ' Read the JSON file
        Dim jsonData As String = File.ReadAllText(jsonFilePath)

        ' Deserialize JSON into a List(Of Dictionary(Of String, List(Of UserAgentData)))
        Dim userAgentDictList As List(Of Dictionary(Of String, List(Of UserAgentData))) = JsonConvert.DeserializeObject(Of List(Of Dictionary(Of String, List(Of UserAgentData))))(jsonData)

        ' Example: Display a random desktop user-agent
        Dim randomDesktopUserAgent As String = GetRandomUserAgent(userAgentDictList(0)("desktop"))
        Console.WriteLine($"Random Desktop User-Agent: {randomDesktopUserAgent}")

        ' Example: Display a random mobile user-agent
        Dim randomMobileUserAgent As String = GetRandomUserAgent(userAgentDictList(0)("mobile"))
        Console.WriteLine($"Random Mobile User-Agent: {randomMobileUserAgent}")

        Console.ReadLine()
    End Sub

    Function GetRandomUserAgent(userAgentList As List(Of UserAgentData)) As String
        ' Select a random user-agent from the list based on percentages
        Dim totalPercentage As Double = userAgentList.Sum(Function(ua) ua.pct)
        Dim randomPercentage As Double = New Random().NextDouble() * totalPercentage

        For Each userAgentEntry As UserAgentData In userAgentList
            randomPercentage -= userAgentEntry.pct
            If randomPercentage <= 0 Then
                Return userAgentEntry.ua
            End If
        Next

        ' Fallback: Return the first user-agent if percentages don't add up to 100%
        Return userAgentList.First().ua
    End Function
End Module
