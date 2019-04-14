hash = @{}
$input_paths = "$env:localappdata\Packages\Microsoft.MicrosoftEdge
_8wekyb3d8bbwe\AC\#!001\MicrosoftEdge\Cookies\*", "$env:localappdata
\Mozilla\Firefox\Profiles\s3xrny7d.default\cache2\entries\*", "$env:
localappdata\Google\Chrome\User Data\Default\Cache\data*"
$regex = "\b172\.\d{1,3}\.[16,17,18,19,20,21,22,23,24,25,26,27,28,29,
30,31]\.\d{1,3}\b|\b10\.\d{1,3}\.\d{1,3}\.\d{1,3}\b|\b192\.168\.\d{1,3}
\.\d{1,3}\b|https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\b"
$input_paths | % {select-string -Path $_ -Pattern $regex -AllMatches |
% { $_.Matches } | % { $_.Value } | %{if($hash.$_ -eq $null) { $_ };
$hash.$_ = 1}}