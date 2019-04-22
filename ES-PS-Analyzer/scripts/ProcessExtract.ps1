tasklist /v
net start
sc.exe query
wmic service list brief
Get-Service
Get-WmiObject -Query "Select * from Win32_Process" |
where {$_.Name -notlike "svchost*"} | Select Name, Handle,
@{Label="Owner";Expression={$_.GetOwner().User}} | ft -AutoSize