Public MustInherit Class Kernel
    Implements KernelInterface


    'Speichern der Werte auf der Anlage----------------
    'speichert die Rückmeldungen
    Protected Shared Rueckmeldungen(,) As Klassen.Kontakt
    Protected Shared RueckmeldungenAlt(,) As Klassen.Kontakt
    Protected Shared gewechselteRueckmeldungen() As Klassen.Kontakt


    'speichert den Zustand der Loks
    Protected Shared Loks() As Klassen.Lok

    'speichert den Zustand der Weichen
    Protected Shared Weichen() As Klassen.Weiche

    'speichert den Zustand von Nothalt
    Protected Shared NothaltAktiv As Boolean = False
    '--------------------------------------------------

    'Beobachter die upgedatet werden müssen, wenn ein Event auftritt
    Private LokEventBeobachter() As LokEventUpdateInterface
    Private WeichenEventBeobachter() As WeichenEventUpdateInterface
    Private KontaktEventBeobachter() As KontaktEventUpdateInterface
    Private NothaltEventBeobachter() As NothaltEventUpdateInterface

    Public Sub New()
        'initialisiert den Speicher für die Lokzustände
        ReDim Loks(Klassen.Konstanten.AnzahlLoks - 1)
        For i As Integer = 0 To Klassen.Konstanten.AnzahlLoks - 1
            Loks(i) = New Klassen.Lok()
            Loks(i).Nummer = i
            Loks(i).Hauptfunktion = False
            Loks(i).Geschwindigkeit = 0
            Loks(i).SollGeschwindigkeit = 0
            Loks(i).Funktion1 = False
            Loks(i).Funktion2 = False
            Loks(i).Funktion3 = False
            Loks(i).Funktion4 = False
        Next

        'initialisiert den Speicher für die Weichen
        ReDim Weichen(Klassen.Konstanten.AnzahlWeichen - 1)
        For i As Integer = 0 To Klassen.Konstanten.AnzahlWeichen - 1
            Weichen(i) = New Klassen.Weiche
            Weichen(i).Nummer = i
            Weichen(i).Richtung = Klassen.WeichenRichtung.none
        Next

        'initialisiert den Speicher für die Weichen
        ReDim Rueckmeldungen(Klassen.Konstanten.AnzahlRueckmeldeModule - 1, Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul - 1)
        ReDim RueckmeldungenAlt(Klassen.Konstanten.AnzahlRueckmeldeModule - 1, Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul - 1)
        For i As Integer = 0 To Klassen.Konstanten.AnzahlRueckmeldeModule - 1
            For q As Integer = 0 To Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul - 1
                Rueckmeldungen(i, q) = New Klassen.Kontakt
                Rueckmeldungen(i, q).Modul = i
                Rueckmeldungen(i, q).Adresse = q
                Rueckmeldungen(i, q).status = False
                RueckmeldungenAlt(i, q) = New Klassen.Kontakt
                RueckmeldungenAlt(i, q).Modul = i
                RueckmeldungenAlt(i, q).Adresse = q
                RueckmeldungenAlt(i, q).status = False
            Next
        Next
    End Sub

    'Registrierungen als Beobachter
    Public Overridable Sub registriereFuerLokEvents(ByRef Referenz As LokEventUpdateInterface) Implements KernelInterface.registriereFuerLokEvents
        If LokEventBeobachter Is Nothing Then
            ReDim LokEventBeobachter(0)
        Else
            ReDim Preserve LokEventBeobachter(LokEventBeobachter.Length)
        End If
        LokEventBeobachter(LokEventBeobachter.Length - 1) = Referenz
    End Sub

    Public Overridable Sub registriereFuerKontaktEvents(ByRef Referenz As KontaktEventUpdateInterface) Implements KernelInterface.registriereFuerKontaktEvents
        If KontaktEventBeobachter Is Nothing Then
            ReDim KontaktEventBeobachter(0)
        Else
            ReDim Preserve KontaktEventBeobachter(KontaktEventBeobachter.Length)
        End If
        KontaktEventBeobachter(KontaktEventBeobachter.Length - 1) = Referenz
    End Sub

    Public Overridable Sub registriereFuerWeichenEvents(ByRef Referenz As WeichenEventUpdateInterface) Implements KernelInterface.registriereFuerWeichenEvents
        If WeichenEventBeobachter Is Nothing Then
            ReDim WeichenEventBeobachter(0)
        Else
            ReDim Preserve WeichenEventBeobachter(WeichenEventBeobachter.Length)
        End If
        WeichenEventBeobachter(WeichenEventBeobachter.Length - 1) = Referenz
    End Sub

    Public Sub registriereFuerNothaltEvents(ByRef Referenz As NothaltEventUpdateInterface) Implements KernelInterface.registriereFuerNothaltEvents
        If NothaltEventBeobachter Is Nothing Then
            ReDim NothaltEventBeobachter(0)
        Else
            ReDim Preserve NothaltEventBeobachter(NothaltEventBeobachter.Length)
        End If
        NothaltEventBeobachter(NothaltEventBeobachter.Length - 1) = Referenz
    End Sub

    Protected Overridable Sub UpdateLoks()
        'Alle Loks updaten
        If Not LokEventBeobachter Is Nothing Then
            For i As Integer = 0 To LokEventBeobachter.Length - 1
                LokEventBeobachter(i).update(Loks)
            Next
        End If
    End Sub

    Protected Overridable Sub UpdateWeichen()
        'Alle Weichen updaten
        If Not WeichenEventBeobachter Is Nothing Then
            For i As Integer = 0 To WeichenEventBeobachter.Length - 1
                WeichenEventBeobachter(i).update(Weichen)
            Next
        End If
    End Sub

    Protected Overridable Sub UpdateKontakte()
        Dim geaendert() As Klassen.Kontakt = geaenderteKontakte()
        'Alle geänderten Kontakte updaten
        If Not KontaktEventBeobachter Is Nothing And Not geaendert Is Nothing Then
            For i As Integer = 0 To KontaktEventBeobachter.Length - 1
                KontaktEventBeobachter(i).update(geaendert)
            Next
        End If
    End Sub

    Protected Function geaenderteKontakte() As Klassen.Kontakt()
        'Findet alle geänderten Kontakte
        Dim gewechselteRueckmeldungen() As Klassen.Kontakt = Nothing
        For i As Integer = 0 To Klassen.Konstanten.AnzahlRueckmeldeModule - 1
            For q As Integer = 0 To Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul - 1
                'Wenn alter Kontakt ungleich neuem Status hat er sich geändert
                If Rueckmeldungen(i, q).status <> RueckmeldungenAlt(i, q).status Then
                    If gewechselteRueckmeldungen Is Nothing Then
                        ReDim gewechselteRueckmeldungen(0)
                    Else
                        ReDim Preserve gewechselteRueckmeldungen(gewechselteRueckmeldungen.Length)
                    End If
                    gewechselteRueckmeldungen(gewechselteRueckmeldungen.Length - 1) = Rueckmeldungen(i, q)
                    RueckmeldungenAlt(i, q).status = Rueckmeldungen(i, q).status
                End If
            Next
        Next
        'If found = True Then
        '    RueckmeldungenAlt = Rueckmeldungen.Clone
        'End If
        Return gewechselteRueckmeldungen
    End Function

    Protected Overridable Sub UpdateNothalt()
        'Alle geänderten Kontakte updaten
        If Not NothaltEventBeobachter Is Nothing Then
            For i As Integer = 0 To NothaltEventBeobachter.Length - 1
                NothaltEventBeobachter(i).update(NothaltAktiv)
            Next
        End If
    End Sub

    Public Overridable ReadOnly Property Lok(ByVal Nummer As Integer) As Klassen.Lok Implements KernelInterface.lok
        Get
            If Nummer < Klassen.Konstanten.AnzahlLoks Then
                Return Loks(Nummer).Clone
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Overridable ReadOnly Property Rueckmeldung(ByVal Modul As Integer, ByVal Nummer As Integer) As Klassen.Kontakt Implements KernelInterface.rueckmeldung
        Get
            If Modul < Klassen.Konstanten.AnzahlRueckmeldeModule And Nummer < Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul Then
                Return Rueckmeldungen(Modul, Nummer).Clone()
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Overridable ReadOnly Property Weiche(ByVal Nummer As Integer) As Klassen.Weiche Implements KernelInterface.weiche
        Get
            If Nummer < Klassen.Konstanten.AnzahlWeichen Then
                Return Weichen(Nummer).Clone
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Overridable Function holeAlleKontakte() As Klassen.Kontakt(,) Implements KernelInterface.holeAlleKontakte
        'Gibt alle Rueckmeldungen zurück
        Dim result(Klassen.Konstanten.AnzahlRueckmeldeModule - 1, Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul - 1) As Klassen.Kontakt
        For i As Integer = 0 To Klassen.Konstanten.AnzahlRueckmeldeModule - 1
            For q As Integer = 0 To Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul - 1
                result(i, q) = Rueckmeldungen(i, q).Clone
            Next
        Next
        Return result
    End Function

    Public Overridable Function holeAlleLoks() As Klassen.Lok() Implements KernelInterface.holeAlleLoks
        'Gibt alle Loks zurück
        Dim result(Klassen.Konstanten.AnzahlLoks - 1) As Klassen.Lok
        For i As Integer = 0 To Klassen.Konstanten.AnzahlLoks - 1
            result(i) = Loks(i).Clone
        Next
        Return result
    End Function

    Public Overridable Function holeAlleWeichen() As Klassen.Weiche() Implements KernelInterface.holeAlleWeichen
        'Gibt alle Weichen zurück
        Dim result(Klassen.Konstanten.AnzahlWeichen - 1) As Klassen.Weiche
        For i As Integer = 0 To Klassen.Konstanten.AnzahlWeichen - 1
            result(i) = Weichen(i).Clone
        Next
        Return result
    End Function

    Protected MustOverride Sub Lokbefehl(ByVal nummer As Integer, ByVal funktion As Klassen.LokEigenschaften, ByVal wert As Integer)
    Protected MustOverride Sub LokUmdrehenBefehl(ByVal nummer As Integer)

    Protected MustOverride Sub Weichenbefehl(ByVal nummer As Integer, ByVal wert As Klassen.WeichenRichtung)

    Protected MustOverride Sub NothaltBefehl(ByVal value As Boolean)

    Public MustOverride Sub beenden()


    Public Sub LokSteuern(ByVal nummer As Integer, ByVal funktion As Klassen.LokEigenschaften, ByVal wert As Integer) Implements KernelInterface.lokSteuern
        'wird an child-Klasse delegiert
        Lokbefehl(nummer, funktion, wert)
    End Sub

    Public Sub WeicheSchalten(ByVal nummer As Integer, ByVal Richtung As Klassen.WeichenRichtung) Implements KernelInterface.weicheSchalten
        'wird an child-Klasse delegiert
        Weichenbefehl(nummer, Richtung)
    End Sub

    Public Overridable Property Nothalt() As Boolean Implements KernelInterface.nothalt
        Get
            Return NothaltAktiv
        End Get
        Set(ByVal value As Boolean)
            NothaltBefehl(value)
        End Set
    End Property

    Public Sub lokUmdrehen(ByVal nummer As Integer) Implements KernelInterface.lokUmdrehen
        LokUmdrehenBefehl(nummer)
    End Sub

    Public Function holeAlleGeaendertenKontakte() As Klassen.Kontakt() Implements KernelInterface.holeAlleGeaendertenKontakte
        Return geaenderteKontakte()
    End Function
End Class
