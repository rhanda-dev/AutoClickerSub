<#
	Select Target Applicatin & Execute AutoClickerSub.ps1
#>
$ErrorActionPreference = "stop"
Set-PSDebug -Strict

$rui = $host.UI.RawUI
$rui.WindowSize = New-Object System.Management.Automation.Host.Size(50, 20)

Remove-Variable -Name * -Scope Global -Force -ErrorAction Ignore
Remove-Variable -Name * -Scope Script -Force -ErrorAction Ignore
$dllpath = "C:\tools\MyTools\AutoClickerSub"

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase, System.Windows.Forms, ReachFramework
Add-Type -Path "$dllpath\AutoClickerSub.dll"
$MessageBox = "System.Windows.Forms.MessageBox" -as [type]

$applist = [AutoClickerSub]::GetProcessList($true) # $true process have WindowText only.

$xamlfile = $PSScriptRoot + "\" + [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Name) + ".xaml"
[xml]$xaml = [System.IO.File]::ReadAllText($xamlfile, [System.Text.Encoding]::UTF8)
$form = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $xaml))
$xaml.SelectNodes("//*") | ? { $_.Attributes["x:Name"] -ne $null } | % {
	New-Variable  -Name $_.Name -Value $form.FindName($_.Name) -Force
}
$form.FontSize = 16
$dgApplicationList.ItemsSource = $applist
$executesub = $false
$btnCancel.add_Click( {
		$script:executesub = $false
		$form.Close()
	})
$btnSelect.add_Click( {
		if ($tbSelected.Text.Length -gt 0) {
			$script:executesub = $true
			$form.Close()
		}
	})
$btnRefresh.add_Click( {
		$script:dgApplicationList.ItemsSource = $null
		$script:applist = [AutoClickerSub]::GetProcessList(-not $chkShow.IsChecked)
		$script:dgApplicationList.ItemsSource = $script:applist
	})
$form.ShowDialog() | Out-Null
$hwnd = [IntPtr][int]$tbSelected.Text
if (0 -eq $hwnd) {
	Exit
}
if (	$executesub) {
	$AutoClickerSub = "$PSScriptRoot\AutoClickerSub.ps1" + " -hwnd $hwnd" + " -targetdir ""$PSScriptRoot"""
	Invoke-Expression $AutoClickerSub
}
exit
