Public Class DummyKernelIFClass
    Implements KernelInterface

    Public Function holeAlleKontakte() As Klassen.Kontakt(,) Implements KernelInterface.holeAlleKontakte
        Dim result(0, 0) As Klassen.Kontakt
        result(0, 0) = New Klassen.Kontakt
        Return result
    End Function

    Public Function holeAlleLoks() As Klassen.Lok() Implements KernelInterface.holeAlleLoks
        Dim result(0) As Klassen.Lok
        result(0) = New Klassen.Lok
        Return result
    End Function

    Public Function holeAlleWeichen() As Klassen.Weiche() Implements KernelInterface.holeAlleWeichen
        Dim result(0) As Klassen.Weiche
        result(0) = New Klassen.Weiche
        Return result
    End Function

    Public ReadOnly Property lok(ByVal Nummer As Integer) As Klassen.Lok Implements KernelInterface.lok
        Get
            Dim result As Klassen.Lok
            result = New Klassen.Lok
            Return result
        End Get
    End Property

    Public Sub lokSteuern(ByVal nummer As Integer, ByVal funktion As Klassen.LokEigenschaften, ByVal wert As Integer) Implements KernelInterface.lokSteuern

    End Sub

    Public Sub lokUmdrehen(ByVal nummer As Integer) Implements KernelInterface.lokUmdrehen

    End Sub

    Public Property nothalt As Boolean Implements KernelInterface.nothalt
        Get
            Return False
        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property

    Public Sub registriereFuerKontaktEvents(ByRef Referenz As KontaktEventUpdateInterface) Implements KernelInterface.registriereFuerKontaktEvents

    End Sub

    Public Sub registriereFuerLokEvents(ByRef Referenz As LokEventUpdateInterface) Implements KernelInterface.registriereFuerLokEvents

    End Sub

    Public Sub registriereFuerNothaltEvents(ByRef Referenz As NothaltEventUpdateInterface) Implements KernelInterface.registriereFuerNothaltEvents

    End Sub

    Public Sub registriereFuerWeichenEvents(ByRef Referenz As WeichenEventUpdateInterface) Implements KernelInterface.registriereFuerWeichenEvents

    End Sub

    Public ReadOnly Property rueckmeldung(ByVal Modul As Integer, ByVal Nummer As Integer) As Klassen.Kontakt Implements KernelInterface.rueckmeldung
        Get
            Dim result As Klassen.Kontakt
            result = New Klassen.Kontakt
            Return result
        End Get
    End Property

    Public ReadOnly Property weiche(ByVal Nummer As Integer) As Klassen.Weiche Implements KernelInterface.weiche
        Get
            Dim result As Klassen.Weiche
            result = New Klassen.Weiche
            Return result
        End Get
    End Property

    Public Sub weicheSchalten(ByVal nummer As Integer, ByVal Richtung As Klassen.WeichenRichtung) Implements KernelInterface.weicheSchalten

    End Sub

    Public Function holeAlleGeaendertenKontakte() As Klassen.Kontakt() Implements KernelInterface.holeAlleGeaendertenKontakte
        Return {New Klassen.Kontakt}
    End Function
End Class
