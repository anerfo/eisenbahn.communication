Imports System.Windows.Forms
Imports System.Threading

Public Class SerialPortKernel
    Inherits Kernel

#Const Sendewertevisualisieren = 0

#Region "Konstanten"
    Private Const NothaltAusloesen = 97
    Private Const NothaltFreigeben = 96
    Private Const WeicheAusschalten = 32
    Private Const WeicheRundSchalten = 33
    Private Const WeicheGeradeSchalten = 34
    Private Const LokUmdrehenGeschwindigkeit = 15
    Private Const RueckmeldemodulAuslesenEinzelnBasis = 192
    Private Const RueckmeldemodulAuslesenMehrereBasis = 128
    Private Const SonderfunktionenBasis = 64
    Private Const HauptfunktionAnOffset = 16

    Private Const BremsdauerBeimUmdrehen = 200
    Private WartezeitZwischenZweiGesendetenWeichenBefehlenInMS = 100
    Private Const WartezeitZwischenZweiGesendetenBefehlenInMS = 200
    Private Const WartezeitZwischenDatenUndAdresseInMS = 0

    Private Const KontaktAbfrageIntervallInMS = 500
#End Region

    'der Thread in dem die Kommunikation läuft
    Private ebCommunicationThread As Thread

    'Wird benötigt um ausgeführte Aktionen zurückzumelden
    Private asyncOp As System.ComponentModel.AsyncOperation = System.ComponentModel.AsyncOperationManager.CreateOperation(Me)

    'die Serielle Schnittstelle über die mit der Eisenbahn kommuniziert wird
    Private serialPort As System.IO.Ports.SerialPort
    Private spEinstellungen As New SerialPortEinstellungen
    Private letzterBefehlGesendet As New TimeSpan

    Private nothaltSollZustand As Boolean = False

    Private NothaltBefehlEventHandle As New extendedEventWaitHandle(False, EventResetMode.AutoReset, AddressOf NothaltBefehlVerarbeiten)
    Private LokBefehlEventHandle As New extendedEventWaitHandle(False, EventResetMode.AutoReset, AddressOf LokBefehlVerarbeiten)
    Private WeichenBefehlEventHandle As New extendedEventWaitHandle(False, EventResetMode.AutoReset, AddressOf WeichenBefehlVerarbeiten)
    Private StopEventHandle As New extendedEventWaitHandle(False, EventResetMode.AutoReset, AddressOf StopBefehlVerarbeiten)
    Private TimerEventHandle As New extendedEventWaitHandle(False, EventResetMode.AutoReset, AddressOf TimerBefehlVerarbeiten)

    'Wird benötigt um Threads zu synchronisieren
    Private nothaltRWLock As New ReaderWriterLock
    Private nothaltChangedCallback As SendOrPostCallback = AddressOf UpdateNothalt

    Private LoksRWLock As New ReaderWriterLock
    Private LoksChangedCallback As SendOrPostCallback = AddressOf UpdateLoks

    Private WeichenRWLock As New ReaderWriterLock
    Private WeichenChangedCallback As SendOrPostCallback = AddressOf UpdateWeichen

    Private KontakteRWLock As New ReaderWriterLock
    Private KontakteChangedCallback As SendOrPostCallback = AddressOf UpdateKontakte

    'Speichert und organisiert die Befehle
    Private Befehlsliste As New Befehlsstack

    Protected Overrides Sub Lokbefehl(ByVal nummer As Integer, ByVal funktion As Klassen.LokEigenschaften, ByVal wert As Integer)
        Dim sendewert As Byte = 0
        LoksRWLock.AcquireWriterLock(100)
        If funktion = Klassen.LokEigenschaften.Geschwindigkeit Then
            Loks(nummer).SollGeschwindigkeit = wert
            sendewert = Loks(nummer).SollGeschwindigkeit + (Loks(nummer).Hauptfunktion And 1) * HauptfunktionAnOffset
        ElseIf funktion = Klassen.LokEigenschaften.Hauptfunktion Then
            If wert = 0 Then
                Loks(nummer).Hauptfunktion = False
            Else
                Loks(nummer).Hauptfunktion = True
            End If
            sendewert = Loks(nummer).SollGeschwindigkeit + (Loks(nummer).Hauptfunktion And 1) * HauptfunktionAnOffset
        Else
            If funktion = Klassen.LokEigenschaften.Funktion1 Then
                If wert = 0 Then
                    Loks(nummer).Funktion1 = False
                Else
                    Loks(nummer).Funktion1 = True
                End If
            ElseIf funktion = Klassen.LokEigenschaften.Funktion2 Then
                If wert = 0 Then
                    Loks(nummer).Funktion2 = False
                Else
                    Loks(nummer).Funktion2 = True
                End If
            ElseIf funktion = Klassen.LokEigenschaften.Funktion3 Then
                If wert = 0 Then
                    Loks(nummer).Funktion3 = False
                Else
                    Loks(nummer).Funktion3 = True
                End If
            ElseIf funktion = Klassen.LokEigenschaften.Funktion4 Then
                If wert = 0 Then
                    Loks(nummer).Funktion4 = False
                Else
                    Loks(nummer).Funktion4 = True
                End If
            End If
            sendewert = SonderfunktionenBasis + boolToInt(Loks(nummer).Funktion1) + boolToInt(Loks(nummer).Funktion2) * 2 + boolToInt(Loks(nummer).Funktion3) * 4 + boolToInt(Loks(nummer).Funktion4) * 8
        End If
        LoksRWLock.ReleaseWriterLock()

        Befehlsliste.addBefehl(New Lokbefehl(nummer, sendewert))
        LokBefehlEventHandle.Set()
    End Sub

    Protected Overrides Sub LokUmdrehenBefehl(ByVal nummer As Integer)
        LoksRWLock.AcquireWriterLock(100)
        Loks(nummer).Geschwindigkeit = 0
        LoksRWLock.ReleaseWriterLock()

        Befehlsliste.addBefehl(New Lokbefehl(nummer, 15))
        LokBefehlEventHandle.Set()
    End Sub

    Protected Overrides Sub Weichenbefehl(ByVal nummer As Integer, ByVal wert As Klassen.WeichenRichtung)
        If wert = Klassen.WeichenRichtung.none Then
            Return
        End If
        Befehlsliste.addBefehl(New Weichenbefehl(nummer, wert))
        WeichenBefehlEventHandle.Set()
    End Sub

    Protected Overrides Sub NothaltBefehl(ByVal value As Boolean)
        nothaltSollZustand = value
        NothaltBefehlEventHandle.Set()
    End Sub

    Public Sub New()
        MyBase.New()

        'Thread starten
        ebCommunicationThread = New Thread(AddressOf ebComThreadSub)
        ebCommunicationThread.Name = "EBCommunicationThread"
        ebCommunicationThread.Priority = ThreadPriority.AboveNormal
        ebCommunicationThread.Start()

    End Sub

#Region "Eisenbahn Kommunikationsthread"
    Private Sub ebComThreadSub()
        Dim connected As Boolean = False
        Dim port As Integer = 0
        While Not connected
            serialPort = New System.IO.Ports.SerialPort("Com" & port, spEinstellungen.baudrate, spEinstellungen.paritaet, spEinstellungen.databits, spEinstellungen.stopbits)
            '            serialPort = New System.IO.Ports.SerialPort(spEinstellungen.portname, spEinstellungen.baudrate, spEinstellungen.paritaet, spEinstellungen.databits, spEinstellungen.stopbits)

            Try
                serialPort.Open()
                serialPort.DiscardInBuffer()
                serialPort.DiscardOutBuffer()
                wertSenden(RueckmeldemodulAuslesenMehrereBasis + 3) '3 Module auslesen => es sollten 6 Byte zurück kommen
                System.Threading.Thread.Sleep(250)
                Dim i As Integer = serialPort.BytesToRead
                If i = 6 Then 'Die wahrscheinlichkeit, dass ein anderes Gerät mit 6 Byte auf '131' antwortet, halte ich für gering.
                    connected = True
                Else
                    port += 1
                    serialPort.Close()
                End If
            Catch
                port += 1
                If port > 10 Then
                    Exit While
                End If
            End Try
        End While

        'Die Reihenfolge der EventHandlers bestimmt auch deren Priorität
        Dim ewhs() As extendedEventWaitHandle = { _
            NothaltBefehlEventHandle, StopEventHandle, TimerEventHandle, _
            WeichenBefehlEventHandle, LokBefehlEventHandle}

        Dim activeWaitHandle As Integer
        Dim timer1 As New System.Threading.Timer(timer1Callback, Nothing, 0, KontaktAbfrageIntervallInMS)

        'Hauptschleife des Kommunikationthreads
        While True
            'If Befehlsliste.wartendeBefehle Then
            '    If TypeOf Befehlsliste.getBefehlOhneLoeschen Is Lokbefehl Then
            '        LokBefehlVerarbeiten()
            '    ElseIf TypeOf Befehlsliste.getBefehlOhneLoeschen Is Weichenbefehl Then
            '        WeichenBefehlVerarbeiten()
            '    End If
            'Else
            'Auf ein Event bei den EventHandles warten
            activeWaitHandle = WaitHandle.WaitAny(ewhs)

            'Funktion des aktiven EventHandlers aufrufen in aktuellem Thread
            ewhs(activeWaitHandle).subToCall()

            While Befehlsliste.wartendeBefehle
                If TypeOf Befehlsliste.getBefehlOhneLoeschen Is Lokbefehl Then
                    LokBefehlVerarbeiten()
                ElseIf TypeOf Befehlsliste.getBefehlOhneLoeschen Is Weichenbefehl Then
                    WeichenBefehlVerarbeiten()
                End If
            End While
            'End If
        End While
    End Sub

    Private Sub wertSenden(ByVal value As Integer)
        If value > Byte.MaxValue Or value < Byte.MinValue Then
            value = Math.Abs(value Mod 256)
        End If
        Dim toSend As Byte = System.Convert.ToByte(value)
        wertSenden(toSend)
    End Sub

#If Sendewertevisualisieren = 1 Then
    Private form1 As New Form()
    Private listbox As New ListBox
#End If

    Private Sub wertSenden(ByVal value As Byte)
#If Sendewertevisualisieren = 1 Then
        If form1.Visible = False Then
            form1.Show()
            form1.Visible = True
            form1.Text = "Sendeliste"
            form1.Controls.Add(listbox)
            listbox.Dock = DockStyle.Fill
        End If
        If serialPort.IsOpen Then
            listbox.ForeColor = Drawing.Color.Green
        Else
            listbox.ForeColor = Drawing.Color.Red
        End If
        listbox.Items.Insert(0, {value}(0))
        Application.DoEvents()
#End If
        If serialPort.IsOpen Then
            serialPort.Write({value}, 0, 1)
        End If
    End Sub

    Private Sub NothaltBefehlVerarbeiten()
        If nothaltSollZustand = True Then
            wertSenden(NothaltAusloesen)
            Thread.Sleep(WartezeitZwischenZweiGesendetenBefehlenInMS)
            wertSenden(NothaltAusloesen) 'Als Sicherung ein zweites mal senden
            Thread.Sleep(WartezeitZwischenZweiGesendetenBefehlenInMS)
            wertSenden(WeicheAusschalten)

            nothaltRWLock.AcquireWriterLock(100)
            NothaltAktiv = True
            nothaltRWLock.ReleaseWriterLock()

            asyncOp.Post(nothaltChangedCallback, Nothing)
            Thread.Sleep(WartezeitZwischenZweiGesendetenBefehlenInMS)

            'Als nächstes muss der Nothalt wieder aufgehoben werden. Ansonsten kann nichts gesendet werden.
            'NothaltBefehlEventHandle.WaitOne(-1, False)
            'NothaltBefehlVerarbeiten()
            Return
        Else
            wertSenden(NothaltFreigeben)

            nothaltRWLock.AcquireWriterLock(100)
            NothaltAktiv = False
            nothaltRWLock.ReleaseWriterLock()
        End If

        asyncOp.Post(nothaltChangedCallback, Nothing)
        Thread.Sleep(WartezeitZwischenZweiGesendetenBefehlenInMS)
    End Sub

    Private Sub LokBefehlVerarbeiten()
        'Wenn keine Befehle in der Liste stehen, aussteigen
        If Befehlsliste.wartendeBefehle = 0 Then
            Return
        End If
        'Befehl aus der Liste holen
        Dim toSend() As Byte = Befehlsliste.getBefehl.getBytes

        If toSend(0) = 15 Then
            Dim stopSpeed As Integer = 0
            If Lok(toSend(1)).Hauptfunktion Then
                stopSpeed = HauptfunktionAnOffset
            End If
            'wenn Lok umgedreht werden soll

            wertSenden(stopSpeed)
            Thread.Sleep(WartezeitZwischenDatenUndAdresseInMS)
            wertSenden(toSend(1))
            Thread.Sleep(BremsdauerBeimUmdrehen)
            wertSenden(15 + stopSpeed)
            Thread.Sleep(WartezeitZwischenDatenUndAdresseInMS)
            wertSenden(toSend(1))
            Thread.Sleep(WartezeitZwischenZweiGesendetenBefehlenInMS)   'Hier steht die Lok sowieso => man braucht nicht zu warten
            wertSenden(stopSpeed)
            Thread.Sleep(WartezeitZwischenDatenUndAdresseInMS)
            wertSenden(toSend(1))
        Else
            'Bytes versenden
            'For i As Integer = 0 To toSend.Length - 1
            '    wertSenden(toSend(i))
            'Nexte
            wertSenden(toSend(0))
            Thread.Sleep(WartezeitZwischenDatenUndAdresseInMS)
            wertSenden(toSend(1))
            LoksRWLock.AcquireWriterLock(100)
            Loks(toSend(1)).Geschwindigkeit = Loks(toSend(1)).SollGeschwindigkeit
            LoksRWLock.ReleaseWriterLock()
        End If
        'Hauptthread benachrichtigen
        asyncOp.Post(LoksChangedCallback, Nothing)

        Thread.Sleep(WartezeitZwischenZweiGesendetenBefehlenInMS)
    End Sub

    Private Sub WeichenBefehlVerarbeiten(Optional ByVal tiefe As Integer = 0)
        'Wenn keine Befehle in der Liste stehen, aussteigen
        If Befehlsliste.wartendeBefehle = 0 Then
            Return
        End If
        'Befehl aus der Liste holen
        Dim bef As Weichenbefehl = Befehlsliste.getBefehl()
        Dim toSend() As Byte = bef.getBytes

        If toSend(0) = 1 Then
            toSend(0) = WeicheRundSchalten
        Else
            toSend(0) = WeicheGeradeSchalten
        End If

        Try
            WeichenRWLock.AcquireWriterLock(100)
            Weichen(bef.weichenNr).Richtung = bef.Richtung
            WeichenRWLock.ReleaseWriterLock()
        Catch ex As Exception
            MsgBox("Fehler: Weichen konnte nicht geschrieben werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
        End Try

        'Benachrichtige Hauptthread, dass Bytes gesendet wurden
        asyncOp.Post(WeichenChangedCallback, Nothing)

        'Sende Bytes
        If toSend.Length <> 2 Then
            Debug.Fail("Erwarte 2 Bytes zum Senden in einem Weichenbefehl")
        End If

        wertSenden(toSend(0))
        wertSenden(toSend(1))

        'Warte Zeit, bis Weiche geschaltet wurde
        Thread.Sleep(100) 'Schaltzeit einer Weiche

        wertSenden(WeicheAusschalten)

        Thread.Sleep(WartezeitZwischenZweiGesendetenWeichenBefehlenInMS)
        If bef.weichenNr = 28 Then 'Um ProgrammStart zu beschleunigen
            WartezeitZwischenZweiGesendetenWeichenBefehlenInMS = 600
        End If
    End Sub

    Private Sub TimerBefehlVerarbeiten()
        'Abfrage der Rückmeldungen
        wertSenden(CType(RueckmeldemodulAuslesenMehrereBasis + Klassen.Konstanten.AnzahlRueckmeldeModule / 2, Byte))

        Dim readByte As Byte
        Dim byteNumber As Integer = 0
#If Sendewertevisualisieren = 1 Then
        Dim allbytes As New List(Of Byte)
#End If

        If serialPort.IsOpen Then
            While serialPort.BytesToRead
                If byteNumber > Klassen.Konstanten.AnzahlRueckmeldeModule - 1 Then
                    serialPort.DiscardInBuffer()
                    Exit While
                End If

                readByte = serialPort.ReadByte()
#If Sendewertevisualisieren = 1 Then
                allbytes.Add(readByte)
#End If
                KontakteRWLock.AcquireWriterLock(100)
                For i As Integer = 0 To Klassen.Konstanten.AnzahlAnschluesseProRueckmeldemodul - 1
                    If byteNumber = 1 And i = 2 Then
                        Dim iasd As Int16 = 3
                    End If
                    If readByte Mod 2 = 1 Then
                        Rueckmeldungen(byteNumber, i).status = True
                    Else
                        Rueckmeldungen(byteNumber, i).status = False
                    End If
                    readByte = Int(readByte / 2 + Rueckmeldungen(byteNumber, i).status / 2)
                Next
                KontakteRWLock.ReleaseWriterLock()
                byteNumber += 1
            End While
        End If

#If Sendewertevisualisieren = 1 Then
        Dim text As String = ""
        For i As Integer = 0 To allbytes.Count - 1
            text &= "(" & allbytes(i) & ")"
        Next
        If text <> "" Then
            listbox.Items.Insert(0, text)
        End If
        listbox.Update()
#End If
            Thread.Sleep(WartezeitZwischenZweiGesendetenBefehlenInMS)

            'Benachrichtige Hauptthread, dass Bytes gesendet wurden
            asyncOp.Post(KontakteChangedCallback, Nothing)
    End Sub

    Private Sub StopBefehlVerarbeiten() 'beendet den laufenden Thread - EBCommunicationThread
        'Falls irgendwie mal ein anderer Thread diese sub aufruft
        wertSenden(WeicheAusschalten)
        serialPort.Close()
        If Thread.CurrentThread.Name = ebCommunicationThread.Name Then
            Thread.CurrentThread.Abort()
        Else
            MsgBox("Ein falscher Thread hat diese Sub aufgerufen!", MsgBoxStyle.OkOnly, "Fehler")
        End If
    End Sub

    Private timer1Callback As New TimerCallback(AddressOf timerRoutine)

    Private Sub timerRoutine()
        TimerEventHandle.Set()
    End Sub
#End Region

#Region "Overrides damit nach Locks geschaut wird, wegen Threads"
    Public Overrides Property Nothalt() As Boolean
        Get
            Dim res As Boolean = Nothing
            Try
                nothaltRWLock.AcquireReaderLock(100)
                res = NothaltAktiv
                nothaltRWLock.ReleaseReaderLock()
            Catch ex As Exception
                MsgBox("Fehler: Nothalt konnte nicht gelesen werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
            End Try
            Return res
        End Get
        Set(ByVal value As Boolean)
            Try
                NothaltBefehl(value)
            Catch
                MsgBox("Fehler: Nothalt konnte nicht geschrieben werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
            End Try
        End Set
    End Property

    Public Overrides Function holeAlleKontakte() As Klassen.Kontakt(,)
        Dim res As Klassen.Kontakt(,) = Nothing
        Try
            KontakteRWLock.AcquireReaderLock(100)
            res = MyBase.holeAlleKontakte()
        Catch ex As Exception
            MsgBox("Fehler: Kontakte konnten nicht gelesen werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
        End Try
        KontakteRWLock.ReleaseReaderLock()
        Return res
    End Function

    Public Overrides Function holeAlleWeichen() As Klassen.Weiche()
        Dim res As Klassen.Weiche() = Nothing
        Try
            WeichenRWLock.AcquireReaderLock(100)
            res = MyBase.holeAlleWeichen
        Catch ex As Exception
            MsgBox("Fehler: Weichen konnten nicht gelesen werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
        End Try
        WeichenRWLock.ReleaseReaderLock()
        Return res
    End Function

    Public Overrides Function holeAlleLoks() As Klassen.Lok()
        Dim res As Klassen.Lok() = Nothing
        Try
            LoksRWLock.AcquireReaderLock(100)
            res = MyBase.holeAlleLoks
        Catch ex As Exception
            MsgBox("Fehler: Loks konnten nicht gelesen werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
        End Try
        LoksRWLock.ReleaseReaderLock()
        Return res
    End Function

    Public Overrides ReadOnly Property Lok(ByVal Nummer As Integer) As Klassen.Lok
        Get
            Dim res As Klassen.Lok = Nothing
            Try
                LoksRWLock.AcquireReaderLock(100)
                res = MyBase.Lok(Nummer)
            Catch ex As Exception
                MsgBox("Fehler: Lok konnte nicht gelesen werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
            End Try
            LoksRWLock.ReleaseReaderLock()
            Return res
        End Get
    End Property

    Public Overrides ReadOnly Property Rueckmeldung(ByVal Modul As Integer, ByVal Nummer As Integer) As Klassen.Kontakt
        Get
            Dim res As Klassen.Kontakt = Nothing
            Try
                KontakteRWLock.AcquireReaderLock(100)
                res = MyBase.Rueckmeldung(Modul, Nummer)
            Catch ex As Exception
                MsgBox("Fehler: Rueckmeldung konnte nicht gelesen werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
            End Try
            KontakteRWLock.ReleaseReaderLock()
            Return res
        End Get
    End Property

    Public Overrides ReadOnly Property Weiche(ByVal Nummer As Integer) As Klassen.Weiche
        Get
            Dim res As Klassen.Weiche = Nothing
            Try
                WeichenRWLock.AcquireReaderLock(100)
                res = MyBase.Weiche(Nummer)
            Catch ex As Exception
                MsgBox("Fehler: Rueckmeldung konnte nicht gelesen werden, da von einem anderen Thread gelockt.", MsgBoxStyle.OkOnly, "Fehler")
            End Try
            WeichenRWLock.ReleaseReaderLock()
            Return res
        End Get
    End Property

    Public Overrides Sub beenden()
        'thread muss beendet werden
        StopEventHandle.Set()
    End Sub
#End Region

    Private Function boolToInt(ByVal value As Boolean) As Integer
        If value = True Then
            Return 1
        Else
            Return 0
        End If
    End Function

End Class
