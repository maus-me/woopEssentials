# you may need to set in a powershell (Admin) terminal the execution policy to allow execution of local scripts, can be done with below command
# Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

$logfile = "vs-log.log"
$copylogfile = "vs-copylog.log"
# worldfile name without file extension
$worldFile = "default"
# world saves directory
$worldDirectory = "C:\Users\Administrator\AppData\Roaming\VintagestoryData\Saves"
# backup directory
$backupDirectory = "D:\vs-backup"
# keep backups for x days
$keep = 4

$7zip = "C:\Program Files\7-Zip\7z.exe"
$vsexe = "C:\VS-Server\Vintagestory\VintagestoryServer.exe"


Start-Transcript -Path $logfile -Append
$job = $null

while (1) {
    Write-Host "Vintage Stor server is starting"
    Start-Process -FilePath $vsexe -Wait
    Write-Host "Vintage Stor server stopped"
    Start-Sleep -s 2
    $timeStamp = Get-Date -Format "MM-dd-yyyy-HH-mm-ss"
    $file = Get-Item """$worldDirectory\$worldFile.vcdbs"""
    Write-Host """$worldDirectory\$worldFile.vcdbs"" -> ""$backupDirectory\$worldFile-$timeStamp.vcdbs"" :: $($file.length/1GB)GB"
    Start-Process "Robocopy" -ArgumentList """$worldDirectory"" ""$backupDirectory"" ""$worldFile.vcdbs"" /j /v" -RedirectStandardOutput $copylogfile -NoNewWindow -Wait
    Rename-Item """$backupDirectory\$worldFile.vcdbs""" -NewName """$worldFile-$timeStamp.vcdbs"""
    if($null -ne $job){
        Remove-Job $job
    }
    $job = Start-Job {
        $worldFile = $args[0]
        $backupDirectory = $args[1]
        $timeStamp = $args[2]
        $7zip = $args[3]
        $keep = $args[4]
        Start-Process $7zip -ArgumentList "a -tzip ""$backupDirectory\$worldFile-$timeStamp.zip"" ""$backupDirectory\$worldFile-$timeStamp.vcdbs""" -Wait
        Remove-Item """$backupDirectory\$worldFile-$timeStamp.vcdbs"""
        $items = Get-ChildItem "$backupDirectory"
        $time = (Get-Date).AddDays(-$keep)
        foreach ($item in $items) {
            if($item.CreationTime -lt $time){
                Write-Host "deleting: $($item.FullName)"
                Remove-Item $item.FullName
            }
        }
    } -ArgumentList $worldFile, $backupDirectory, $timeStamp, $7zip, $keep
    Start-Sleep -s 2
}

Stop-Transcript