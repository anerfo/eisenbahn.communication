Public Class Lokbefehl
    Inherits Befehl

    Private iLoknummer As Byte
    Private iSendewert As Byte

    Public Overrides Function getBytes() As Byte()
        Dim res(1) As Byte
        res(0) = iSendewert
        res(1) = iLoknummer
        Return res
    End Function

    Public Sub New(ByVal Loknummer As Integer, ByVal Sendewert As Integer)
        iLoknummer = System.Convert.ToByte(Loknummer)
        iSendewert = System.Convert.ToByte(Sendewert)
    End Sub

    Public Overrides Function vergleich(ByVal zuVergleichen As Befehl) As Boolean
        If Not TypeOf zuVergleichen Is Lokbefehl Then
            Return False
        End If
        If CType(zuVergleichen, Lokbefehl).iLoknummer = iLoknummer Then
            'Wenn Loknummer überein stimmt, ist Vergleich positiv
            Return True
        End If
        Return False
    End Function
End Class
