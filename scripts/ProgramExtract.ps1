Get-WmiObject -Class Win32_Product
Get-ChildItem ’C:\Program Files’, ’C:\Program Files (x86)’ | ft Parent,
Name,LastWriteTime
Get-ChildItem -path Registry::HKEY_LOCAL_MACHINE\SOFTWARE | ft Name