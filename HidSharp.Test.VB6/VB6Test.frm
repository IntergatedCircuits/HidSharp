VERSION 5.00
Begin VB.Form Form1 
   ClientHeight    =   3060
   ClientLeft      =   120
   ClientTop       =   420
   ClientWidth     =   4560
   LinkTopic       =   "Form1"
   ScaleHeight     =   3060
   ScaleWidth      =   4560
   StartUpPosition =   3  'Windows Default
   Begin VB.Timer Timer1 
      Interval        =   100
      Left            =   2280
      Top             =   1560
   End
   Begin VB.Label Label2 
      Alignment       =   2  'Center
      Height          =   855
      Left            =   360
      TabIndex        =   1
      Top             =   1680
      Width           =   3975
   End
   Begin VB.Label Label1 
      Alignment       =   2  'Center
      Height          =   855
      Left            =   240
      TabIndex        =   0
      Top             =   360
      Width           =   3975
   End
End
Attribute VB_Name = "Form1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Private hidLoader As HidDeviceLoader
Private hidDevice As hidDevice
Private hidStream As Object
Private dymoScale As dymoScale

Private Sub Timer1_Timer()
    If hidLoader Is Nothing Then
        Set hidLoader = New HidDeviceLoader
    End If
    
    If hidDevice Is Nothing Then
        Set hidDevice = hidLoader.GetDeviceOrDefault(24726, 344)
    End If
    
    If Not hidDevice Is Nothing Then
        Me.Caption = "Scale plugged in"
        
        On Error GoTo HadError
        Set hidStream = hidDevice.Open
    Else
        Me.Caption = "Scale not plugged in"
    End If
    
    If dymoScale Is Nothing Then
        Set dymoScale = New dymoScale
        Set dymoScale.stream = hidStream
        
        Dim value As Long, exponent As Long
        Dim unit As String, status As String
        Dim buffered As Boolean
        
        On Error GoTo HadError
        dymoScale.ReadSample value, exponent, unit, status, buffered
        
        Label1.Caption = CStr(value) + "x10^" + CStr(exponent) + " " + unit
        Label2.Caption = status + IIf(buffered, " B", "")
    End If
    
HadError:
    Set dymoScale = Nothing
    
    If Not hidStream Is Nothing Then
        hidStream.Close
        Set hidStream = Nothing
    End If
    
    Set hidDevice = Nothing
    Set hidLoader = Nothing
    Exit Sub
End Sub
