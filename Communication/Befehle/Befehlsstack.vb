Public Class Befehlsstack
    Private Befehlsliste As New List(Of Befehl)

    Private lockStack As New Object

    Public Sub addBefehl(ByVal hinzuzufuegenderBefehl As Befehl)
        SyncLock lockStack
            For i As Integer = 0 To Befehlsliste.Count - 1
                If Befehlsliste.Item(i).vergleich(hinzuzufuegenderBefehl) Then
                    Befehlsliste.RemoveAt(i)
                    Befehlsliste.Insert(i, hinzuzufuegenderBefehl)
                    Return
                End If
            Next
            Befehlsliste.Add(hinzuzufuegenderBefehl)
        End SyncLock
    End Sub

    Public Function getBefehl() As Befehl
        Dim res As Befehl = Nothing
        SyncLock lockStack
            If Befehlsliste.Count > 0 Then
                res = Befehlsliste.Item(0)
                Befehlsliste.RemoveAt(0)
            End If
        End SyncLock
        'Damit der Spashscreen upgedated werden kann
        Klassen.GlobaleVariablen.AnzahlBefehle = Befehlsliste.Count
        Return res
    End Function

    Public Function wartendeBefehle() As Integer
        Return Befehlsliste.Count
    End Function

    Public Function getBefehlOhneLoeschen() As Befehl
        Dim res As Befehl = Nothing
        SyncLock lockStack
            If Befehlsliste.Count > 0 Then
                res = Befehlsliste.Item(0)
            End If
        End SyncLock
        Return res
    End Function

End Class
