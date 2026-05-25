' Launches MediaConverter.exe without showing a console window.
' Double-click this file to run the app.

Option Explicit

Dim shell, fso, scriptDir, exePath

Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
exePath = scriptDir & "\src\YoutubeConverter\bin\Release\net8.0-windows\MediaConverter.exe"

If Not fso.FileExists(exePath) Then
    ' Fallback: Debug build
    exePath = scriptDir & "\src\YoutubeConverter\bin\Debug\net8.0-windows\MediaConverter.exe"
End If

If Not fso.FileExists(exePath) Then
    MsgBox "Could not find MediaConverter.exe." & vbCrLf & vbCrLf & _
           "Run first:  dotnet build -c Release" & vbCrLf & _
           "in the folder src\YoutubeConverter", vbExclamation, "Media Converter"
    WScript.Quit 1
End If

shell.Run """" & exePath & """", 1, False
