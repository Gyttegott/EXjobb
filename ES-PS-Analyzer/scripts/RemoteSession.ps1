function connect_remote ($hostname,$account,$password){
$pass = ConvertTo-SecureString $password -AsPlainText -Force
$cred= New-Object System.Management.Automation.PSCredential
($account, $pass )
Enter-PSSession -ComputerName $hostname -Credential $cred
}

connect_remote -hostname "" -account "" -password ""