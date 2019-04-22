function testport ($hostname=’yahoo.com’,$port=80,$timeout=100) {
$requestCallback = $state = $null
$client = New-Object System.Net.Sockets.TcpClient
$beginConnect = $client.BeginConnect($hostname,$port,
$requestCallback,$state)
Start-Sleep -milli $timeOut
23
Web
scrape.ps1
if ($client.Connected) { $open = $true } else { $open = $false }
$client.Close()
[pscustomobject]@{hostname=$hostname;port=$port;open=$open}
}

1..254 | %{testport -hostname "192.168.0.$_" -port ""} | Format-Table -AutoSize

