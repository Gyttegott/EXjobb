hash = @{}
$one = wmic service list full | findstr /i "pathname" |
findstr /i /v "system32" | % {$t = $_ -split ’=’; $t[1]}
$two = sc.exe query state=all | findstr "SERVICE_NAME:" |
% {$t = $_ -split ’ ’; $t[1]} | % {sc.exe qc "$_"} |
findstr "BINARY_PATH_NAME" | % { $t = $_ -split ": "; $t[1]}
$one + $two | % { if ($_.StartsWith(’"’)) {$r = $_ -split ’"’;
$s = $r[1]} else {$r = $_ -split ’ ’; $s = $r[0]} $s } |
%{if($hash.$_ -eq $null) { $_ }; $hash.$_ = 1} | % {icacls $_}