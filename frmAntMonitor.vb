﻿Imports MAntMonitor.Extensions

Public Class frmMain

    Public Event StartupNextInstance As Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventHandler

    Private ToldUserRunningInNotificationTray As Boolean

    Private wb(0 To 2) As WebBrowser
    Private iBrowserSubmitted As Integer

    Private RebootInfo, EMailAlertInfo As System.Collections.Generic.Dictionary(Of String, Date)

    Private Shared colResponses As System.Collections.Generic.List(Of String)

    Private ds As DataSet

    Private Const csRegKey As String = "Software\MAntMonitor"

    Private Const csVersion As String = "M's Ant Monitor v2.4"

    Private iCountDown, iWatchDog, bAnt As Integer

    Private iRefreshRate As Integer

    Private ctlsByKey As ControlsByRegistry

    Private bStarted As Boolean
    Private bSortingColumns As Boolean

    Private Enum enAntType
        S1
        S2
    End Enum

    Private Class clsPoolData
        Public URL As String
        Public UID As String
        Public PW As String

        Public Sub New()
            PW = ""
        End Sub
    End Class

#If DEBUG Then
    Private Const bErrorHandle As Boolean = False
#Else
    Private Const bErrorHandle As Boolean = True
#End If

    Private Sub Form1_Load(sender As Object, e As System.EventArgs) Handles Me.Load

        Dim host As System.Net.IPHostEntry
        Dim x As Integer

        bStarted = True

        Me.Text = csVersion

        AddToLog(csVersion & " starting")

        host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName)

        For Each IP As System.Net.IPAddress In host.AddressList
            If IP.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then
                Me.cmbLocalIPs.Items.Add(IP.ToString)
            End If
        Next

        Me.cmbLocalIPs.Text = Me.cmbLocalIPs.Items(0)

        RebootInfo = New System.Collections.Generic.Dictionary(Of String, Date)
        EMailAlertInfo = New System.Collections.Generic.Dictionary(Of String, Date)

        ds = New DataSet

        bSortingColumns = True

        With ds
            .Tables.Add()
            Me.dataAnts.DataSource = .Tables(0)

            With .Tables(0).Columns
                .Add("Name")
                .Add("Uptime")
                .Add("GH/s(5s)", GetType(Double))
                .Add("GH/s(avg)", GetType(Double))
                .Add("Blocks")
                .Add("HWE%")
                .Add("BestShare")
                .Add("Diff")
                .Add("Pools")
                .Add("PoolData")
                .Add("PoolData2", GetType(Object))
                .Add("Rej%")
                .Add("Stale%")
                .Add("HFan", GetType(Integer))
                .Add("Fans")
                .Add("HTemp", GetType(Integer))
                .Add("Temps")
                .Add("Freq", GetType(Integer))
                .Add("XCount")
                .Add("Status")
                .Add("ACount", GetType(Integer))
                .Add("IPAddress")
            End With
        End With

        Me.dataAnts.Columns("PoolData").Visible = False
        Me.dataAnts.Columns("PoolData2").Visible = False
        Me.dataAnts.Columns("IPAddress").Visible = False

        bSortingColumns = False

        With Me.dataAnts
            .Columns(0).Width = 59
            .Columns(1).Width = 66
            .Columns(2).Width = 87
            .Columns(3).Width = 96
            .Columns(4).Width = 66
            .Columns(5).Width = 85
            .Columns(6).Width = 99
            .Columns(7).Width = 59
            .Columns(8).Width = 65
            .Columns(9).Width = 65
            .Columns(10).Width = 56
            .Columns(11).Width = 169
            .Columns(12).Width = 70
            .Columns(13).Width = 239
            .Columns(14).Width = 50
            .Columns(15).Width = 73
            .Columns(16).Width = 260
        End With

        ctlsByKey = New ControlsByRegistry(csRegKey)

        Call SetGridSizes("\Columns\dataAnts", Me.dataAnts)
        Call SetGridColumnPositions("\Columns\" & Me.dataAnts.Name & "_DisplayIndex", Me.dataAnts)

        AddHandler Me.dataAnts.ColumnWidthChanged, AddressOf Me.dataGrid_ColumnWidthChanged
        AddHandler Me.dataAnts.ColumnDisplayIndexChanged, AddressOf Me.dataAnts_ColumnDisplayIndexChanged

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey)
            If key Is Nothing Then
                My.Computer.Registry.CurrentUser.CreateSubKey(csRegKey)
            End If
        End Using

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey)
            If key.GetValue("Width") > 100 Then
                Me.Width = key.GetValue("Width")
            End If

            If key.GetValue("Height") > 100 Then
                Me.Height = key.GetValue("Height")
            End If

            If key.GetValue("ToldUserAboutNotification") = "Y" Then
                ToldUserRunningInNotificationTray = True
            End If
        End Using

        With ctlsByKey
            'options
            .AddControl(Me.chklstAnts, "AntList")
            .AddControl(Me.txtRefreshRate, "RefreshRateValue")
            .AddControl(Me.cmbRefreshRate, "RefreshRateVolume")
            .AddControl(Me.chkShowBestShare, "ShowBestShare")
            .AddControl(Me.chkShowBlocks, "ShowBlocks")
            .AddControl(Me.chkShowFans, "ShowFans")
            .AddControl(Me.chkShowGHs5s, "ShowGHs5s")
            .AddControl(Me.chkShowGHsAvg, "ShowGHsAvg")
            .AddControl(Me.chkShowHWE, "ShowHWE")
            .AddControl(Me.chkShowPools, "ShowPools")
            .AddControl(Me.chkShowStatus, "ShowStatus")
            .AddControl(Me.chkShowTemps, "ShowTemps")
            .AddControl(Me.chkShowUptime, "ShowUptime")
            .AddControl(Me.chkShowFreqs, "ShowFreqs")
            .AddControl(Me.chkShowHighFan, "ShowHighFan")
            .AddControl(Me.chkShowHighTemp, "ShowHighTemp")
            .AddControl(Me.chkShowXCount, "ShowXCount")
            .AddControl(Me.chkShowRej, "ShowReject")
            .AddControl(Me.chkShowStale, "ShowStale")
            .AddControl(Me.chkShowDifficulty, "ShowDifficulty")
            .AddControl(Me.chkShowACount, "ShowACount")

            .AddControl(Me.chkShowSelectionColumn, "ShowSelectionColumn")

            .AddControl(Me.chkUseAPI, "UseAPI")

            'alerts
            .AddControl(Me.chkAlertIfS1Temp, "AlertIfS1Temp")
            .AddControl(Me.chkAlertIfS2Temp, "AlertIfS2Temp")
            .AddControl(Me.txtAlertS1Temp, "AlertValueS1Temp")
            .AddControl(Me.txtAlertS2Temp, "AlertValueS2Temp")

            .AddControl(Me.chkAlertIfS1FanHigh, "AlertIfS1Fan")
            .AddControl(Me.chkAlertIfS2FanHigh, "AlertIfS2Fan")
            .AddControl(Me.txtAlertS1FanHigh, "AlertValueS1Fan")
            .AddControl(Me.txtAlertS2FanHigh, "AlertValueS2Fan")

            .AddControl(Me.chkAlertIfS1FanLow, "AlertIfS1FanLow")
            .AddControl(Me.chkAlertIfS2FanLow, "AlertIfS2FanLow")
            .AddControl(Me.txtAlertS1FanLow, "AlertValueS1FanLow")
            .AddControl(Me.txtAlertS2FanLow, "AlertValueS2FanLow")

            .AddControl(Me.chkAlertIfS1Hash, "AlertIfS1Hash")
            .AddControl(Me.chkAlertIfS2Hash, "AlertIFS2Hash")
            .AddControl(Me.txtAlertS1Hash, "AlertValueS1Hash")
            .AddControl(Me.txtAlertS2Hash, "AlertValueS2Hash")

            .AddControl(Me.chkAlertIfS1XCount, "AlertIfS1XCount")
            .AddControl(Me.chkAlertIfS2XCount, "AlertIFS2XCount")
            .AddControl(Me.txtAlertS1XCount, "AlertValueS1XCount")
            .AddControl(Me.txtAlertS2XCount, "AlertValueS2XCount")

            .AddControl(Me.chkAlertHighlightField, "AlertHighlightField")
            .AddControl(Me.chkAlertShowAnnoyingPopup, "AlertShowAnnoyingPopup")
            .AddControl(Me.chkAlertShowNotifyPopup, "AlertShowNotifyPopup")
            .AddControl(Me.chkAlertStartProcess, "AlertStartProcess")
            .AddControl(Me.txtAlertStartProcessName, "AlertProcessName")
            .AddControl(Me.txtAlertStartProcessParms, "AlertProcessParms")
            .AddControl(Me.txtAlertEMailGovernor, "AlertEMailGovernorSize")
            .AddControl(Me.cmbAlertEMailGovernor, "AlertEMailGovernorValue")

            .AddControl(Me.chkAlertSendEMail, "AlertSendEMail")

            'email settings
            .AddControl(Me.txtSMTPServer, "SMTPServerName")
            .AddControl(Me.txtSMTPPort, "SMTPServerPort")
            .AddControl(Me.txtSMTPUserName, "SMTPUserName")
            .AddControl(Me.txtSMTPPassword, "SMTPUserPassword")
            .AddControl(Me.txtSMTPAlertName, "SMTPAlertName")
            .AddControl(Me.txtSMTPAlertAddress, "SMTPAlertAddress")
            .AddControl(Me.txtSMTPAlertSubject, "SMTPAlertSubject")
            .AddControl(Me.txtSMTPFromName, "SMTPFromName")
            .AddControl(Me.txtSMTPFromAddress, "SMTPFromAddress")
            .AddControl(Me.chkSMTPSSL, "SMTPUseSSL")

            .AddControl(Me.chkAlertRebootIfXd, "RebootAntIfXd")
            .AddControl(Me.txtAlertRebootGovernor, "AlertRebootGovernor")
            .AddControl(Me.cmbAlertRebootGovernor, "AlertRebootGovernorValue")

            'change unmarked S1s to S1s
            Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\" & Me.chklstAnts.Name)
                If key Is Nothing Then
                    My.Computer.Registry.CurrentUser.CreateSubKey(csRegKey & "\" & Me.chklstAnts.Name)
                End If
            End Using

            Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\" & Me.chklstAnts.Name, True)
                Dim sTemp, sValue As String

                For Each sTemp In key.GetValueNames
                    sValue = key.GetValue(sTemp)

                    If sValue.Substring(0, 2) <> "S1" AndAlso sValue.Substring(0, 2) <> "S2" Then
                        key.SetValue(sTemp, "S1: " & sTemp)
                    End If
                Next
            End Using

            .SetControlByRegKey(Me.chklstAnts)
            .SetControlByRegKey(Me.txtRefreshRate, "300")
            .SetControlByRegKey(Me.cmbRefreshRate, "Seconds")
            .SetControlByRegKey(Me.chkShowBestShare, True)
            .SetControlByRegKey(Me.chkShowBlocks, True)
            .SetControlByRegKey(Me.chkShowFans, True)
            .SetControlByRegKey(Me.chkShowGHs5s, True)
            .SetControlByRegKey(Me.chkShowGHsAvg, True)
            .SetControlByRegKey(Me.chkShowHWE, True)
            .SetControlByRegKey(Me.chkShowPools, True)
            .SetControlByRegKey(Me.chkShowStatus, True)
            .SetControlByRegKey(Me.chkShowTemps, True)
            .SetControlByRegKey(Me.chkShowUptime, True)
            .SetControlByRegKey(Me.chkShowFreqs, True)
            .SetControlByRegKey(Me.chkShowHighTemp, True)
            .SetControlByRegKey(Me.chkShowHighFan, True)
            .SetControlByRegKey(Me.chkShowXCount, True)
            .SetControlByRegKey(Me.chkShowRej, True)
            .SetControlByRegKey(Me.chkShowStale, True)
            .SetControlByRegKey(Me.chkShowDifficulty, True)
            .SetControlByRegKey(Me.chkShowACount, True)

            .SetControlByRegKey(Me.chkShowSelectionColumn)

            .SetControlByRegKey(Me.chkUseAPI, True)

            'alerts
            .SetControlByRegKey(Me.chkAlertIfS1Temp)
            .SetControlByRegKey(Me.chkAlertIfS2Temp)
            .SetControlByRegKey(Me.txtAlertS1Temp)
            .SetControlByRegKey(Me.txtAlertS2Temp)

            .SetControlByRegKey(Me.chkAlertIfS1FanHigh)
            .SetControlByRegKey(Me.chkAlertIfS2FanHigh)
            .SetControlByRegKey(Me.txtAlertS1FanHigh)
            .SetControlByRegKey(Me.txtAlertS2FanHigh)

            .SetControlByRegKey(Me.chkAlertIfS1FanLow)
            .SetControlByRegKey(Me.chkAlertIfS2FanLow)
            .SetControlByRegKey(Me.txtAlertS1FanLow)
            .SetControlByRegKey(Me.txtAlertS2FanLow)

            .SetControlByRegKey(Me.chkAlertIfS1Hash)
            .SetControlByRegKey(Me.chkAlertIfS2Hash)
            .SetControlByRegKey(Me.txtAlertS1Hash)
            .SetControlByRegKey(Me.txtAlertS2Hash)

            .SetControlByRegKey(Me.chkAlertIfS1XCount)
            .SetControlByRegKey(Me.chkAlertIfS2XCount)
            .SetControlByRegKey(Me.txtAlertS1XCount)
            .SetControlByRegKey(Me.txtAlertS2XCount)

            .SetControlByRegKey(Me.chkAlertHighlightField, True)
            .SetControlByRegKey(Me.chkAlertShowNotifyPopup, True)
            .SetControlByRegKey(Me.chkAlertShowAnnoyingPopup)
            .SetControlByRegKey(Me.chkAlertStartProcess)
            .SetControlByRegKey(Me.txtAlertStartProcessName)
            .SetControlByRegKey(Me.txtAlertStartProcessParms)
            .SetControlByRegKey(Me.chkAlertSendEMail)

            .SetControlByRegKey(Me.chkAlertRebootIfXd, True)
            .SetControlByRegKey(Me.txtAlertRebootGovernor, 30)
            .SetControlByRegKey(Me.cmbAlertRebootGovernor, "Minutes")

            'email settings
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPServer)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPPort)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPUserName)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPPassword)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPAlertName)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPAlertAddress)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPAlertSubject)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPFromName)
            Call ctlsByKey.SetControlByRegKey(Me.txtSMTPFromAddress)
            Call ctlsByKey.SetControlByRegKey(Me.chkSMTPSSL)

            Call ctlsByKey.SetControlByRegKey(Me.txtAlertEMailGovernor, "10")
            Call ctlsByKey.SetControlByRegKey(Me.cmbAlertEMailGovernor, "Minutes")

            'establish credentials for existing Ants when they aren't already there (upgrade code)
            For Each sAnt As String In Me.chklstAnts.Items
                Dim sDefaultUN, sDefaultPW As String

                sDefaultUN = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey, "Username", "root")
                sDefaultPW = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey, "Password", "root")

                If My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "WebUsername", "") Is Nothing Then
                    If sAnt.Substring(0, 2) = "S1" Then
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "WebUsername", sDefaultUN, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "WebPassword", sDefaultPW, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "SSHUsername", sDefaultUN, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "SSHPassword", sDefaultPW, Microsoft.Win32.RegistryValueKind.String)
                    End If

                    If sAnt.Substring(0, 2) = "S2" Then
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "WebUsername", sDefaultUN, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "WebPassword", sDefaultPW, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "SSHUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sAnt, "SSHPassword", "admin", Microsoft.Win32.RegistryValueKind.String)
                    End If
                End If
            Next

            colResponses = New System.Collections.Generic.List(Of String)
        End With

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\Pools")
            If key Is Nothing Then
                My.Computer.Registry.CurrentUser.CreateSubKey(csRegKey & "\Pools")
            End If
        End Using

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\Pools")
            For Each sKey As String In key.GetSubKeyNames
                Me.lstPools.AddItem(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & sKey, "Description", ""), sKey)
            Next
        End Using

        'check each of the boxes
        For x = 0 To Me.chklstAnts.Items.Count - 1
            Me.chklstAnts.SetItemChecked(x, True)
        Next

        Call CalcRefreshRate()

        wb(0) = New WebBrowser
        AddHandler wb(0).DocumentCompleted, AddressOf Me.wb_completed

        wb(1) = New WebBrowser
        AddHandler wb(1).DocumentCompleted, AddressOf Me.wb_completed

        wb(2) = New WebBrowser
        AddHandler wb(2).DocumentCompleted, AddressOf Me.wb_completed

        Call RefreshGrid()

    End Sub

    Private Sub wb_completed(sender As Object, e As System.Windows.Forms.WebBrowserDocumentCompletedEventArgs)

        Dim dr As DataRow
        Dim x, y, z As Integer
        Dim sAnt As String
        Dim bAntFound As Boolean
        Dim wb As WebBrowser
        Dim sbTemp As System.Text.StringBuilder
        Dim count(0 To 9) As Integer
        Dim sWebUN, sWebPW As String
        Dim sIP As String

        wb = sender

        'Select Case wb.Name
        '    Case "WebBrowser1"
        '        Me.lblBrowser1.Text = wb.Url.AbsoluteUri

        '    Case "WebBrowser2"
        '        Me.lblBrowser2.Text = wb.Url.AbsoluteUri

        '    Case "WebBrowser3"
        '        Me.lblBrowser3.Text = wb.Url.AbsoluteUri

        'End Select

        sbTemp = New System.Text.StringBuilder

        'first slash slash
        x = InStr(wb.Url.AbsoluteUri, "/")

        'second slash, should be after address
        x = InStr(x + 2, wb.Url.AbsoluteUri, "/")

        sIP = wb.Url.AbsoluteUri.Substring(7, x - 8)

        While y < x
            z = InStr(y + 1, wb.Url.AbsoluteUri.Substring(0, x), ".")

            If z = 0 Then
                If y = 0 Then
                    y = InStr(wb.Url.AbsoluteUri, "//") + 1
                End If

                Exit While
            Else
                y = z
            End If
        End While

        sAnt = wb.Url.AbsoluteUri.Substring(y, x - y - 1)

        'sAnt = Me.chklstAnts.CheckedItems(bAnt).ToString.Substring(0, 2) & ":" & wb.Url.AbsoluteUri.Substring(y, x - y - 1)

        If wb.Document.All(1).OuterHtml.ToLower.Contains("authorization") Then
            AddToLog(sAnt & " responded with login page")

            Call GetWebCredentials("S1: " & wb.Url.AbsoluteUri.Substring(7, InStr(8, wb.Url.AbsoluteUri, "/") - 8), sWebUN, sWebPW)

            wb.Document.All("username").SetAttribute("value", sWebUN)
            wb.Document.All("password").SetAttribute("value", sWebPW)
            wb.Document.All(48).InvokeMember("click")
        Else
#If DEBUG Then
            For x = 0 To wb.Document.All.Count - 1
                Debug.Print(x & " -- " & wb.Document.All(x).OuterText)
                Debug.Print(x & " -- " & wb.Document.All(x).OuterHtml)
            Next
#End If
            If wb.Url.AbsoluteUri.Contains("minerStatus.cgi") Then
                sAnt = "S2:" & sAnt

                For Each dr In ds.Tables(0).Rows
                    If dr.Item("Name") = sAnt Then
                        bAntFound = True

                        Exit For
                    End If
                Next

                If bAntFound = False Then
                    dr = ds.Tables(0).NewRow
                End If

                dr.Item("Name") = sAnt
                dr.Item("IPAddress") = "S2: " & sIP

                'S2 status code
                AddToLog(wb.Url.AbsoluteUri & " responded with status page")

                dr.Item("Uptime") = wb.Document.All(88).OuterText
                dr.Item("GH/s(5s)") = wb.Document.All(91).OuterText
                dr.Item("GH/s(avg)") = wb.Document.All(94).OuterText
                dr.Item("Blocks") = wb.Document.All(97).OuterText
                dr.Item("HWE%") = Format(UInt64.Parse(wb.Document.All(109).OuterText) / _
                        (UInt64.Parse(wb.Document.All(127).OuterText) + UInt64.Parse(wb.Document.All(130).OuterText) + UInt64.Parse(wb.Document.All(109).OuterText)), "##0.###%")
                dr.Item("BestShare") = Format(UInt64.Parse(wb.Document.All(137).OuterText), "###,###,###,###,###,##0")

                Select Case wb.Document.All(193).OuterText
                    Case "Alive"
                        sbTemp.Append("U")

                    Case "Dead"
                        sbTemp.Append("D")

                End Select

                If wb.Document.All.Count > 224 Then
                    Select Case wb.Document.All(245).OuterText
                        Case "Alive"
                            sbTemp.Append("U")

                        Case "Dead"
                            sbTemp.Append("D")

                    End Select

                    Select Case wb.Document.All(297).OuterText
                        Case "Alive"
                            sbTemp.Append("U")

                        Case "Dead"
                            sbTemp.Append("D")

                    End Select

                    dr.Item("Pools") = sbTemp.ToString

                    sbTemp.Clear()

                    dr.Item("HFan") = GetHighValue(wb.Document.All(530).OuterText, wb.Document.All(531).OuterText, wb.Document.All(532).OuterText, wb.Document.All(533).OuterText)

                    dr.Item("Fans") = wb.Document.All(530).OuterText & " " & wb.Document.All(531).OuterText & " " & wb.Document.All(532).OuterText & " " & wb.Document.All(533).OuterText

                    'dr.Item("Freq") = wb.Document.All(366).OuterText & " " & wb.Document.All(382).OuterText & " " & wb.Document.All(398).OuterText & " " & wb.Document.All(414).OuterText & " " & _
                    '                   wb.Document.All(430).OuterText & " " & wb.Document.All(446).OuterText & " " & wb.Document.All(462).OuterText & " " & wb.Document.All(478).OuterText & " " & _
                    '                   wb.Document.All(494).OuterText & " " & wb.Document.All(510).OuterText
                    dr.Item("Freq") = Val(wb.Document.All(366).OuterText)

                    dr.Item("HTemp") = GetHighValue(wb.Document.All(369).OuterText, wb.Document.All(385).OuterText, wb.Document.All(401).OuterText, wb.Document.All(417).OuterText, _
                                                    wb.Document.All(433).OuterText, wb.Document.All(449).OuterText, wb.Document.All(465).OuterText, wb.Document.All(481).OuterText, _
                                                    wb.Document.All(497).OuterText, wb.Document.All(513).OuterText)

                    dr.Item("Temps") = wb.Document.All(369).OuterText & " " & wb.Document.All(385).OuterText & " " & wb.Document.All(401).OuterText & " " & wb.Document.All(417).OuterText & " " & _
                                       wb.Document.All(433).OuterText & " " & wb.Document.All(449).OuterText & " " & wb.Document.All(465).OuterText & " " & wb.Document.All(481).OuterText & " " & _
                                       wb.Document.All(497).OuterText & " " & wb.Document.All(513).OuterText

                    count(0) = HowManyInString(wb.Document.All(372).OuterText, "x") + HowManyInString(wb.Document.All(372).OuterText, "-")
                    count(1) = HowManyInString(wb.Document.All(388).OuterText, "x") + HowManyInString(wb.Document.All(388).OuterText, "-")
                    count(2) = HowManyInString(wb.Document.All(404).OuterText, "x") + HowManyInString(wb.Document.All(404).OuterText, "-")
                    count(3) = HowManyInString(wb.Document.All(420).OuterText, "x") + HowManyInString(wb.Document.All(420).OuterText, "-")
                    count(4) = HowManyInString(wb.Document.All(436).OuterText, "x") + HowManyInString(wb.Document.All(436).OuterText, "-")
                    count(5) = HowManyInString(wb.Document.All(452).OuterText, "x") + HowManyInString(wb.Document.All(452).OuterText, "-")
                    count(6) = HowManyInString(wb.Document.All(468).OuterText, "x") + HowManyInString(wb.Document.All(468).OuterText, "-")
                    count(7) = HowManyInString(wb.Document.All(484).OuterText, "x") + HowManyInString(wb.Document.All(484).OuterText, "-")
                    count(8) = HowManyInString(wb.Document.All(500).OuterText, "x") + HowManyInString(wb.Document.All(500).OuterText, "-")
                    count(9) = HowManyInString(wb.Document.All(516).OuterText, "x") + HowManyInString(wb.Document.All(516).OuterText, "-")

                    dr.Item("XCount") = count(0) + count(1) + count(2) + count(3) + count(4) + count(5) + count(6) + count(7) + count(8) + count(9) & "X"

                    dr.Item("Status") = count(0) & "X " & count(1) & "X " & count(2) & "X " & count(3) & "X " & count(4) & "X " & count(5) & "X " & _
                                        count(6) & "X " & count(7) & "X " & count(8) & "X " & count(9) & "X"

                    If (count(0) <> 0 OrElse count(1) <> 0 OrElse count(2) <> 0 OrElse count(3) <> 0 OrElse count(4) <> 0 OrElse count(5) <> 0 _
                        OrElse count(6) <> 0 OrElse count(7) <> 0 OrElse count(8) <> 0 OrElse count(9) <> 0) AndAlso Me.chkAlertRebootIfXd.Checked = True Then
                        'only reboot once every x minutes
                        If TryGovernor(RebootInfo, sAnt, Me.cmbAlertRebootGovernor, Me.txtAlertRebootGovernor, 60 * 30) = True Then
                            AddToLog("REBOOTING " & dr.Item("Name"))

                            wb.Navigate("http://192.168.0." & dr.Item("Name") & "/reboot.html")
                        Else
                            AddToLog("Need to reboot " & dr.Item("Name") & " but it hasn't been long enough since last reboot")
                        End If
                    Else
                        If Me.TimerRefresh.Enabled = False Then
                            Call RefreshGrid()
                        End If
                    End If
                End If

            ElseIf wb.Url.AbsoluteUri.Contains("/reboot.html") = True Then
                'S2 reboot
                wb.Document.All(66).InvokeMember("click")

            ElseIf wb.Url.AbsoluteUri.Contains("/admin/status/minerstatus/") = True Then
                'S1 status code    
                AddToLog(wb.Url.AbsoluteUri & " responded with status page")

                sAnt = "S1:" & sAnt

                For Each dr In ds.Tables(0).Rows
                    If dr.Item("Name") = sAnt Then
                        bAntFound = True

                        Exit For
                    End If
                Next

                If bAntFound = False Then
                    dr = ds.Tables(0).NewRow
                End If

                dr.Item("Name") = sAnt
                dr.Item("IPAddress") = "S1: " & sIP

                If wb.Url.AbsoluteUri.Contains("minerstatus") AndAlso wb.Document.All.Count > 75 Then
                    dr.Item("Uptime") = wb.Document.All(122).OuterText.TrimEnd

                    If wb.Document.All(84).Children(2).Children.Count <> 1 Then
                        dr.Item("GH/s(5s)") = wb.Document.All(126).OuterText.TrimEnd
                        dr.Item("GH/s(avg)") = wb.Document.All(130).OuterText.TrimEnd
                        dr.Item("Blocks") = wb.Document.All(134).OuterText.TrimEnd
                        dr.Item("HWE%") = Format(UInt64.Parse(wb.Document.All(150).OuterText.TrimEnd.Replace(",", "")) / _
                                         (UInt64.Parse(wb.Document.All(174).OuterText.TrimEnd.Replace(",", "")) + _
                                          UInt64.Parse(wb.Document.All(178).OuterText.TrimEnd.Replace(",", "")) + _
                                          UInt64.Parse(wb.Document.All(150).OuterText.TrimEnd.Replace(",", ""))), "##0.###%")
                        dr.Item("BestShare") = wb.Document.All(186).OuterText.TrimEnd

                        Select Case wb.Document.All(247).OuterText.TrimEnd
                            Case "Alive"
                                sbTemp.Append("U")

                            Case "Dead"
                                sbTemp.Append("D")

                        End Select

                        'dr.Item("P0Status") = wb.Document.All(192).Children(2).Children(0).Children(0).Children(2).Children(3).Children(0).OuterText.TrimEnd

                        If wb.Document.All(192).Children(2).Children(0).Children(0).Children.Count > 3 Then
                            'dr.Item("P1Status") = wb.Document.All(192).Children(2).Children(0).Children(0).Children(3).Children(3).Children(0).OuterText.TrimEnd

                            Select Case wb.Document.All(192).Children(2).Children(0).Children(0).Children(3).Children(3).Children(0).OuterText.TrimEnd
                                Case "Alive"
                                    sbTemp.Append("U")

                                Case "Dead"
                                    sbTemp.Append("D")

                            End Select

                            If wb.Document.All(192).Children(2).Children(0).Children(0).Children.Count > 4 Then
                                'dr.Item("P2Status") = wb.Document.All(192).Children(2).Children(0).Children(0).Children(4).Children(3).Children(0).OuterText.TrimEnd

                                Select Case wb.Document.All(192).Children(2).Children(0).Children(0).Children(4).Children(3).Children(0).OuterText.TrimEnd
                                    Case "Alive"
                                        sbTemp.Append("U")

                                    Case "Dead"
                                        sbTemp.Append("D")

                                End Select

                                x = 443
                            Else
                                'dr.Item("P2Status") = "N/A"
                                sbTemp.Append("N")

                                x = 374
                            End If
                        Else
                            'dr.Item("P1Status") = "N/A"
                            sbTemp.Append("NN")

                            x = 305
                        End If
                        dr.Item("Pools") = sbTemp.ToString

                        sbTemp.Clear()

                        dr.Item("HFan") = GetHighValue(wb.Document.All(x + 33).OuterText.TrimEnd, wb.Document.All(x + 58).OuterText.TrimEnd)

                        dr.Item("Fans") = wb.Document.All(x + 33).OuterText.TrimEnd & " " & _
                                          wb.Document.All(x + 58).OuterText.TrimEnd

                        dr.Item("HTemp") = GetHighValue(wb.Document.All(x + 37).OuterText.TrimEnd, wb.Document.All(x + 62).OuterText.TrimEnd)

                        dr.Item("Temps") = wb.Document.All(x + 37).OuterText.TrimEnd & " " & _
                                           wb.Document.All(x + 62).OuterText.TrimEnd

                        'dr.Item("Freq") = wb.Document.All(x + 29).OuterText.TrimEnd & " " & wb.Document.All(x + 54).OuterText.TrimEnd
                        dr.Item("Freq") = Val(wb.Document.All(x + 29).OuterText.TrimEnd)

                        count(0) = HowManyInString(wb.Document.All(x + 41).OuterText.TrimEnd, "x")
                        count(1) = HowManyInString(wb.Document.All(x + 66).OuterText.TrimEnd, "x")

                        dr.Item("XCount") = count(0) + count(1) & "X"

                        dr.Item("Status") = count(0) & "X " & count(1) & "X"
                    End If

                    If (count(0) <> 0 OrElse count(1) <> 0) AndAlso Me.chkAlertRebootIfXd.Checked = True Then
                        If TryGovernor(RebootInfo, sAnt, Me.cmbAlertRebootGovernor, Me.txtAlertRebootGovernor, 60 * 30) = True Then
                            AddToLog("REBOOTING " & dr.Item("Name"))

                            wb.Navigate("http://192.168.0." & dr.Item("Name") & "/cgi-bin/luci/;stok=/admin/system/reboot?reboot=1")
                        Else
                            AddToLog("Need to reboot " & dr.Item("Name") & " but it hasn't been long enough since last reboot")
                        End If
                    Else
                        If Me.TimerRefresh.Enabled = False Then
                            Call RefreshGrid()
                        End If
                    End If
                End If
            End If

            If bAntFound = False Then
                ds.Tables(0).Rows.Add(dr)
            End If

            Me.dataAnts.Refresh()

            iBrowserSubmitted -= 1

            If iBrowserSubmitted = 0 Then
                Call HandleAlerts()
            End If
        End If

    End Sub

    Private Function GetHighValue(ByVal s1 As String, ByVal s2 As String, Optional ByVal s3 As String = "", Optional ByVal s4 As String = "", Optional ByVal s5 As String = "", _
                                  Optional ByVal s6 As String = "", Optional ByVal s7 As String = "", Optional ByVal s8 As String = "", Optional ByVal s9 As String = "", _
                                  Optional ByVal s10 As String = "") As Integer

        Dim h As Integer

        If Val(s1) > Val(s2) Then
            h = Val(s1)
        Else
            h = Val(s2)
        End If

        If s3.IsNullOrEmpty = False Then
            If Val(s3) > h Then
                h = Val(s3)
            End If

            If s4.IsNullOrEmpty = False Then
                If Val(s4) > h Then
                    h = Val(s4)
                End If

                If s5.IsNullOrEmpty = False Then
                    If Val(s5) > h Then
                        h = Val(s5)
                    End If

                    If s6.IsNullOrEmpty = False Then
                        If Val(s6) > h Then
                            h = Val(s6)
                        End If

                        If s7.IsNullOrEmpty = False Then
                            If Val(s7) > h Then
                                h = Val(s7)
                            End If

                            If s8.IsNullOrEmpty = False Then
                                If Val(s8) > h Then
                                    h = Val(s8)
                                End If

                                If s9.IsNullOrEmpty = False Then
                                    If Val(s9) > h Then
                                        h = Val(s9)
                                    End If

                                    If s10.IsNullOrEmpty = False Then
                                        If Val(s10) > h Then
                                            h = Val(s10)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End If

        Return h

    End Function


    Private Function HowManyInString(ByVal sString As String, sSearch As String) As Integer

        Dim i, x As Integer

        For x = 0 To sString.Length - 1
            If sString.Substring(x, 1).ToLower = sSearch.ToLower Then
                i += 1
            End If
        Next

        Return i

    End Function

    Private Sub TimerRefresh_Tick(sender As Object, e As System.EventArgs) Handles TimerRefresh.Tick

        Dim x As Integer

        iCountDown -= 1

        If iCountDown < 0 Then
            iCountDown = iRefreshRate
        End If

        For x = 0 To colResponses.Count - 1
            AddToLog(colResponses(x))

            colResponses.RemoveAt(x)
        Next

        If iCountDown = 0 Then
            Me.TimerRefresh.Enabled = False
            Me.cmdPause.Enabled = False

            iWatchDog = 300 '5 minutes

            If Me.chkUseAPI.Checked = False Then
                Me.TimerWatchdog.Enabled = True
            End If

            'clear the uptime column to indicate we're refreshing
            For Each dr As DataRow In Me.ds.Tables(0).Rows
                dr.Item("UpTime") = "???"
            Next

            Me.dataAnts.Refresh()

            AddToLog("Initiated refresh")

            Call RefreshGrid()

            iCountDown = iRefreshRate
        End If

        Me.cmdRefresh.Text = "Refreshing in " & iCountDown

    End Sub

    Private Sub cmdRefresh_Click(sender As System.Object, e As System.EventArgs) Handles cmdRefresh.Click

        iCountDown = 1

        Call TimerRefresh_Tick(sender, e)

    End Sub

    Private Function GetHeader() As String

        Return "Authorization: Basic " & Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(Me.txtWebUsername.Text & ":" & Me.txtWebPassword.Text)) & System.Environment.NewLine

    End Function

    Private Sub RefreshGrid()

        Dim sTemp, sAntIP, sAnt As String
        Dim bAntFound As Boolean
        Dim dr As DataRow
        Dim j, jp1 As Newtonsoft.Json.Linq.JObject
        Dim ja As Newtonsoft.Json.Linq.JArray
        Dim ts As TimeSpan
        Dim sbTemp, sbTemp2 As System.Text.StringBuilder
        Dim count(0 To 9), iTemp As Integer
        Dim sResult As String
        Dim dBestShare As Double
        Dim AntType As enAntType
        Dim x As Integer
        Dim bStep As Byte
        Dim sWebUN, sWebPW As String
        Dim dbTemp As Double
        Dim pd As clsPoolData
        Dim pdl As System.Collections.Generic.List(Of clsPoolData)

        If Me.chklstAnts.Items.Count = 0 Then
            MsgBox("Please add some Ant addresses first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

            Me.TabControl1.SelectTab(1)

            Exit Sub
        End If

        sbTemp = New System.Text.StringBuilder

        If Me.chkUseAPI.Checked = True Then
            While bAnt <> Me.chklstAnts.CheckedItems.Count
                Try
                    sbTemp.Clear()

                    Select Case Me.chklstAnts.CheckedItems(bAnt).ToString.Substring(0, 2)
                        Case "S2"
                            AntType = enAntType.S2

                        Case "S1"
                            AntType = enAntType.S1

                        Case Else
                            Throw New Exception("Unknown ant type.")

                    End Select

                    sAntIP = Me.chklstAnts.CheckedItems(bAnt).ToString.Substring(4)

                    sAnt = Me.chklstAnts.CheckedItems(bAnt).ToString.Substring(0, 2) & ":" & sAntIP.InstrRev(".")

                    For Each dr In ds.Tables(0).Rows
                        If dr.Item("Name") = sAnt Then
                            bAntFound = True

                            Exit For
                        End If
                    Next

                    If bAntFound = False Then
                        dr = ds.Tables(0).NewRow
                    End If

                    dr.Item("Name") = sAnt
                    dr.Item("IPAddress") = Me.chklstAnts.CheckedItems(bAnt).ToString

                    bStep = 1

                    sResult = GetIPData(sAntIP, "stats")

                    j = Newtonsoft.Json.Linq.JObject.Parse(sResult)

                    For Each ja In j.Property("STATS")
                        For Each jp1 In ja
                            ts = New TimeSpan(0, 0, jp1.Value(Of Integer)("Elapsed"))

                            dr.Item("Uptime") = Format(ts.Days, "0d") & " " & Format(ts.Hours, "0h") & " " & Format(ts.Minutes, "0m") & " " & Format(ts.Seconds, "0s")
                            dr.Item("HWE%") = jp1.Value(Of String)("Device Hardware%") & "%"

                            dr.Item("HFan") = GetHighValue(jp1.Value(Of Integer)("fan1"), jp1.Value(Of Integer)("fan2"), jp1.Value(Of Integer)("fan3"), jp1.Value(Of Integer)("fan4"))

                            sbTemp.Clear()

                            For x = 1 To jp1.Value(Of Integer)("fan_num")
                                sbTemp.Append(jp1.Value(Of Integer)("fan" & x))

                                If x <> jp1.Value(Of Integer)("fan_num") Then
                                    sbTemp.Append(" ")
                                End If
                            Next

                            dr.Item("Fans") = sbTemp.ToString

                            sbTemp.Clear()

                            iTemp = 0

                            For x = 1 To jp1.Value(Of Integer)("temp_num")
                                sbTemp.Append(jp1.Value(Of Integer)("temp" & x))

                                If jp1.Value(Of Integer)("temp" & x) > iTemp Then
                                    iTemp = jp1.Value(Of Integer)("temp" & x)
                                End If

                                If x <> jp1.Value(Of Integer)("temp_num") Then
                                    sbTemp.Append(" ")
                                End If
                            Next

                            dr.Item("HTemp") = iTemp

                            dr.Item("Temps") = sbTemp.ToString

                            dr.Item("Freq") = Val(jp1.Value(Of String)("frequency"))

                            count(0) = HowManyInString(jp1.Value(Of String)("chain_acs1"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs1"), "x")
                            count(1) = HowManyInString(jp1.Value(Of String)("chain_acs2"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs2"), "x")

                            If AntType = enAntType.S2 Then
                                count(2) = HowManyInString(jp1.Value(Of String)("chain_acs3"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs3"), "x")
                                count(3) = HowManyInString(jp1.Value(Of String)("chain_acs4"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs4"), "x")
                                count(4) = HowManyInString(jp1.Value(Of String)("chain_acs5"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs5"), "x")
                                count(5) = HowManyInString(jp1.Value(Of String)("chain_acs6"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs6"), "x")
                                count(6) = HowManyInString(jp1.Value(Of String)("chain_acs7"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs7"), "x")
                                count(7) = HowManyInString(jp1.Value(Of String)("chain_acs8"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs8"), "x")
                                count(8) = HowManyInString(jp1.Value(Of String)("chain_acs9"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs9"), "x")
                                count(9) = HowManyInString(jp1.Value(Of String)("chain_acs10"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs10"), "x")
                            Else
                                count(2) = 0
                                count(3) = 0
                                count(4) = 0
                                count(5) = 0
                                count(6) = 0
                                count(7) = 0
                                count(8) = 0
                                count(9) = 0
                            End If

                            dr.Item("XCount") = count(0) + count(1) + count(2) + count(3) + count(4) + count(5) + count(6) + count(7) + count(8) + count(9) & "X"

                            If AntType = enAntType.S2 Then
                                dr.Item("Status") = count(0) & "X " & count(1) & "X " & count(2) & "X " & count(3) & "X " & count(4) & "X " & count(5) & "X " & _
                                                    count(6) & "X " & count(7) & "X " & count(8) & "X " & count(9) & "X"
                            Else
                                dr.Item("Status") = count(0) & "X " & count(1) & "X "
                            End If

                            Exit For
                        Next

                        Exit For
                    Next

                    bStep = 2

                    sResult = GetIPData(sAntIP, "summary")

                    j = Newtonsoft.Json.Linq.JObject.Parse(sResult)

                    For Each ja In j.Property("SUMMARY")
                        For Each jp1 In ja
                            dr.Item("GH/s(5s)") = Val(jp1.Value(Of String)("GHS 5s"))
                            dr.Item("GH/s(avg)") = Val(jp1.Value(Of String)("GHS av"))

                            dr.Item("Rej%") = jp1.Value(Of String)("Pool Rejected%")
                            dr.Item("Stale%") = jp1.Value(Of String)("Pool Stale%")

                            dr.Item("Blocks") = jp1.Value(Of String)("Found Blocks")
                        Next
                    Next

                    bStep = 3

                    sResult = GetIPData(sAntIP, "pools")

                    j = Newtonsoft.Json.Linq.JObject.Parse(sResult)

                    dBestShare = 0

                    sbTemp.Clear()

                    sbTemp2 = New System.Text.StringBuilder

                    If IsDBNull(dr.Item("PoolData2")) = True Then
                        dr.Item("PoolData2") = New System.Collections.Generic.List(Of clsPoolData)
                    End If

                    pdl = dr.Item("PoolData2")
                    pd = New clsPoolData

                    For Each ja In j.Property("POOLS")
                        For Each jp1 In ja
                            If jp1.Value(Of Double)("Best Share") > dBestShare Then
                                dBestShare = jp1.Value(Of Double)("Best Share")
                            End If

                            Select Case jp1.Value(Of String)("Status")
                                Case "Alive"
                                    If sbTemp.ToString.Contains("U") = False Then
                                        dr.Item("Diff") = Format(jp1.Value(Of Double)("Last Share Difficulty"), "#,###,###")
                                    End If

                                    sbTemp.Append("U")

                                Case "Dead"
                                    sbTemp.Append("D")

                                Case Else
                                    sbTemp.Append("U")

                            End Select

                            If sbTemp2.Length <> 0 Then
                                sbTemp2.Append(vbCrLf)
                            End If

                            sbTemp2.Append(jp1.Value(Of String)("POOL") & ": " & jp1.Value(Of String)("URL") & " (" & jp1.Value(Of String)("User") & ") " & jp1.Value(Of String)("Status"))

                            pd.URL = jp1.Value(Of String)("URL")
                            pd.UID = jp1.Value(Of String)("User")

                            pdl.Add(pd)
                        Next

                        Exit For
                    Next

                    dr.Item("BestShare") = Format(dBestShare, "###,###,###,###,###,##0")
                    dr.Item("Pools") = sbTemp.ToString
                    dr.Item("PoolData") = sbTemp2.ToString
                Catch ex As Exception
                    dr.Item("Uptime") = "ERROR"
                    AddToLog("ERROR when querying " & sAnt & " (step " & bStep & "): " & ex.Message)
                End Try

                If bAntFound = False Then
                    ds.Tables(0).Rows.Add(dr)
                End If

                Me.dataAnts.Refresh()

                bAnt += 1
            End While

            Me.cmdPause.Enabled = True
            Me.TimerRefresh.Enabled = True
            Me.TimerWatchdog.Enabled = False
            bAnt = 0

            x = 0

            For Each dg As DataGridViewRow In Me.dataAnts.Rows
                If dg.Cells("Uptime").Value <> "ERROR" AndAlso dg.Cells("Uptime").Value <> "???" Then
                    x += 1
                End If

                dbTemp += dg.Cells("GH/s(avg)").Value
            Next

            Me.Text = csVersion & " - " & Now.ToString & " - " & x & " of " & Me.chklstAnts.CheckedItems.Count & " responded - " & FormatHashRate(dbTemp * 1000)

            Call HandleAlerts()
        Else
            'browser logic
            Call GetWebCredentials(Me.chklstAnts.CheckedItems(bAnt).ToString, sWebUN, sWebPW)

            If bAnt <> Me.chklstAnts.CheckedItems.Count Then
                sTemp = Me.chklstAnts.CheckedItems(bAnt).ToString

                If wb(0).IsBusy = False Then
                    AddToLog("Submitting " & Me.chklstAnts.CheckedItems(bAnt) & " on instance 0")

                    'sock.Connect("192.168.0.91", 4028)

                    If sTemp.Substring(0, 2) = "S2" Then
                        wb(0).Navigate(String.Format("http://{0}:{1}@" & sTemp.Substring(4) & "/cgi-bin/minerStatus.cgi", sWebUN, sWebPW), Nothing, Nothing, GetHeader)
                    Else
                        wb(0).Navigate("http://" & sTemp.Substring(4) & "/cgi-bin/luci/;stok=/admin/status/minerstatus/", False)
                    End If

                    bAnt += 1

                    iBrowserSubmitted += 1
                End If
            End If

            Call GetWebCredentials(Me.chklstAnts.CheckedItems(bAnt).ToString, sWebUN, sWebPW)

            If bAnt <> Me.chklstAnts.CheckedItems.Count Then
                sTemp = Me.chklstAnts.CheckedItems(bAnt).ToString

                If wb(1).IsBusy = False Then
                    AddToLog("Submitting " & Me.chklstAnts.CheckedItems(bAnt) & " on instance 1")

                    If sTemp.Substring(0, 2) = "S2" Then
                        wb(1).Navigate(String.Format("http://{0}:{1}@" & sTemp.Substring(4) & "/cgi-bin/minerStatus.cgi", sWebUN, sWebPW), Nothing, Nothing, GetHeader)
                    Else
                        wb(1).Navigate("http://" & sTemp.Substring(4) & "/cgi-bin/luci/;stok=/admin/status/minerstatus/", False)
                    End If

                    bAnt += 1

                    iBrowserSubmitted += 1
                End If
            End If

            Call GetWebCredentials(Me.chklstAnts.CheckedItems(bAnt).ToString, sWebUN, sWebPW)

            If bAnt <> Me.chklstAnts.CheckedItems.Count Then
                sTemp = Me.chklstAnts.CheckedItems(bAnt).ToString

                If wb(2).IsBusy = False Then
                    AddToLog("Submitting " & Me.chklstAnts.CheckedItems(bAnt) & " on instance 2")

                    If sTemp.Substring(0, 2) = "S2" Then
                        wb(2).Navigate(String.Format("http://{0}:{1}@" & sTemp.Substring(4) & "/cgi-bin/minerStatus.cgi", sWebUN, sWebPW), Nothing, Nothing, GetHeader)
                    Else
                        wb(2).Navigate("http://" & sTemp.Substring(4) & "/cgi-bin/luci/;stok=/admin/status/minerstatus/", False)
                    End If

                    bAnt += 1

                    iBrowserSubmitted += 1
                End If
            End If

            If bAnt = Me.chklstAnts.CheckedItems.Count Then
                Me.cmdPause.Enabled = True
                Me.TimerRefresh.Enabled = True
                Me.TimerWatchdog.Enabled = False
                bAnt = 0

                Me.Text = csVersion & " - Refreshed " & Now.ToString
            End If
        End If

    End Sub

    Private Sub GetWebCredentials(ByVal sAnt As String, ByRef sUsername As String, ByRef sPassword As String)

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\Ants\" & sAnt)
            sUsername = key.GetValue("WebUsername")
            sPassword = key.GetValue("WebPassword")
        End Using

    End Sub

    Private Sub GetSSHCredentials(ByVal sAnt As String, ByRef sUsername As String, ByRef sPassword As String)

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\Ants\" & sAnt)
            sUsername = key.GetValue("SSHUsername")
            sPassword = key.GetValue("SSHPassword")
        End Using

    End Sub

    Private Sub HandleAlerts()

        Dim x As Integer
        Dim dr As DataGridViewRow
        Dim iAlertCount, iAntAlertCount As Integer
        Dim bStep As Byte
        Dim colHighlightColumns As System.Collections.Generic.List(Of Integer)

        'alert logic
        For Each dr In Me.dataAnts.Rows
            Try
                If dr.Cells("Uptime").Value <> "ERROR" AndAlso dr.Cells("Uptime").Value <> "???" Then
                    iAntAlertCount = 0

                    If dr.Tag Is Nothing Then
                        dr.Tag = New System.Collections.Generic.List(Of Integer)
                    End If

                    colHighlightColumns = dr.Tag
                    colHighlightColumns.Clear()

                    Select Case dr.Cells("Name").Value.Substring(0, 2)
                        Case "S1"
                            If Me.chkAlertIfS1Temp.Checked = True Then
                                bStep = 1

                                x = Val(Me.txtAlertS1Temp.Text)

                                If x > 0 Then
                                    If Integer.Parse(dr.Cells("HTemp").Value) >= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("HTemp").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " exceeded " & x & " celcius", "S1 Temp Alert")
                                    End If
                                End If
                            End If

                            If Me.chkAlertIfS1FanHigh.Checked = True Then
                                bStep = 2

                                x = Val(Me.txtAlertS1FanHigh.Text)

                                If x > 0 Then
                                    If Integer.Parse(dr.Cells("HFan").Value) >= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("HFan").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " exceeded " & x & " RPM", "S1 Fan Alert")
                                    End If
                                End If
                            End If

                            If Me.chkAlertIfS1FanLow.Checked = True Then
                                bStep = 3

                                x = Val(Me.txtAlertS1FanLow.Text)

                                If Integer.Parse(dr.Cells("HFan").Value) <= x Then
                                    iAntAlertCount += 1

                                    colHighlightColumns.Add(dr.Cells("HFan").ColumnIndex)

                                    Call ProcessAlerts(dr, dr.Cells("Name").Value & " is below " & x & " RPM", "S1 Fan Alert")
                                End If
                            End If

                            If Me.chkAlertIfS1Hash.Checked = True Then
                                bStep = 4

                                x = Val(Me.txtAlertS1Hash.Text)

                                If x > 0 Then
                                    If Val(dr.Cells("GH/s(avg)").Value) <= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("GH/s(avg)").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " less than " & x & " GH/s", "S1 Hash Alert")
                                    End If
                                End If
                            End If

                            If Me.chkAlertIfS1XCount.Checked = True Then
                                bStep = 5

                                x = Val(Me.txtAlertS1XCount.Text)

                                If x > 0 Then
                                    If Integer.Parse(dr.Cells("XCount").Value.ToString.LeftMost(1)) >= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("XCount").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " exceeded " & x & " X count", "S1 XCount Alert")

                                        'use SSH only if using the API, as the web code has its own reboot logic
                                        If Me.chkAlertRebootIfXd.Checked = True AndAlso Me.chkUseAPI.Checked = True Then
                                            Call RebootAnt(dr.Cells("IPAddress").Value, False)
                                        End If
                                    End If
                                End If
                            End If

                        Case "S2"
                            If Me.chkAlertIfS2Temp.Checked = True Then
                                bStep = 6

                                x = Val(Me.txtAlertS2Temp.Text)

                                If x > 0 Then
                                    If Integer.Parse(dr.Cells("HTemp").Value) >= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("HTemp").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " exceeded " & x & " celcius", "S2 Temp Alert")
                                    End If
                                End If
                            End If

                            If Me.chkAlertIfS2FanHigh.Checked = True Then
                                bStep = 7

                                x = Val(Me.txtAlertS2FanHigh.Text)

                                If x > 0 Then
                                    If Integer.Parse(dr.Cells("HFan").Value) >= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("HFan").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " exceeded " & x & " RPM", "S2 Fan Alert")
                                    End If
                                End If
                            End If

                            If Me.chkAlertIfS2FanLow.Checked = True Then
                                bStep = 8

                                x = Val(Me.txtAlertS2FanLow.Text)

                                If Integer.Parse(dr.Cells("HFan").Value) <= x Then
                                    iAntAlertCount += 1

                                    colHighlightColumns.Add(dr.Cells("HFan").ColumnIndex)

                                    Call ProcessAlerts(dr, dr.Cells("Name").Value & " is below " & x & " RPM", "S2 Fan Alert")
                                End If

                            End If

                            If Me.chkAlertIfS2Hash.Checked = True Then
                                bStep = 9

                                x = Val(Me.txtAlertS2Hash.Text)

                                If x > 0 Then
                                    If Val(dr.Cells("GH/s(avg)").Value) <= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("GH/s(avg)").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " less than " & x & " GH/s", "S2 Hash Alert")
                                    End If
                                End If
                            End If

                            If Me.chkAlertIfS2XCount.Checked = True Then
                                bStep = 10

                                x = Val(Me.txtAlertS2XCount.Text)

                                If x > 0 Then
                                    If Integer.Parse(dr.Cells("XCount").Value.ToString.LeftMost(1)) >= x Then
                                        iAntAlertCount += 1

                                        colHighlightColumns.Add(dr.Cells("XCount").ColumnIndex)

                                        Call ProcessAlerts(dr, dr.Cells("Name").Value & " exceeded " & x & " X count", "S2 XCount Alert")

                                        'use SSH only if using the API, as the web code has its own reboot logic
                                        If Me.chkAlertRebootIfXd.Checked = True AndAlso Me.chkUseAPI.Checked = True Then
                                            Call RebootAnt(dr.Cells("IPAddress").Value, False)
                                        End If
                                    End If
                                End If
                            End If

                    End Select

                    dr.Cells("ACount").Value = iAntAlertCount
                End If
            Catch ex As Exception
                AddToLog("ERROR when checking alerts on " & dr.Cells("Name").Value & " (step " & bStep & "): " & ex.Message)
            End Try
        Next

        For Each dr In Me.dataAnts.Rows
            colHighlightColumns = dr.Tag

            dr.Cells("HTemp").Style.BackColor = New Color
            dr.Cells("HFan").Style.BackColor = New Color
            dr.Cells("GH/s(avg)").Style.BackColor = New Color
            dr.Cells("XCount").Style.BackColor = New Color

            For Each x In colHighlightColumns
                dr.Cells(x).Style.BackColor = Color.Red
            Next
        Next

        If iAlertCount <> 0 Then
            Me.Text = Me.Text & " !!! " & iAlertCount & " ALERTS !!!"
        End If

    End Sub

    ''' <summary>
    ''' Returns the seconds value of a drop down/text box combo for a governor rate
    ''' </summary>
    ''' <param name="dictList">The dictionary the data is in</param>
    ''' <param name="sKey">The key to search for in the dictionary</param>
    ''' <param name="cmbValueType">Seconds, Minutes, Hours, Days</param>
    ''' <param name="txtValue">Actual value, ie, 60</param>
    ''' <param name="iDefault">Which value to use if the end result is zero</param>
    ''' <returns>True if the action needs to be performed</returns>
    ''' <remarks>Will update the dictionary as necessary</remarks>
    Private Function TryGovernor(ByRef dictList As Dictionary(Of String, Date), ByVal sKey As String, ByVal cmbValueType As ComboBox, ByVal txtValue As TextBox, ByVal iDefault As Integer) As Boolean

        Dim iResult As Integer
        Dim dLastDate As Date

        If dictList.TryGetValue(sKey, dLastDate) = True Then
            Select Case cmbValueType.Text
                Case "Seconds"
                    iResult = Val(txtValue.Text)

                Case "Minutes"
                    iResult = Val(txtValue.Text) * 60

                Case "Hours"
                    iResult = Val(txtValue.Text) * 60 * 60

                Case "Days"
                    iResult = Val(txtValue.Text) * 60 * 60 * 24

            End Select

            If iResult = 0 Then
                iResult = iDefault
            End If

            If dLastDate.AddSeconds(iResult) < Now Then
                dictList(sKey) = Now

                Return True
            Else
                Return False
            End If
        Else
            dictList.Add(sKey, Now)

            Return True
        End If

    End Function

    Private Sub RebootAnt(ByVal sAnt As String, ByVal bRebootNow As Boolean)

        Dim t As Threading.Thread

        If TryGovernor(RebootInfo, sAnt, Me.cmbAlertRebootGovernor, Me.txtAlertRebootGovernor, 30 * 60) = True Then
            bRebootNow = True
        Else
            If bRebootNow = False Then
                AddToLog("Need to reboot " & sAnt & " but it hasn't been long enough since last reboot")
            End If
        End If

        If bRebootNow = True Then
            t = New Threading.Thread(AddressOf Me._RebootAnt)

            AddToLog("REBOOTING " & sAnt)

            t.Start(sAnt)
        End If

    End Sub

    Private Sub _RebootAnt(ByVal sAnt As String)

        Dim ssh As Renci.SshNet.SshClient
        Dim sshCommand As Renci.SshNet.SshCommand
        Dim sUN, sPW As String

        Try
            Call GetSSHCredentials(sAnt, sUN, sPW)

            ssh = New Renci.SshNet.SshClient(sAnt.Substring(4), sUN, sPW)
            ssh.Connect()

            sshCommand = ssh.CreateCommand("/sbin/reboot")
            sshCommand.Execute()

            If sshCommand.Error.IsNullOrEmpty = False Then
                colResponses.Add("Reboot of " & sAnt & " appears to have failed: " & sshCommand.Error)
            Else
                colResponses.Add("Reboot of " & sAnt & " appears to have succeeded")
            End If

            ssh.Disconnect()
            ssh.Dispose()

            sshCommand.Dispose()
        Catch ex As Exception
            colResponses.Add("Reboot of " & sAnt & " FAILED: " & ex.Message)
        End Try

    End Sub

    Private Sub ProcessAlerts(ByRef dr As DataGridViewRow, ByVal sAlertMsg As String, ByVal sAlertTitle As String)

        Dim ap As frmAnnoyingPopup
        Dim bStep As Byte

        Try
            bStep = 9

            'notify icon
            If Me.chkAlertShowNotifyPopup.Checked = True Then
                Me.NotifyIcon1.ShowBalloonTip(0, sAlertTitle, Now.ToString & vbCrLf & sAlertMsg, ToolTipIcon.Warning)
            End If

            'annoying popup
            If Me.chkAlertShowAnnoyingPopup.Checked = True Then
                ap = New frmAnnoyingPopup
                ap.Text = sAlertTitle
                ap.lblAlert.Text = Now.ToString & vbCrLf & sAlertMsg
                ap.Show()
            End If

            'launch process
            If Me.chkAlertStartProcess.Checked = True Then
                bStep = 10

                If Me.txtAlertStartProcessName.Text.IsNullOrEmpty = False Then
                    Try
                        If My.Computer.FileSystem.FileExists(Me.txtAlertStartProcessName.Text) = False Then
                            Me.NotifyIcon1.ShowBalloonTip(30000, "Error launching alert process!", "Error launching alert process!  The specified file to start does not seem to exist.", ToolTipIcon.Error)
                        Else
                            Process.Start(Me.txtAlertStartProcessName.Text, Replace(Me.txtAlertStartProcessParms.Text, "%A", dr.Cells("Name").Value))
                        End If
                    Catch ex As Exception
                        Me.NotifyIcon1.ShowBalloonTip(30000, "Error starting idle worker process!", "Error starting idle worker process!" & vbCrLf & vbCrLf & ex.Message, ToolTipIcon.Error)
                    End Try
                End If
            End If

            'email
            If Me.chkAlertSendEMail.Checked = True Then
                bStep = 11

                If TryGovernor(EMailAlertInfo, dr.Cells("IPAddress").Value, Me.cmbAlertEMailGovernor, Me.txtAlertEMailGovernor, 10 * 30) = True Then
                    If Me.txtSMTPAlertSubject.Text.IsNullOrEmpty = True Then
                        Call SendEMail(sAlertMsg, sAlertTitle)
                    Else
                        Call SendEMail(sAlertMsg, Me.txtSMTPAlertSubject.Text)
                    End If
                End If
            End If

            Call AddToLog("ALERT: " & sAlertMsg)
        Catch ex As Exception When bErrorHandle = True
            AddToLog("ERROR when processing alerts on " & dr.Cells("Name").Value & " (step " & bStep & "): " & ex.Message)
        End Try

    End Sub

    Private Function GetIPData(ByVal sIP As String, ByVal sCommand As String) As String

        Dim socket As System.Net.Sockets.TcpClient
        Dim s As System.IO.Stream
        Dim b() As Byte
        Dim sbTemp As System.Text.StringBuilder
        Dim d As Date

        Try
            socket = New System.Net.Sockets.TcpClient
            socket.Connect(sIP, "4028")
            s = socket.GetStream

            b = System.Text.Encoding.ASCII.GetBytes("{""command"":""" & sCommand & """}" & vbCrLf)

            s.Write(b, 0, b.Length)

            sbTemp = New System.Text.StringBuilder

            d = Now

            While (sbTemp.Length < 2 OrElse sbTemp.ToString.Substring(sbTemp.Length - 2, 1) <> "}") AndAlso d.AddMinutes(1) > Now
                My.Application.DoEvents()
                System.Threading.Thread.Sleep(100)

                If socket.Available <> 0 Then
                    Array.Resize(b, socket.Available)
                    s.Read(b, 0, b.Length)

                    sbTemp.Append(System.Text.Encoding.ASCII.GetString(b))
                End If
            End While

            s.Close()
            socket.Close()

            Return sbTemp.ToString
        Catch ex As Exception
            AddToLog("ERROR when accessing API on " & sIP & ": " & ex.Message)
        End Try

    End Function

    Private Sub dataGrid_ColumnWidthChanged(sender As Object, e As System.Windows.Forms.DataGridViewColumnEventArgs)

        Dim dt As DataGridView

        dt = DirectCast(sender, DataGridView)

        With My.Computer.Registry
            .CurrentUser.CreateSubKey(csRegKey & "\Columns\" & dt.Name)
            .SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Columns\" & dt.Name, e.Column.Name, e.Column.Width, Microsoft.Win32.RegistryValueKind.DWord)
        End With

    End Sub

    Private Sub SetGridSizes(ByVal sKey As String, ByRef dataGrid As DataGridView)

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & sKey)
            If key IsNot Nothing Then
                For Each colAny As DataGridViewColumn In dataGrid.Columns
                    If key.GetValue(colAny.Name) <> 0 Then
                        colAny.Width = key.GetValue(colAny.Name)
                    End If
                Next

                key.Close()
            End If
        End Using

    End Sub

    Private Sub SetGridColumnPositions(ByVal sKey As String, ByRef datagrid As DataGridView)

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & sKey)
            If key IsNot Nothing Then
                For Each colAny As DataGridViewColumn In datagrid.Columns
                    If key.GetValue(colAny.Name) <> 0 Then
                        colAny.DisplayIndex = key.GetValue(colAny.Name)
                    End If
                Next

                key.Close()
            End If
        End Using

    End Sub

    Private Sub frmMain_ResizeEnd(sender As Object, e As System.EventArgs) Handles Me.ResizeEnd

        With My.Computer.Registry
            .CurrentUser.CreateSubKey(csRegKey)
            .SetValue("HKEY_CURRENT_USER\" & csRegKey, "Width", Me.Width, Microsoft.Win32.RegistryValueKind.DWord)
            .SetValue("HKEY_CURRENT_USER\" & csRegKey, "Height", Me.Height, Microsoft.Win32.RegistryValueKind.DWord)
        End With

    End Sub

    Private Sub cmdScan_Click(sender As System.Object, e As System.EventArgs) Handles cmdScan.Click

        Dim sResponse, sLocalNet As String
        Dim x As Integer
        Dim wc As eWebClient

        Static bStopRequested As Boolean

        Try
            If Me.cmdScan.Text = "Scan" Then
                If Me.cmbLocalIPs.Text.IsNullOrEmpty = True Then
                    MsgBox("Please select your local IP address first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

                    Me.cmbLocalIPs.DroppedDown = True

                    Exit Sub
                End If

                If Me.txtWebUsername.Text.IsNullOrEmpty = True OrElse Me.txtWebPassword.Text.IsNullOrEmpty = True Then
                    MsgBox("Please enter your Ant credentials (web) first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

                    Me.txtWebUsername.Focus()

                    Exit Sub
                End If

                sLocalNet = Me.cmbLocalIPs.Text.Substring(0, Microsoft.VisualBasic.InStrRev(Me.cmbLocalIPs.Text, "."))

                wc = New eWebClient
                wc.Credentials = New System.Net.NetworkCredential(Me.txtWebUsername.Text, Me.txtWebPassword.Text)

                Me.cmdScan.Text = "STOP!"

                Me.ProgressBar1.Minimum = 1
                Me.ProgressBar1.Maximum = 255
                Me.ProgressBar1.Visible = True
                Me.lblScanning.Visible = True
                My.Application.DoEvents()

                For x = 1 To 255
                    If bStopRequested = True Then
                        bStopRequested = False

                        Me.cmdScan.Text = "Scan"

                        Exit For
                    End If

                    Me.ProgressBar1.Value = x
                    Me.ToolTip1.SetToolTip(Me.ProgressBar1, sLocalNet & x.ToString)

                    If sLocalNet & x.ToString <> Me.cmbLocalIPs.Text Then
                        Try
                            Debug.Print(x)

                            My.Application.DoEvents()

                            sResponse = wc.DownloadString("http://" & sLocalNet & x.ToString)

                            If sResponse.Contains("href=""/cgi-bin/luci"">LuCI - Lua Configuration Interface</a>") = True Then
                                wc.DownloadFile("http://" & sLocalNet & x.ToString & "/luci-static/resources/icons/antminer_logo.png", My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                My.Computer.FileSystem.DeleteFile(My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                Me.chklstAnts.SetItemChecked("S1: " & Me.chklstAnts.Items.Add(sLocalNet & x.ToString), True)

                                Call AddToLog("S1 found at " & sLocalNet & x.ToString & "!")

                                My.Application.DoEvents()
                            End If

                            If sResponse.Contains("<tr><td width=""33%"">Miner Type</td><td id=""ant_minertype""></td></tr>") Then
                                wc.DownloadFile("http://" & sLocalNet & x.ToString & "/images/antminer_logo.png", My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                My.Computer.FileSystem.DeleteFile(My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                Me.chklstAnts.SetItemChecked("S2: " & Me.chklstAnts.Items.Add(sLocalNet & x.ToString), True)

                                Call AddToLog("S2 found at " & sLocalNet & x.ToString & "!")

                                My.Application.DoEvents()
                            End If

                            Debug.Print(sResponse)
                        Catch ex As Exception
                        End Try
                    End If
                Next
            Else
                bStopRequested = True
            End If
        Catch ex As Exception When bErrorHandle = True
            MsgBox("The following error has occurred:" & vbCrLf & vbCrLf & ex.Message, MsgBoxStyle.Critical Or MsgBoxStyle.OkOnly)
        Finally
            Me.ToolTip1.SetToolTip(Me.ProgressBar1, "")
            Me.ProgressBar1.Visible = False
            Me.cmdScan.Enabled = True
            Me.lblScanning.Visible = False
        End Try

    End Sub

    Private Class eWebClient

        Inherits System.Net.WebClient

        Protected Overrides Function GetWebRequest(address As System.Uri) As System.Net.WebRequest
            Dim w As System.Net.WebRequest

            w = MyBase.GetWebRequest(address)
            w.Timeout = 5000

            Return w
        End Function

    End Class

    Private Sub cmdPause_Click(sender As System.Object, e As System.EventArgs) Handles cmdPause.Click

        Me.TimerRefresh.Enabled = Not Me.TimerRefresh.Enabled

        If Me.TimerRefresh.Enabled = True Then
            Me.cmdPause.Text = "Pause"
        Else
            Me.cmdPause.Text = "Resume"
        End If

    End Sub

    Private Sub cmdSaveConfig_Click(sender As System.Object, e As System.EventArgs) Handles cmdSaveConfig.Click

        With ctlsByKey
            .SetRegKeyByControl(Me.chklstAnts)

            .SetRegKeyByControl(Me.txtRefreshRate)
            .SetRegKeyByControl(Me.cmbRefreshRate)

            .SetRegKeyByControl(Me.chkShowBestShare)
            .SetRegKeyByControl(Me.chkShowBlocks)
            .SetRegKeyByControl(Me.chkShowFans)
            .SetRegKeyByControl(Me.chkShowGHs5s)
            .SetRegKeyByControl(Me.chkShowGHsAvg)
            .SetRegKeyByControl(Me.chkShowHWE)
            .SetRegKeyByControl(Me.chkShowPools)
            .SetRegKeyByControl(Me.chkShowStatus)
            .SetRegKeyByControl(Me.chkShowTemps)
            .SetRegKeyByControl(Me.chkShowUptime)
            .SetRegKeyByControl(Me.chkShowFreqs)
            .SetRegKeyByControl(Me.chkShowHighTemp)
            .SetRegKeyByControl(Me.chkShowHighFan)
            .SetRegKeyByControl(Me.chkShowXCount)
            .SetRegKeyByControl(Me.chkShowRej)
            .SetRegKeyByControl(Me.chkShowStale)
            .SetRegKeyByControl(Me.chkShowDifficulty)
            .SetRegKeyByControl(Me.chkShowACount)

            .SetRegKeyByControl(Me.chkShowSelectionColumn)

            .SetRegKeyByControl(Me.chkUseAPI)
        End With

    End Sub

    'will re-enable the normal countdown if it counts down to 0 
    'that should only happen if there are so many ants they can't be refreshed in 5 minutes
    'or something went wrong, like it's trying to reach an ant that is offline
    Private Sub TimerWatchdog_Tick(sender As Object, e As System.EventArgs) Handles TimerWatchdog.Tick

        iWatchDog -= 1

        If iWatchDog = 0 Then
            Me.TimerWatchdog.Enabled = False
            Me.TimerRefresh.Enabled = True
            Me.cmdPause.Enabled = True
        End If

    End Sub

    Private Sub cmdAddAnt_Click(sender As Object, e As System.EventArgs) Handles cmdAddAnt.Click

        Dim sTemp As String

        If Me.optAddS1.Checked = False AndAlso Me.optAddS2.Checked = False Then
            MsgBox("Please specify if this is an S1 or an S2.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

            Exit Sub
        End If

        If Me.optAddS1.Checked = True Then
            sTemp = "S1: "
        ElseIf Me.optAddS2.Checked = True Then
            sTemp = "S2: "
        End If

        If Me.txtAntAddress.Text.IsNullOrEmpty = False Then
            If Me.chklstAnts.Items.Contains(Me.txtAntAddress.Text) = False Then
                Me.chklstAnts.SetItemChecked(Me.chklstAnts.Items.Add(sTemp & Me.txtAntAddress.Text), True)
                Me.txtAntAddress.Text = ""

                If sTemp.Substring(0, 2) = "S1" Then
                    If Me.txtWebUsername.Text.IsNullOrEmpty = True Then
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebPassword", "root", Microsoft.Win32.RegistryValueKind.String)
                    Else
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebUsername", Me.txtWebUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebPassword", Me.txtWebPassword.Text, Microsoft.Win32.RegistryValueKind.String)
                    End If

                    If Me.txtSSHUsername.Text.IsNullOrEmpty = True Then
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHPassword", "root", Microsoft.Win32.RegistryValueKind.String)
                    Else
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHUsername", Me.txtSSHUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHPassword", Me.txtSSHPassword.Text, Microsoft.Win32.RegistryValueKind.String)
                    End If
                End If

                If sTemp.Substring(0, 2) = "S2" Then
                    If Me.txtWebUsername.Text.IsNullOrEmpty = True Then
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebPassword", "root", Microsoft.Win32.RegistryValueKind.String)
                    Else
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebUsername", Me.txtWebUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "WebPassword", Me.txtWebPassword.Text, Microsoft.Win32.RegistryValueKind.String)
                    End If

                    If Me.txtSSHUsername.Text.IsNullOrEmpty = True Then
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHPassword", "admin", Microsoft.Win32.RegistryValueKind.String)
                    Else
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHUsername", Me.txtSSHUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & Me.txtAntAddress.Text, "SSHPassword", Me.txtSSHPassword.Text, Microsoft.Win32.RegistryValueKind.String)
                    End If
                End If
            Else
                MsgBox("This address appears to already be in the list.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
            End If
        End If

    End Sub

    Private Sub cmdDelAnt_Click(sender As System.Object, e As System.EventArgs) Handles cmdDelAnt.Click

        If Me.chklstAnts.SelectedItem Is Nothing Then
            MsgBox("Please select an item to remove first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

            Exit Sub
        End If

        Me.chklstAnts.Items.RemoveAt(Me.chklstAnts.SelectedIndex)

    End Sub

    Private Sub cmbRefreshRate_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs) Handles cmbRefreshRate.KeyPress, cmbAlertEMailGovernor.KeyPress

        e.Handled = True

    End Sub

    Private Sub txtRefreshRate_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtRefreshRate.KeyPress, txtAlertEMailGovernor.KeyPress

        Select Case e.KeyChar
            Case "0" To "9", vbBack
                'okay

            Case Else
                e.Handled = True

        End Select

    End Sub

    Private Sub CalcRefreshRate()

        Select Case Me.cmbRefreshRate.Text
            Case "Seconds"
                iRefreshRate = Val(Me.txtRefreshRate.Text)

            Case "Minutes"
                iRefreshRate = Val(Me.txtRefreshRate.Text) * 60

            Case "Hours"
                iRefreshRate = Val(Me.txtRefreshRate.Text) * 60 * 60

        End Select

        If iRefreshRate = 0 Then
            iRefreshRate = 300
        End If

    End Sub

    Private Sub txtRefreshRate_LostFocus(sender As Object, e As System.EventArgs) Handles txtRefreshRate.LostFocus

        Call CalcRefreshRate()

    End Sub

    Private Sub cmbRefreshRate_LostFocus(sender As Object, e As System.EventArgs) Handles cmbRefreshRate.LostFocus

        Call CalcRefreshRate()

    End Sub

    Private Sub AddToLog(ByVal sText As String)

        Me.txtLog.AppendText(Now.ToLocalTime & ": " & sText & vbCrLf)

    End Sub

    Private Sub chkShow_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkShowBestShare.CheckedChanged, chkShowBlocks.CheckedChanged, _
        chkShowFans.CheckedChanged, chkShowGHs5s.CheckedChanged, chkShowGHsAvg.CheckedChanged, chkShowHWE.CheckedChanged, chkShowPools.CheckedChanged, _
        chkShowStatus.CheckedChanged, chkShowTemps.CheckedChanged, chkShowUptime.CheckedChanged, chkShowFreqs.CheckedChanged, chkShowHighFan.CheckedChanged, _
        chkShowHighTemp.CheckedChanged, chkShowXCount.CheckedChanged, chkShowRej.CheckedChanged, chkShowStale.CheckedChanged, chkShowDifficulty.CheckedChanged, _
        chkShowACount.CheckedChanged

        Dim chkAny As CheckBox

        If bStarted = False Then Exit Sub

        chkAny = DirectCast(sender, CheckBox)

        Select Case chkAny.Name
            Case "chkShowUptime"
                Me.dataAnts.Columns("Uptime").Visible = chkAny.Checked

            Case "chkShowGHs5s"
                Me.dataAnts.Columns("GH/s(5s)").Visible = chkAny.Checked

            Case "chkShowGHsAvg"
                Me.dataAnts.Columns("GH/s(avg)").Visible = chkAny.Checked

            Case "chkShowBlocks"
                Me.dataAnts.Columns("Blocks").Visible = chkAny.Checked

            Case "chkShowHWE"
                Me.dataAnts.Columns("HWE%").Visible = chkAny.Checked

            Case "chkShowBestShare"
                Me.dataAnts.Columns("BestShare").Visible = chkAny.Checked

            Case "chkShowPools"
                Me.dataAnts.Columns("Pools").Visible = chkAny.Checked

            Case "chkShowFans"
                Me.dataAnts.Columns("Fans").Visible = chkAny.Checked

            Case "chkShowTemps"
                Me.dataAnts.Columns("Temps").Visible = chkAny.Checked

            Case "chkShowStatus"
                Me.dataAnts.Columns("Status").Visible = chkAny.Checked

            Case "chkShowFreqs"
                Me.dataAnts.Columns("Freq").Visible = chkAny.Checked

            Case "chkShowHighFan"
                Me.dataAnts.Columns("HFan").Visible = chkAny.Checked

            Case "chkShowHighTemp"
                Me.dataAnts.Columns("HTemp").Visible = chkAny.Checked

            Case "chkShowXCount"
                Me.dataAnts.Columns("XCount").Visible = chkAny.Checked

            Case "chkShowRej"
                Me.dataAnts.Columns("Rej%").Visible = chkAny.Checked

            Case "chkShowStale"
                Me.dataAnts.Columns("Stale%").Visible = chkAny.Checked

            Case "chkShowDifficulty"
                Me.dataAnts.Columns("Diff").Visible = chkAny.Checked

            Case "chkShowACount"
                Me.dataAnts.Columns("ACount").Visible = chkAny.Checked

            Case Else
                MsgBox(chkAny.Name & " not found!", MsgBoxStyle.Critical Or MsgBoxStyle.OkOnly)

        End Select

        Me.dataAnts.Refresh()

    End Sub

    Private Sub optAddS1_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles optAddS1.CheckedChanged, optAddS2.CheckedChanged

        Dim opt As RadioButton

        opt = sender

        If opt.Checked = True Then
            If opt.Name = "optAddS1" Then
                optAddS2.Checked = False
            Else
                optAddS1.Checked = False
            End If
        End If

    End Sub

    'Private Sub chkUseAPI_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkUseAPI.CheckedChanged

    '    If chkUseAPI.Checked = True Then
    '        Me.chkWBRebootIfXd.Visible = False
    '        Me.lblWBPassword.Visible = False
    '        Me.lblWBUserName.Visible = False
    '        Me.txtWBPassword.Visible = False
    '        Me.txtWBUserName.Visible = False
    '    Else
    '        Me.chkWBRebootIfXd.Visible = True
    '        Me.lblWBPassword.Visible = True
    '        Me.lblWBUserName.Visible = True
    '        Me.txtWBPassword.Visible = True
    '        Me.txtWBUserName.Visible = True
    '    End If

    'End Sub

    Private Sub NotifyIcon1_DoubleClick(sender As Object, e As System.EventArgs) Handles NotifyIcon1.DoubleClick

        Me.Show()
        Me.Focus()

    End Sub

    Private Sub frmAntMonitor_FormClosed(sender As Object, e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

        Me.NotifyIcon1.Visible = False
        My.Application.DoEvents()

    End Sub

    Private Sub frmMain_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        Me.Hide()
        e.Cancel = True

        If ToldUserRunningInNotificationTray = False Then
            Me.NotifyIcon1.ShowBalloonTip(30000, "Still running!", "Still running in notification tray!  If you want to close me, right click me and click Exit.", ToolTipIcon.Info)
            ToldUserRunningInNotificationTray = True

            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey, "ToldUserAboutNotification", "Y", Microsoft.Win32.RegistryValueKind.String)
        End If

    End Sub

    Private Sub txtAlertS1Temp_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtAlertS1Temp.KeyPress, txtAlertS2Temp.KeyPress

        Select Case e.KeyChar
            Case "0" To "9", vbBack
                'good

            Case Else
                e.Handled = True

        End Select

    End Sub

    Private Sub mnuShow_Click(sender As Object, e As System.EventArgs) Handles mnuShow.Click

        Me.Show()
        Me.Focus()

    End Sub

    Private Sub cmdSaveAlerts_Click(sender As System.Object, e As System.EventArgs) Handles cmdSaveAlerts1.Click, cmdSaveAlerts2.Click, cmdSaveAlerts3.Click, cmdSaveAlerts4.Click

        With ctlsByKey
            .SetRegKeyByControl(Me.chkAlertHighlightField)
            .SetRegKeyByControl(Me.chkAlertShowAnnoyingPopup)
            .SetRegKeyByControl(Me.chkAlertShowNotifyPopup)

            If Me.chkAlertStartProcess.Checked = True Then
                If Me.txtAlertStartProcessName.Text.IsNullOrEmpty = True Then
                    MsgBox("Please specify a file to launch.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertStartProcess.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertStartProcess)
            .SetRegKeyByControl(Me.txtAlertStartProcessName)
            .SetRegKeyByControl(Me.txtAlertStartProcessParms)

            If Me.chkAlertIfS1Temp.Checked = True Then
                If Me.txtAlertS1Temp.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S1 temp alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS1Temp.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS1Temp)
            .SetRegKeyByControl(Me.txtAlertS1Temp)

            If Me.chkAlertIfS2Temp.Checked = True Then
                If Me.txtAlertS2Temp.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S2 temp alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS2Temp.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS2Temp)
            .SetRegKeyByControl(Me.txtAlertS2Temp)

            If Me.chkAlertIfS1FanHigh.Checked = True Then
                If Me.txtAlertS1FanHigh.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S1 high fan alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS1FanHigh.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS1FanHigh)
            .SetRegKeyByControl(Me.txtAlertS1FanHigh)

            If Me.chkAlertIfS2FanHigh.Checked = True Then
                If Me.txtAlertS2FanHigh.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S2 high fan alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS2FanHigh.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS2FanHigh)
            .SetRegKeyByControl(Me.txtAlertS2FanHigh)

            If Me.chkAlertIfS1FanLow.Checked = True Then
                If Me.txtAlertS1FanLow.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S1 low fan alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS1FanLow.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS1FanLow)
            .SetRegKeyByControl(Me.txtAlertS1FanLow)

            If Me.chkAlertIfS2FanLow.Checked = True Then
                If Me.txtAlertS2FanLow.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S2 low fan alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS2FanLow.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS2FanLow)
            .SetRegKeyByControl(Me.txtAlertS2FanLow)

            If Me.chkAlertIfS1Hash.Checked = True Then
                If Me.txtAlertS1Hash.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S1 hash alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS1Hash.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS1Hash)
            .SetRegKeyByControl(Me.txtAlertS1Hash)

            If Me.chkAlertIfS2Hash.Checked = True Then
                If Me.txtAlertS2Hash.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S2 hash alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS2Hash.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS2Hash)
            .SetRegKeyByControl(Me.txtAlertS2Hash)

            If Me.chkAlertIfS1XCount.Checked = True Then
                If Me.txtAlertS1XCount.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S1 XCount alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS1XCount.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS1XCount)
            .SetRegKeyByControl(Me.txtAlertS1XCount)

            If Me.chkAlertIfS2XCount.Checked = True Then
                If Me.txtAlertS2XCount.Text.IsNullOrEmpty Then
                    MsgBox("Please specify an S2 XCount alert value.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertIfS2XCount.Checked = False
                End If
            End If

            .SetRegKeyByControl(Me.chkAlertIfS2XCount)
            .SetRegKeyByControl(Me.txtAlertS2XCount)

            'email notifications
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPServer)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPPort)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPUserName)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPPassword)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPAlertName)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPAlertAddress)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPAlertSubject)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPFromAddress)
            Call ctlsByKey.SetRegKeyByControl(Me.txtSMTPFromName)
            Call ctlsByKey.SetRegKeyByControl(Me.chkSMTPSSL)

            Call ctlsByKey.SetRegKeyByControl(Me.txtAlertEMailGovernor)
            Call ctlsByKey.SetRegKeyByControl(Me.cmbAlertEMailGovernor)

            If Me.chkAlertSendEMail.Checked = True Then
                If String.IsNullOrEmpty(Me.txtSMTPServer.Text) = True OrElse String.IsNullOrEmpty(Me.txtSMTPPort.Text) = True OrElse String.IsNullOrEmpty(Me.txtSMTPUserName.Text) = True _
                    OrElse String.IsNullOrEmpty(Me.txtSMTPPassword.Text) = True OrElse String.IsNullOrEmpty(Me.txtSMTPAlertAddress.Text) = True OrElse _
                    String.IsNullOrEmpty(Me.txtSMTPFromAddress.Text) = True Then

                    Me.chkAlertSendEMail.Checked = False

                    MsgBox("EMail alerts are enabled, but one of more required SMTP fields are not specified.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")
                End If

                If Val(Me.txtAlertEMailGovernor.Text) = 0 Then
                    MsgBox("EMail alerts are enabled, but the EMail governor field appears to be zero.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertSendEMail.Checked = False
                End If
            End If

            Call ctlsByKey.SetRegKeyByControl(Me.chkAlertSendEMail)

            If Me.chkAlertRebootIfXd.Checked = True Then
                If Val(Me.txtAlertRebootGovernor.Text) = 0 Then
                    MsgBox("Reboot if XCount is enabled, but the reboot governor field appears to be zero.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

                    Me.chkAlertRebootIfXd.Checked = False
                End If
            End If

            Call .SetRegKeyByControl(Me.chkAlertRebootIfXd)
            Call .SetRegKeyByControl(Me.txtAlertRebootGovernor)
            Call .SetRegKeyByControl(Me.cmbAlertRebootGovernor)
        End With

    End Sub

    Private Sub mnuMainExit_Click(sender As System.Object, e As System.EventArgs) Handles mnuMainExit.Click, mnuExit.Click

        Me.NotifyIcon1.Visible = False
        My.Application.DoEvents()

        End

    End Sub

    Private Sub cmdAlertStartFileFinder_Click(sender As System.Object, e As System.EventArgs) Handles cmdAlertProcessFileFinder.Click

        Dim dlg As OpenFileDialog

        dlg = New OpenFileDialog

        dlg.InitialDirectory = "c:\"
        dlg.ShowDialog()

        If String.IsNullOrEmpty(dlg.FileName) = True Then Exit Sub

        Me.txtAlertStartProcessName.Text = dlg.FileName

    End Sub

    Private Sub cmdSendTestEMail_Click(sender As System.Object, e As System.EventArgs) Handles cmdSendTestEMail.Click

        If String.IsNullOrEmpty(Me.txtSMTPAlertSubject.Text) = True Then
            Call SendEMail("Ant TEST alert!", "Ant TEST alert!")
        Else
            Call SendEMail("Ant TEST alert!", Me.txtSMTPAlertSubject.Text)
        End If

    End Sub

    Private Sub SendEMail(ByVal sBody As String, ByVal sSubject As String)

        Dim SMTP As System.Net.Mail.SmtpClient
        Dim MSGfrom, MSGto As System.Net.Mail.MailAddress
        Dim MSG As System.Net.Mail.MailMessage

        SMTP = New System.Net.Mail.SmtpClient(Me.txtSMTPServer.Text, Me.txtSMTPPort.Text)

        SMTP.UseDefaultCredentials = False
        SMTP.Credentials = New System.Net.NetworkCredential(Me.txtSMTPUserName.Text, Me.txtSMTPPassword.Text)
        SMTP.EnableSsl = Me.chkSMTPSSL.Checked
        SMTP.DeliveryMethod = Net.Mail.SmtpDeliveryMethod.Network

        If String.IsNullOrEmpty(Me.txtSMTPAlertName.Text) = True Then
            MSGto = New System.Net.Mail.MailAddress(Me.txtSMTPAlertAddress.Text, Me.txtSMTPAlertAddress.Text, System.Text.Encoding.UTF8)
        Else
            MSGto = New System.Net.Mail.MailAddress(Me.txtSMTPAlertAddress.Text, Me.txtSMTPAlertName.Text, System.Text.Encoding.UTF8)
        End If

        If String.IsNullOrEmpty(Me.txtSMTPFromName.Text) = True Then
            MSGfrom = New System.Net.Mail.MailAddress(Me.txtSMTPFromAddress.Text, Me.txtSMTPFromAddress.Text, System.Text.Encoding.UTF8)
        Else
            MSGfrom = New System.Net.Mail.MailAddress(Me.txtSMTPFromAddress.Text, Me.txtSMTPFromName.Text, System.Text.Encoding.UTF8)
        End If

        MSG = New System.Net.Mail.MailMessage(MSGfrom, MSGto)
        MSG.Body = sBody
        MSG.BodyEncoding = System.Text.Encoding.UTF8

        MSG.Subject = sSubject
        MSG.SubjectEncoding = System.Text.Encoding.UTF8

        AddHandler SMTP.SendCompleted, AddressOf HandleEMailResponse

        SMTP.SendAsync(MSG, SMTP)

    End Sub

    Private Sub HandleEMailResponse(ByVal Sender As Object, e As System.ComponentModel.AsyncCompletedEventArgs)

        If e.Error Is Nothing Then
            Me.NotifyIcon1.ShowBalloonTip(5000, "Alert email sent", "Alert email sent", ToolTipIcon.Info)
            AddToLog("Alert email sent.")
        Else
            Me.NotifyIcon1.ShowBalloonTip(5000, "Alert email failed", "Alert email failed", ToolTipIcon.Warning)
            AddToLog("Alert email failed!")
        End If

        With DirectCast(e.UserState, System.Net.Mail.SmtpClient)
            RemoveHandler .SendCompleted, AddressOf HandleEMailResponse
            .Dispose()
        End With

    End Sub

    'if already running, forces the other one to come to the foreground
    Public Sub HandlesAlreadyRunning(sender As Object, e As Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs) Handles Me.StartupNextInstance

        e.BringToForeground = True

    End Sub

    Private Sub dataAnts_ColumnDisplayIndexChanged(sender As Object, e As System.Windows.Forms.DataGridViewColumnEventArgs)

        With My.Computer.Registry
            .CurrentUser.CreateSubKey(csRegKey & "\Columns\" & Me.dataAnts.Name & "_DisplayIndex")
            .SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Columns\" & Me.dataAnts.Name & "_DisplayIndex", e.Column.Name, e.Column.DisplayIndex, Microsoft.Win32.RegistryValueKind.DWord)
        End With

    End Sub

    Private Sub dataAnts_CellContextMenuStripNeeded(sender As Object, e As System.Windows.Forms.DataGridViewCellContextMenuStripNeededEventArgs) Handles dataAnts.CellContextMenuStripNeeded

        Dim colAnts As System.Collections.Generic.List(Of String)
        Dim x As Integer

        '0 - reboot one
        '1 - reboot multiple
        '2 - shutdown s2
        '3 - update pools
        mnuAntMenu.Items(0).Text = "Reboot " & Me.dataAnts.Rows(e.RowIndex).Cells("IPAddress").Value
        mnuAntMenu.Items(0).Tag = Me.dataAnts.Rows(e.RowIndex).Cells("IPAddress").Value

        'reboot multiple
        If Me.dataAnts.SelectedRows.Count = 0 Then
            mnuAntMenu.Items(1).Visible = False
        Else
            mnuAntMenu.Items(1).Visible = True

            mnuAntMenu.Items(1).Tag = New System.Collections.Generic.List(Of String)
            colAnts = mnuAntMenu.Items(1).Tag

            x = 0

            For Each dr As DataGridViewRow In Me.dataAnts.SelectedRows
                colAnts.Add(dr.Cells("IPAddress").Value)

                x += 1
            Next

            If x > 1 Then
                mnuAntMenu.Items(1).Text = "Reboot Multiple (" & x & " Ants)"
            Else
                mnuAntMenu.Items(1).Text = "Reboot Multiple (" & x & " Ant)"
            End If
        End If

        'shutdown s2
        If Me.dataAnts.Rows(e.RowIndex).Cells("IPAddress").Value.ToString.Substring(0, 2) = "S2" Then
            mnuAntMenu.Items(2).Visible = True
            mnuAntMenu.Items(2).Text = "Shutdown " & Me.dataAnts.Rows(e.RowIndex).Cells("IPAddress").Value
            mnuAntMenu.Items(2).Tag = Me.dataAnts.Rows(e.RowIndex).Cells("IPAddress").Value
        End If

        'update pools
        If Me.lblPools1.Tag IsNot Nothing AndAlso Me.lblPools2.Tag IsNot Nothing AndAlso Me.lblPools3.Tag IsNot Nothing Then
            mnuAntMenu.Items(3).Tag = New System.Collections.Generic.List(Of String)
            colAnts = mnuAntMenu.Items(3).Tag

            x = 0

            If Me.dataAnts.SelectedRows.Count = 0 Then
                colAnts.Add(Me.dataAnts.Rows(e.RowIndex).Cells("IPAddress").Value)

                x = 1
            Else
                For Each dr As DataGridViewRow In Me.dataAnts.SelectedRows
                    colAnts.Add(dr.Cells("IPAddress").Value)

                    x += 1
                Next
            End If

            If x > 1 Then
                mnuAntMenu.Items(3).Text = "Update Pools (" & x & " Ants)"
            Else
                mnuAntMenu.Items(3).Text = "Update Pools (" & x & " Ant)"
            End If

            mnuAntMenu.Items(3).Visible = True
        End If

        e.ContextMenuStrip = mnuAntMenu

    End Sub

    Private Sub dataAnts_CellToolTipTextNeeded(sender As Object, e As System.Windows.Forms.DataGridViewCellToolTipTextNeededEventArgs) Handles dataAnts.CellToolTipTextNeeded

        If e.ColumnIndex = Me.dataAnts.Columns("Pools").Index AndAlso e.RowIndex <> -1 Then
            e.ToolTipText = Me.dataAnts.Rows(e.RowIndex).Cells("PoolData").Value
        End If

    End Sub

    Private Sub chkShowSelectionColumn_Click(sender As Object, e As System.EventArgs) Handles chkShowSelectionColumn.Click

        Me.dataAnts.RowHeadersVisible = Me.chkShowSelectionColumn.Checked

    End Sub

    Private Sub chklstAnts_SelectedValueChanged(sender As Object, e As System.EventArgs) Handles chklstAnts.SelectedValueChanged

        If Me.chklstAnts.SelectedItems.Count <> 1 Then
            Me.cmdSaveAnt.Enabled = False
        Else
            Me.cmdSaveAnt.Enabled = True

            Call GetWebCredentials(Me.chklstAnts.SelectedItem.ToString, Me.txtWebUsername.Text, Me.txtWebPassword.Text)
            Call GetSSHCredentials(Me.chklstAnts.SelectedItem.ToString, Me.txtSSHUsername.Text, Me.txtSSHPassword.Text)
        End If

    End Sub

    Private Sub cmdSaveAnt_Click(sender As Object, e As System.EventArgs) Handles cmdSaveAnt.Click

        Dim sTemp As String

        sTemp = Me.chklstAnts.SelectedItem

        If sTemp.Substring(0, 2) = "S1" Then
            If Me.txtWebUsername.Text.IsNullOrEmpty = True Then
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebPassword", "root", Microsoft.Win32.RegistryValueKind.String)
            Else
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebUsername", Me.txtWebUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebPassword", Me.txtWebPassword.Text, Microsoft.Win32.RegistryValueKind.String)
            End If

            If Me.txtSSHUsername.Text.IsNullOrEmpty = True Then
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHPassword", "root", Microsoft.Win32.RegistryValueKind.String)
            Else
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHUsername", Me.txtSSHUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHPassword", Me.txtSSHPassword.Text, Microsoft.Win32.RegistryValueKind.String)
            End If
        End If

        If sTemp.Substring(0, 2) = "S2" Then
            If Me.txtWebUsername.Text.IsNullOrEmpty = True Then
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebPassword", "root", Microsoft.Win32.RegistryValueKind.String)
            Else
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebUsername", Me.txtWebUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "WebPassword", Me.txtWebPassword.Text, Microsoft.Win32.RegistryValueKind.String)
            End If

            If Me.txtSSHUsername.Text.IsNullOrEmpty = True Then
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHUsername", "root", Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHPassword", "admin", Microsoft.Win32.RegistryValueKind.String)
            Else
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHUsername", Me.txtSSHUsername.Text, Microsoft.Win32.RegistryValueKind.String)
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Ants\" & sTemp, "SSHPassword", Me.txtSSHPassword.Text, Microsoft.Win32.RegistryValueKind.String)
            End If
        End If

    End Sub

    Private Sub mnuRebootAnt_Click(sender As Object, e As System.EventArgs) Handles mnuRebootAnt.Click

        Dim t As ToolStripMenuItem

        t = sender

        Call AddToLog("Reboot of " & t.Tag.ToString & " requested")
        Call RebootAnt(t.Tag.ToString, True)

    End Sub

    Private Function FormatHashRate(ByVal dHashRate As Double) As String

        Dim sTemp As String

        Select Case dHashRate
            Case 0
                sTemp = "ZERO"

            Case Is < 0.001
                sTemp = Format(dHashRate * 1000000, "###.##") & " H/s"

            Case Is < 1
                sTemp = Format(dHashRate * 1000, "###.##") & " KH/s"

            Case Is < 1000
                sTemp = Format(dHashRate, "###.##") & " MH/s"

            Case Is < 1000000
                sTemp = Format(dHashRate / 1000, "###.##") & " GH/s"

            Case Is < 1000000000
                sTemp = Format(dHashRate / 1000000, "###.##") & " TH/s"

            Case Is < 1000000000000
                sTemp = Format(dHashRate / 1000000000, "###.##") & " PH/s"

            Case Is < 1000000000000000
                sTemp = Format(dHashRate / 1000000000000, "###.##") & " EH/s"

            Case Is < 1000000000000000000
                sTemp = Format(dHashRate / 1000000000000000, "###.##") & " ZH/s"

            Case Else
                sTemp = "UNKNOWN (BFH?)"

        End Select

        'Debug.Print(dHashRate & ": " & sTemp)

        Return sTemp

    End Function

    Private Sub mnuRebootMultiple_Click(sender As Object, e As System.EventArgs) Handles mnuRebootMultiple.Click

        Dim t As ToolStripMenuItem
        Dim c As System.Collections.Generic.List(Of String)

        t = sender
        c = t.Tag

        For Each sAnt As String In c
            Call AddToLog("Reboot of " & sAnt & " requested")
            Call RebootAnt(sAnt, True)
        Next

    End Sub

    Private Sub mnuShutdownS2_Click(sender As Object, e As System.EventArgs) Handles mnuShutdownS2.Click

        Dim th As Threading.Thread
        Dim t As ToolStripMenuItem
        Dim sAnt As String

        t = sender
        sAnt = t.Tag

        th = New Threading.Thread(AddressOf Me._RebootAnt)

        AddToLog("SHUTTING DOWN " & sAnt)

        th.Start(sAnt)

    End Sub

    Private Sub _ShutdownAnt(ByVal sAnt As String)

        Dim ssh As Renci.SshNet.SshClient
        Dim sshCommand As Renci.SshNet.SshCommand
        Dim sUN, sPW As String

        Try
            Call GetSSHCredentials(sAnt, sUN, sPW)

            ssh = New Renci.SshNet.SshClient(sAnt.Substring(4), sUN, sPW)
            ssh.Connect()

            sshCommand = ssh.CreateCommand("/sbin/shutdown -h -P now")
            sshCommand.Execute()

            If sshCommand.Error.IsNullOrEmpty = False Then
                colResponses.Add("Shutdown of " & sAnt & " appears to have failed: " & sshCommand.Error)
            Else
                colResponses.Add("Shutdown of " & sAnt & " appears to have succeeded")
            End If

            ssh.Disconnect()
            ssh.Dispose()

            sshCommand.Dispose()

        Catch ex As Exception
            colResponses.Add("Shutdown of " & sAnt & " FAILED: " & ex.Message)
        End Try

    End Sub

    Private Function ValidatePool() As Boolean

        If Me.txtPoolDesc.Text.IsNullOrEmpty Then
            MsgBox("Please enter a pool description first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

            Me.txtPoolDesc.Focus()

            Return False
        End If

        If Me.txtPoolURL.Text.IsNullOrEmpty Then
            MsgBox("Please enter a pool URL first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

            Me.txtPoolURL.Focus()

            Return False
        End If

        If Me.txtPoolUsername.Text.IsNullOrEmpty Then
            MsgBox("Please enter a pool username first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Oops!")

            Me.txtPoolUsername.Focus()

            Return False
        End If

        Return True

    End Function

    Private Sub cmdPoolAdd_Click(sender As System.Object, e As System.EventArgs) Handles cmdPoolAdd.Click

        Dim c As Integer

        If ValidatePool() = False Then Exit Sub

        c = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools", "Count", 0) + 1

        Me.lstPools.AddItem(Me.txtPoolDesc.Text, c)

        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & c, "Description", Me.txtPoolDesc.Text, Microsoft.Win32.RegistryValueKind.String)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & c, "URL", Me.txtPoolURL.Text, Microsoft.Win32.RegistryValueKind.String)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & c, "Username", Me.txtPoolUsername.Text, Microsoft.Win32.RegistryValueKind.String)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & c, "Password", Me.txtPoolPassword.Text, Microsoft.Win32.RegistryValueKind.String)

        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools", "Count", c, Microsoft.Win32.RegistryValueKind.String)

        Me.txtPoolDesc.Text = ""
        Me.txtPoolPassword.Text = ""
        Me.txtPoolURL.Text = ""
        Me.txtPoolUsername.Text = ""

    End Sub

    Private Sub lstPools_Click(sender As Object, e As System.EventArgs) Handles lstPools.Click

        Dim i As Integer

        i = Me.lstPools.ItemTag(Me.lstPools.SelectedIndex)

        Me.txtPoolDesc.Text = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "Description", "")
        Me.txtPoolURL.Text = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "URL", "")
        Me.txtPoolUsername.Text = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "Username", "")
        Me.txtPoolPassword.Text = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "Password", "")

    End Sub

    Private Sub cmdPoolChange_Click(sender As System.Object, e As System.EventArgs) Handles cmdPoolChange.Click

        Dim i As Integer

        If ValidatePool() = False Then Exit Sub

        i = Me.lstPools.ItemTag(Me.lstPools.SelectedIndex)

        Me.lstPools.Items(Me.lstPools.SelectedIndex) = Me.txtPoolDesc.Text

        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "Description", Me.txtPoolDesc.Text, Microsoft.Win32.RegistryValueKind.String)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "URL", Me.txtPoolURL.Text, Microsoft.Win32.RegistryValueKind.String)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "Username", Me.txtPoolUsername.Text, Microsoft.Win32.RegistryValueKind.String)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "Password", Me.txtPoolPassword.Text, Microsoft.Win32.RegistryValueKind.String)

    End Sub

    Private Sub cmdPoolDelete_Click(sender As Object, e As System.EventArgs) Handles cmdPoolDelete.Click

        Dim i As Integer

        If MsgBox("Are you sure you want to delete the selected pool: " & vbCrLf & vbCrLf & Me.lstPools.SelectedItem, MsgBoxStyle.Question Or MsgBoxStyle.YesNo) <> MsgBoxResult.Yes Then Exit Sub

        i = Me.lstPools.ItemTag(Me.lstPools.SelectedIndex)

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\Pools", True)
            key.DeleteSubKey(i, False)
        End Using

        Me.lstPools.RemoveSelectedItem()

        Me.txtPoolDesc.Text = ""
        Me.txtPoolPassword.Text = ""
        Me.txtPoolURL.Text = ""
        Me.txtPoolUsername.Text = ""

    End Sub

    Private Sub cmdPoolsImportFromAnts_Click(sender As System.Object, e As System.EventArgs) Handles cmdPoolsImportFromAnts.Click

        Dim pd, pd2 As clsPoolData
        Dim pdl, pdl2 As System.Collections.Generic.List(Of clsPoolData)
        Dim i, c, x As Integer
        Dim bCheckAgain, bFound As Boolean

        c = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools", "Count", 0) + 1

        pdl = New System.Collections.Generic.List(Of clsPoolData)

        'create a list of all the poolds we know about already
        For x = 0 To Me.lstPools.Items.Count - 1
            pd = New clsPoolData

            i = Me.lstPools.ItemTag(x)

            pd.UID = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "Username", "")
            pd.URL = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & i, "URL", "")

            pdl.Add(pd)
        Next

        'go through each ant
        For Each dr As DataGridViewRow In Me.dataAnts.Rows
            pdl2 = dr.Cells("PoolData2").Value

            'go through each pool for each ant
            For x = 0 To pdl2.Count - 1
                pd = pdl2(x)

                Do
                    bCheckAgain = False
                    bFound = False

                    For Each pd2 In pdl 'see if pool is in list we know about
                        If pd2.URL = pd.URL AndAlso pd2.UID = pd.UID Then
                            bFound = True

                            Exit For
                        End If
                    Next

                    If bFound = False Then
                        'add pool
                        pdl.Add(pd)

                        Me.lstPools.AddItem(dr.Cells("IPAddress").Value & " #" & x + 1, c)

                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & c, "Description", dr.Cells("IPAddress").Value & " #" & x + 1, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & c, "URL", pd.URL, Microsoft.Win32.RegistryValueKind.String)
                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools\" & c, "Username", pd.UID, Microsoft.Win32.RegistryValueKind.String)

                        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Pools", "Count", c, Microsoft.Win32.RegistryValueKind.String)

                        c += 1

                        bCheckAgain = True
                    End If
                Loop While bCheckAgain = True   'if the list is changed, the enumeration can not continue, so we start over again
            Next
        Next

    End Sub

    Private Sub cmdPoolMake1_Click(sender As System.Object, e As System.EventArgs) Handles cmdPoolMake1.Click, cmdPoolMake2.Click, cmdPoolMake3.Click

        Dim pd As clsPoolData
        Dim lblPool As Label

        If ValidatePool() = False Then Exit Sub

        Select Case DirectCast(sender, Button).Text
            Case "Make Pool #1"
                lblPool = Me.lblPools1

            Case "Make Pool #2"
                lblPool = Me.lblPools2

            Case "Make Pool #3"
                lblPool = Me.lblPools3

        End Select

        lblPool.Text = Me.txtPoolDesc.Text

        If lblPool.Tag Is Nothing Then
            lblPool.Tag = New clsPoolData
        End If

        pd = lblPool.Tag

        pd.URL = Me.txtPoolURL.Text
        pd.UID = Me.txtPoolUsername.Text
        pd.PW = Me.txtPoolPassword.Text

    End Sub

    Private Sub cmdPoolClear2_Click(sender As System.Object, e As System.EventArgs) Handles cmdPoolClear2.Click, cmdPoolClear3.Click

        Dim pd As clsPoolData
        Dim lblPool As Label

        Select Case DirectCast(sender, Button).Text
            Case "Clear Pool #2"
                lblPool = Me.lblPools2

            Case "Clear Pool #3"
                lblPool = Me.lblPools3

        End Select

        lblPool.Text = "<Blank>"

        If lblPool.Tag Is Nothing Then
            lblPool.Tag = New clsPoolData
        End If

        pd = lblPool.Tag

        pd.URL = ""
        pd.UID = ""
        pd.PW = ""

    End Sub

    Private Sub mnuUpdatePools_Click(sender As Object, e As System.EventArgs) Handles mnuUpdatePools.Click

        Dim th As Threading.Thread
        Dim t As ToolStripMenuItem
        Dim c As System.Collections.Generic.List(Of String)

        t = sender
        c = t.Tag

        For Each sAnt As String In c
            th = New Threading.Thread(AddressOf _UpdatePools)

            Call AddToLog("Update of pool info on " & sAnt & " requested")
            th.Start(sAnt)
        Next

    End Sub

    Private Sub _UpdatePools(ByVal sAnt As String)

        Dim ssh As Renci.SshNet.SshClient
        Dim sshCommand As Renci.SshNet.SshCommand
        Dim sUN, sPW As String
        Dim pd1, pd2, pd3 As clsPoolData

        Try
            pd1 = Me.lblPools1.Tag
            pd2 = Me.lblPools2.Tag
            pd3 = Me.lblPools3.Tag

            If pd1.PW = "" Then pd1.PW = "abc"
            If pd2.PW = "" Then pd2.PW = "abc"
            If pd3.PW = "" Then pd3.PW = "abc"

            Call GetSSHCredentials(sAnt, sUN, sPW)

            ssh = New Renci.SshNet.SshClient(sAnt.Substring(4), sUN, sPW)
            ssh.Connect()

            sshCommand = ssh.CreateCommand("/usr/bin/cgminer-api ""removepool|1""")
            sshCommand.Execute()
            sshCommand.Execute()

            sshCommand = ssh.CreateCommand("/usr/bin/cgminer-api ""addpool|" & pd1.URL.Replace("\", "\\") & "," & pd1.UID.Replace("\", "\\").Replace(",", "\,") & "," & pd1.PW.Replace("\", "\\").Replace(",", "\,") & """")
            sshCommand.Execute()

            sshCommand = ssh.CreateCommand("/usr/bin/cgminer-api ""switchpool|1""")
            sshCommand.Execute()

            sshCommand = ssh.CreateCommand("/usr/bin/cgminer-api ""removepool|0""")
            sshCommand.Execute()

            sshCommand = ssh.CreateCommand("/usr/bin/cgminer-api ""addpool|" & pd2.URL.Replace("\", "\\") & "," & pd2.UID.Replace("\", "\\").Replace(",", "\,") & "," & pd2.PW.Replace("\", "\\").Replace(",", "\,") & """")
            sshCommand.Execute()

            sshCommand = ssh.CreateCommand("/usr/bin/cgminer-api ""addpool|" & pd3.URL.Replace("\", "\\") & "," & pd3.UID.Replace("\", "\\").Replace(",", "\,") & "," & pd3.PW.Replace("\", "\\").Replace(",", "\,") & """")
            sshCommand.Execute()

            sshCommand = ssh.CreateCommand("/usr/bin/cgminer-api save")
            sshCommand.Execute()

            colResponses.Add("Update of pool info on  " & sAnt & " appears to have succeeded")

            ssh.Disconnect()
            ssh.Dispose()

            sshCommand.Dispose()
        Catch ex As Exception
            colResponses.Add("Update of pool info on " & sAnt & " FAILED: " & ex.Message)
        End Try

    End Sub
End Class
