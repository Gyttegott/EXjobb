function test-cred($username,$password){
$CurrentDomain = "LDAP://" + ([ADSI]"").distinguishedName
$domain = New-Object System.DirectoryServices.
DirectoryEntry($CurrentDomain,$UserName,$Password)
if ($domain.name -eq $null)
{
write-host "Username and password does not match"
}
else
{
write-host "Username and password match"
}
}

test-cred -username "" -password ""