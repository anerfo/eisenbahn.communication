Imports System.IO.Ports

Public Class SerialPortEinstellungen
    Public portname As String = "Com4"
    Public baudrate As Integer = 2400
    Public startbits As Integer = 0
    Public databits As Integer = 8
    Public stopbits As StopBits = IO.Ports.StopBits.Two
    Public paritaet As Parity = Parity.None
End Class
