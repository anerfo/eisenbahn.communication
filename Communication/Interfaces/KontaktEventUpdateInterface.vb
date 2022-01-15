''' <summary>
''' Wenn eine Klasse bei Kontakt-Events aktualisiert werden soll, muss sie dieses Interface implementieren
''' </summary>
''' <remarks></remarks>
Public Interface KontaktEventUpdateInterface
    ''' <summary>
    ''' Wird ausgeführt wenn sich ein Kontakt ändert. In 'Kontakte' werden alle sich geänderten Kontakte übergeben
    ''' </summary>
    ''' <param name="Kontakte">alle Kontakte die sich geändert haben</param>
    ''' <remarks></remarks>
    Sub update(ByVal Kontakte() As Klassen.Kontakt)
End Interface
