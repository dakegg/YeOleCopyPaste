$Agency = "GREEN"

. .\Execute-Runprofile.ps1

Execute-RunProfile 'PORTAL' 'Delta Import'
Get-Job | Wait-Job | Receive-Job

Execute-RunProfile 'PORTAL' 'Delta Sync'
Get-Job | Wait-Job | Receive-Job

Execute-RunProfile 'eadlab.local' 'Export'
Get-Job | Wait-Job | Receive-Job

Execute-RunProfile $Agency 'Export'
Get-Job | Wait-Job | Receive-Job

Write-Host -fore Yellow "Pausing here to go manually preview sync the objects from " -nonewline
Write-Host -fore Green "GREEN" -nonewline
Write-Host -fore Yellow " so everyone doesnt provision"
Pause

Execute-RunProfile 'eadlab.local' 'Export'
Get-Job | Wait-Job | Receive-Job

Execute-RunProfile 'eadlab.local' 'Delta Import'
Get-Job | Wait-Job | Receive-Job

Execute-RunProfile 'eadlab.local' 'Delta Sync'
Get-Job | Wait-Job | Receive-Job

Execute-RunProfile 'PORTAL' 'Export'
Get-Job | Wait-Job | Receive-Job

Execute-RunProfile $Agency 'Export'
Get-Job | Wait-Job | Receive-Job

Get-Job | Remove-Job





