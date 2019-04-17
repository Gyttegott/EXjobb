function testport ($hostname=’yahoo.com’,$port=80,$timeout=100) {
$requestCallback = $state = $null
$client = New-Object System.Net.Sockets.TcpClient
$beginConnect = $client.BeginConnect($hostname,$port,
$requestCallback,$state)
Start-Sleep -milli $timeOut
if ($client.Connected) { $open = $true } else { $open = $false }
$client.Close()
[pscustomobject]@{hostname=$hostname;port=$port;open=$open}
}

80..443 | %{testport -hostname "" -port $_} | Format-Table -AutoSize
