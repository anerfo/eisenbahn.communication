Public Interface KernelInterface
    ReadOnly Property lok(ByVal Nummer As Integer) As Klassen.Lok
    ReadOnly Property weiche(ByVal Nummer As Integer) As Klassen.Weiche
    ReadOnly Property rueckmeldung(ByVal Modul As Integer, ByVal Nummer As Integer) As Klassen.Kontakt

    Function holeAlleWeichen() As Klassen.Weiche()
    Function holeAlleLoks() As Klassen.Lok()
    Function holeAlleKontakte() As Klassen.Kontakt(,)
    Function holeAlleGeaendertenKontakte() As Klassen.Kontakt()

    Sub lokSteuern(ByVal nummer As Integer, ByVal funktion As Klassen.LokEigenschaften, ByVal wert As Integer)
    Sub lokUmdrehen(ByVal nummer As Integer)
    Sub weicheSchalten(ByVal nummer As Integer, ByVal Richtung As Klassen.WeichenRichtung)

    Sub registriereFuerLokEvents(ByRef Referenz As LokEventUpdateInterface)
    Sub registriereFuerWeichenEvents(ByRef Referenz As WeichenEventUpdateInterface)
    Sub registriereFuerKontaktEvents(ByRef Referenz As KontaktEventUpdateInterface)
    Sub registriereFuerNothaltEvents(ByRef Referenz As NothaltEventUpdateInterface)

    Property nothalt() As Boolean
End Interface
