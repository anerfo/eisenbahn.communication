Public Class Weichenbefehl
    Inherits Befehl

    Private iWeichennummer As Klassen.WeichenRichtung
    Private iSendewert As Klassen.WeichenRichtung

    Public Sub New(ByVal Weichennummer As Integer, ByVal Sendewert As Klassen.WeichenRichtung)
        iWeichennummer = Weichennummer
        iSendewert = Sendewert
    End Sub

    Public Overrides Function getBytes() As Byte()
        Dim res(1) As Byte
        res(0) = iSendewert
        res(1) = iWeichennummer
        Return res
    End Function

    Public Overrides Function vergleich(ByVal zuVergleichen As Befehl) As Boolean
        If Not TypeOf zuVergleichen Is Weichenbefehl Then
            Return False
        End If
        If CType(zuVergleichen, Weichenbefehl).iWeichennummer = iWeichennummer Then
            'Wenn Loknummer überein stimmt, ist Vergleich positiv
            Return True
        End If
        Return False
    End Function

    Public ReadOnly Property weichenNr As Integer
        Get
            Return iWeichennummer
        End Get
    End Property

    Public ReadOnly Property Richtung As Klassen.WeichenRichtung
        Get
            Return iSendewert
        End Get
    End Property
End Class
