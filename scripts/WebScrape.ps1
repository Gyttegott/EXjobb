$hash = @{}
$base_url = <<host name/ip>>
$output_folder = <<output folder>>
function Scrape ($url) {
Write-Host $url
$hash.$url = 1;
$res = Invoke-WebRequest -Uri $url -UseDefaultCredentials
$res | Select-Object -Expand Content > ($output_folder +
$url.replace("/"," ") + ".txt")
$res | Select-Object -Expand Links | Select href |
%{if($hash.($base_url + $_.href) -eq $null -And
$_.href.StartsWith("/")) { Scrape ($base_url + $_.href) }}
}