Public Class CommunicationPlugin
    Implements PluginManagerLibrary.PluginInterface

    Dim eb As New SerialPortKernel

    Private mainProgramm As PluginManagerLibrary.InterfaceFuerPlugins

    Public ReadOnly Property Beschreibung() As String Implements PluginManagerLibrary.PluginInterface.Beschreibung
        Get
            Return "Implementiert eine Verbindung zu einer Eisenbahn"
        End Get
    End Property

    Public ReadOnly Property Name() As String Implements PluginManagerLibrary.PluginInterface.Name
        Get
            Return "Communication Plugin"
        End Get
    End Property

    Public Function PluginInitalisieren() As Object() Implements PluginManagerLibrary.PluginInterface.PluginInitalisieren
        Dim obj() As Object = {eb}
        Return obj
    End Function

    Public Sub PluginStarten(ByVal Referenz As PluginManagerLibrary.InterfaceFuerPlugins) Implements PluginManagerLibrary.PluginInterface.pluginStarten
        mainProgramm = Referenz
    End Sub

    Public Sub PluginStoppen() Implements PluginManagerLibrary.PluginInterface.PluginStoppen
        eb.Beenden()
    End Sub
End Class
