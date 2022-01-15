Imports System.Threading
Public Class extendedEventWaitHandle
    Inherits EventWaitHandle

    Public Sub New(ByVal initialState As Boolean, ByVal mode As System.Threading.EventResetMode, ByVal subToCall1 As SubToCallDelegate)
        MyBase.New(initialState, mode)
        subToCall = subToCall1
    End Sub

    Public Sub New(ByVal initialState As Boolean, ByVal mode As System.Threading.EventResetMode, ByVal name As String)
        MyBase.New(initialState, mode, name)
    End Sub

    Public Sub New(ByVal initialState As Boolean, ByVal mode As System.Threading.EventResetMode, ByVal name As String, ByVal createdNew As Boolean)
        MyBase.New(initialState, mode, name, createdNew)
    End Sub

    Public Sub New(ByVal initialState As Boolean, ByVal mode As System.Threading.EventResetMode, ByVal name As String, ByVal createdNew As Boolean, ByVal eventSecurity As System.Security.AccessControl.EventWaitHandleSecurity)
        MyBase.New(initialState, mode, name, createdNew, eventSecurity)
    End Sub

    Public Delegate Sub SubToCallDelegate()
    'Public Delegate Sub SubToCallWithParametersDelegate(ByVal parameters() As Object)

    Public subToCall As SubToCallDelegate
    'Public subtoCallWithParameters As SubToCallWithParametersDelegate
End Class
