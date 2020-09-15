$InstallPath = 'C:\DiscordBot'
$BuildPath = 'C:\source\foldingcash\DiscordBot\DiscordBot\DiscordBot.Console\bin\Release\netcoreapp3.1'

'InstallPath: ' + $InstallPath
'BuildPath: ' + $BuildPath

'Uninstalling windows service'
.\uninstall_windows_service.ps1

'Removing build'
Remove-Item -Path $InstallPath -Recurse
New-Item -Path $InstallPath -ItemType Directory
Copy-Item -Path $BuildPath -Destination $InstallPath -Recurse
'Copied build'

'Installing Windows Service'
.\install_windows_service.ps1
'Finished Install Windows Service'