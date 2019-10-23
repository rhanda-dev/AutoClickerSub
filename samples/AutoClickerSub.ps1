<#
	GetWindowHandle(target_window_title)
	return(hWnd)

	SetArgs(IntPtr targetwindowhandle, isRealSize, UseColor, magnification, magnification, threshold)
	return(args) args using for some methods.(CheckImage,TemplateMatch...)

	GetTemplateFileList(target_directory_name)
	return(template_file_list)

	MouseEvent(IntPtr hWnd, MOUSEEVENT, x, y, int wheel, fwKeys)
	MOUSEEVENT : [MOUSEEVENT]::CLICK,DBLCLICK,WHEEL,SWIPE_UP,SWIPE_DOWN,SWIPE_LEFT,SWIPE_RIGHT
	fwKeys : [fwKeys]::MK_LBUTTON, MK_RBUTTON, MK_WHEEL

	CheckImage(args, image_file_name)
	return (bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT)

	TemplateMatch(args, template_file_list)
	return(bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT, matched_image, matched_file_name, matched_index)

	GetWindowRectangle(hwnd)
	return(RECT rect)

	SetWindowPos(hWnd, rect)
#>
$rui = $host.UI.RawUI
$rui.WindowSize = New-Object System.Management.Automation.Host.Size(50, 20)

Param([parameter(mandatory)][IntPtr]$hwnd, [parameter(mandatory)][string]$targetdir)

$dllpath = "C:\tools\MyTools\AutoClickerSub"

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase, System.Windows.Forms, ReachFramework
Add-Type -Path "$dllpath\AutoClickerSub.dll"
Add-Type -Path "$dllpath\OpenCvSharp.Extensions.dll"
$MessageBox = "System.Windows.Forms.MessageBox" -as [type]

$AC = "AutoClickerSub" -as [type]
$BitmapConverter = "OpenCvSharp.Extensions.WriteableBitmapConverter" -as [type]

$xamlfile = "$PSScriptRoot\AutoClickerSub.xaml"
[xml]$xaml = [System.IO.File]::ReadAllText($xamlfile, [System.Text.Encoding]::UTF8)
$form = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $xaml))
$xaml.SelectNodes("//*") | ? { $_.Attributes["x:Name"] -ne $null } | % {
    New-Variable  -Name $_.Name -Value $form.FindName($_.Name) -Force
}
$txtWidth.Text = "0.5"
$txtHeight.Text = "0.5"
$chkRealSize.IsChecked = $false
$chkUseHDC.IsChecked = $true
$chkColor.IsChecked = $false
$txtThreshold.Text = "0.99"
$chkClick.IsChecked = $true
$chkAskClick.IsChecked = $true
$tbTemplatelist.Text = $targetdir
$params = $AC::SetArgs($hwnd, $chkRealSize.IsChecked, $chkUseHDC.IsChecked, $chkColor.IsChecked, $txtWidth.Text, $txtHeight.Text, $txtThreshold.Text)
$targetimage = $AC::GetCapture($params)
$templatefiles = $AC::GetTemplateFileList($tbTemplatelist.Text);
$dgFileList.ItemsSource = $templatefiles
$txtAppWidth.Text = $targetimage.Item2.X
$txtAppHeight.Text = $targetimage.Item2.Y

$txtMagnification.Text = [string]($targetimage.Item2.X * [double]$txtWidth.Text) + " x " + [string]($targetimage.Item2.Y * [double]$txtHeight.Text)
$imgCapture.Width = $targetimage.Item2.X * [double]$txtWidth.Text
$imgCapture.Height = $targetimage.Item2.Y * [double]$txtHeight.Text
$imgCapture.Source = $BitmapConverter::ToWriteableBitmap($targetimage.Item1)

$btnParam.add_Click( {
        $params = $AC::SetArgs($hwnd, $chkRealSize.IsChecked, $chkUseHDC.IsChecked, $chkColor.IsChecked, $txtWidth.Text, $txtHeight.Text, $txtThreshold.Text)
        $targetimage = $AC::GetCapture($params)
        $templatefiles = $AC::GetTemplateFileList($tbTemplatelist.Text);
        $dgFileList.ItemsSource = $templatefiles
        $txtAppWidth.Text = $targetimage.Item2.X
        $txtAppHeight.Text = $targetimage.Item2.Y
        $txtMagnification.Text = [string]($targetimage.Item2.X * [double]$txtWidth.Text) + " x " + [string]($targetimage.Item2.Y * [double]$txtHeight.Text)
        $imgCapture.Width = $targetimage.Item2.X * [double]$txtWidth.Text
        $imgCapture.Height = $targetimage.Item2.Y * [double]$txtHeight.Text
        $imgCapture.Source = $BitmapConverter::ToWriteableBitmap($targetimage.Item1)
    })
$btnClose.add_Click( {
        $form.Close()
    })
$btnSetDirectory.add_Click( {
        $dialog = New-Object System.Windows.Forms.OpenFileDialog
        $dialog.Filter = "全てのファイル(*.*)|*.*"
        $dialog.InitialDirectory = $targetdir
        $dialog.Title = "フォルダを選択してください"
        if ($dialog.ShowDialog() -eq "OK") {
            $targetdir = (Get-Item $dialog.FileName).DirectoryName
            $templatefiles = $AC::GetTemplateFileList($targetdir)
            $tbTemplatelist.Text = $targetdir
            $dgFileList.ItemsSource = $null
            $dgFileList.ItemsSource = $templatefiles
        }
    })
$btnCheck.add_Click( {
        $targetimage = $AC::GetCapture($params)
        $imgCapture.Source = $BitmapConverter::ToWriteableBitmap($targetimage.Item1)
        $ret = $AC::TemplateMatch($params, $templatefiles)
        if ($ret.Item1) {
            if ($chkClick.IsChecked) {
                if ($chkAskClick.IsChecked) {
                    $YesNo = $MessageBox::Show("Found : " + $ret.Item5, "Found", "YesNo")
                    if ($YesNo -eq "Yes") {
                        $AC::MouseEvent($hwnd, [MOUSEEVENT]::CLICK, $ret.item3.X, $ret.Item3.Y, 0, [fwKeys]::MK_LBUTTON )
                    }
                }
                else {
                    $AC::MouseEvent($hwnd, [MOUSEEVENT]::CLICK, $ret.item3.X, $ret.Item3.Y, 0, [fwKeys]::MK_LBUTTON )
                }
            }
            else {
                $YesNo = $MessageBox::Show("Found : " + $ret.Item5, "Found", "OK")
            }
        }
    })
$btnAUTO.add_Click( {
        $loop = $true
        Write-Host "Auto Check Start"
        $imgCapture.Source = $null
        $tbCapture.Text = "Auto Execution..."
        $AutoJob = Start-Job -Name "AutoClick" {
            param (
                [string]$dllpath,
                [string]$targetapp,
                [string]$targetdir,
                [bool]$chkRealSize,
                [bool]$chkUseHDC,
                [bool]$chkColor,
                [double]$toX,
                [double]$toY,
                [string]$txtThreshold
            )
            $ErrorActionPreference = "Stop"

            Add-Type -Path "$dllpath\AutoClickerSub.dll"
            Add-Type -Path "$dllpath\OpenCvSharp.Extensions.dll"
            $AC = "AutoClickerSub" -as [type]
            $BitmapConverter = "OpenCvSharp.Extensions.WriteableBitmapConverter" -as [type]

            $hwnd = $AC::GetWindowHandle($targetapp)
            $templatefiles = $AC::GetTemplateFileList($targetdir);
            $params = $AC::SetArgs($hwnd, $chkRealSize, $chkUseHDC, $chkColor, 0.5, 0.5, [double]$txtThreshold)
            while ($true) {
                #$params
                #$targetimage = $AC::GetCapture($params)
                #$targetimage
                #$imgCapture.Source = $BitmapConverter::ToWriteableBitmap($targetimage.Item1)
                $ret = $AC::TemplateMatch($params, $templatefiles)
                if ($ret.Item1) {
                    $AC::MouseEvent($hwnd, [MOUSEEVENT]::CLICK, $ret.item3.X, $ret.Item3.Y, 0, [fwKeys]::MK_LBUTTON )
                }
                Start-Sleep -Seconds 1
            } } -ArgumentList $dllpath, $targetapp, $targetdir, $chkRealSize.IsChecked, $chkUseHDC.IsChecked, $chkColor.IsChecked, 0.5, 0.5, $txtThreshold.Text
    })
$btnStop.add_Click( {
        $ErrorActionPreference = "silentlycontinue"
        if ($AutoJob) {
            receive-job -Name "AutoClick" | Out-Null
            Stop-Job -Name "AutoClick" | Out-Null
            Remove-Job -Name "AutoClick" | Out-Null
        }
        $ErrorActionPreference = "continue"
        $targetimage = $AC::GetCapture($params)
        $imgCapture.Source = $BitmapConverter::ToWriteableBitmap($targetimage.Item1)
    })
$form.ShowDialog() | Out-Null
$ErrorActionPreference = "silentlycontinue"
if ($AutoJob) {

    receive-job -Name "AutoClick" | Out-Null
    Stop-Job -Name "AutoClick" | Out-Null
    Remove-Job -Name "AutoClick" | Out-Null
}
$ErrorActionPreference = "continue"
