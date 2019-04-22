# 1y
systeminfo
#2y
net user
#3y
whoami /all
#4y
Get-ChildItem C:\Users -Force | select name
#5y
net user Administratör
#6y
net accounts
#7y
Get-LocalGroup | %{$_; Get-LocalGroupMember -Group "$_"}
#8y
ipconfig /all
#9y
findstr /si password *.xml *.ini *.txt *.config
#10 måste göras något åt för att parse i loggen
cmd /c "dir /S /B *pass*.txt == *pass*.xml == *pass*.ini == *cred* == *vnc* == *.config*"
#11y 
#See RegScarpe.ps1
#12y
Get-Childitem -Path C:\inetpub\ -Include web.config -File -Recurse -ErrorAction SilentlyContinue
#13 måste testa på bärbar dator
#See WLANExtract.bat
#14y
#See ProgramExtract.ps1
#15y
#See ProcessExtract.ps1
#16y
tasklist /v /fi "username eq system"
#17y
REG QUERY "HKLM\SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine" /v PowerShellVersion
#18y
cmd /c "schtasks /query /fo LIST 2>nul | findstr TaskName";
Get-ScheduledTask | where $_.TaskPath -notlike "\Microsoft*" | ft TaskName,TaskPath,State
#19y
#See AutostartExtract.ps1
#20y
#See VulnServiceExtract.ps1
#21y
gwmi -class Win32_Service -Property Name, DisplayName, PathName, StartMode | Where $_.StartMode -eq "Auto" -and $_.PathName -notlike "C:\Windows*" -and $_.PathName -notlike '"*' | select PathName,DisplayName,Name
#22y
reg query HKLM\SOFTWARE\Policies\Microsoft\Windows\Installer /v AlwaysInstallElevated;
reg query HKCU\SOFTWARE\Policies\Microsoft\Windows\Installer /v AlwaysInstallElevated
#23y
REG QUERY "HKCU\Software\Microsoft\TerminalServer Client\Servers" /s
#24y
Get-PSDrive
#25y
#See CacheExtract.ps1
#26y
#See GetChromeCreds.ps1
#27y
#See GetEdgeCreds.ps1
#28!
([adsisearcher]"objectCategory=User").Findall() | ForEach_.properties.samaccountname}
#29y
1..254 | %{nslookup "192.168.0.$_"} | select-string "Name" -Context 0,1
#30y
$ping = New-Object System.Net.Networkinformation.Ping;
1..254 | %{$ping.send("192.168.0.$_") | select address,status}
#31y
#See PortScanAll.ps1
#32y
#See PortScanOne.ps1
#33y
#See WebScrape.ps1
#34y
#See RemoteSession.ps1
#35y
#See TestCred.ps1
#36!
([adsisearcher]"(&(objectCategory=User)(samaccountname=<<user name>>))").FindOne() | %{[datetime]::FromFileTime([int64]::Parse($_.properties.item("lastLogon") ))}
#37
([adsisearcher]"objectCategory=User").Findall() | %{$s ="$($_.properties.samaccountname) - $($_.properties.description)"; $s}
#38
([adsisearcher]"(&(objectCategory=User)(samaccountname=<<user name>>))").FindOne() | %{$_.properties.memberof}
