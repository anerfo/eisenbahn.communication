Public MustInherit Class Befehl

    Public MustOverride Function getBytes() As Byte()

    Public MustOverride Function vergleich(ByVal zuVergleichen As Befehl) As Boolean

End Class
