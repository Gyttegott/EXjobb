# 1
systeminfo
#2
net user
#3
whoami /all
#4
Get-ChildItem C:\Users -Force | select name
#5
net user Administratör
#6
net accounts
#7
Get-LocalGroup | %{$_; Get-LocalGroupMember -Group "$_"}
#8
ipconfig /all
#9
findstr /si password *.xml *.ini *.txt *.config
#10
cmd /c "dir /S /B *pass*.txt == *pass*.xml == *pass*.ini == *cred* == *vnc* == *.config*"
#11
#See RegScarpe.ps1
#12
Get-Childitem -Path C:\inetpub\ -Include web.config -File -Recurse -ErrorAction SilentlyContinue
#13
#See WLANExtract.bat
#14
#See ProgramExtract.ps1
#15
#See ProcessExtract.ps1
#16
tasklist /v /fi "username eq system"
#17
REG QUERY "HKLM\SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine" /v PowerShellVersion
#18
cmd /c "schtasks /query /fo LIST 2>nul | findstr TaskName";
Get-ScheduledTask | where $_.TaskPath -notlike "\Microsoft*" | ft TaskName,TaskPath,State
#19
#See AutostartExtract.ps1
#20
#See VulnServiceExtract.ps1
#21
gwmi -class Win32_Service -Property Name, DisplayName, PathName, StartMode | Where $_.StartMode -eq "Auto" -and $_.PathName -notlike "C:\Windows*" -and $_.PathName -notlike '"*' | select PathName,DisplayName,Name
#22
reg query HKLM\SOFTWARE\Policies\Microsoft\Windows\Installer /v AlwaysInstallElevated;
reg queryHKCU\SOFTWARE\Policies\Microsoft\Windows\Installer /v AlwaysInstallElevated
#23
REG QUERY "HKCU\Software\Microsoft\TerminalServer Client\Servers" /s
#24
Get-PSDrive
#25
#See CacheExtract.ps1
#26
#See GetChromeCreds.ps1
#27
#See GetEdgeCreds.ps1
#28
([adsisearcher]"objectCategory=User").Findall() | ForEach_.properties.samaccountname}
#29
1..254 | %{nslookup "192.168.0.$_"} | select-string "Name" -Context 0,1
#30
$ping = New-Object System.Net.Networkinformation.Ping;
1..254 | %{$ping.send("192.168.0.$_") | select address,status}
#31
#See PortScanAll.ps1
#32
#See PortScanOne.ps1
#33
#See WebScrape.ps1
#34
#See RemoteSession.ps1
#35
#See TestCred.ps1
#36
([adsisearcher]"(&(objectCategory=User)(samaccountname=<<user name>>))").FindOne() | %{[datetime]::FromFileTime([int64]::Parse($_.properties.item("lastLogon") ))}
#37
([adsisearcher]"objectCategory=User").Findall() | %{$s ="$($_.properties.samaccountname) - $($_.properties.description)"; $s}
#38
([adsisearcher]"(&(objectCategory=User)(samaccountname=<<user name>>))").FindOne() | %{$_.properties.memberof}
