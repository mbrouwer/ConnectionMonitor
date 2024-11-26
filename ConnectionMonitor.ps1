#!/bin/pwsh

function Start-Monitor
{
    [CmdletBinding()]
    param (
        [Parameter()]$destination,
        [Parameter()]$port,
        [Parameter()]$protocol
    )

    $outputFile = "./$((Get-Date).ToString("yyyyMMddHHmmss"))_$($destination.Replace(".","-"))_$($port).xml"
    $port = $protocol -eq "TCP" ? "-pT:$($port)" : " -sU -pU:$($port)" 
    $parameters = "-oX $($outputFile) -Pn"

    Start-Process -FilePath "nmap" -ArgumentList "$($parameters) $($port) $($destination)"
    # [xml]$xml = Get-Content -Path $outputFile | ConvertTo-Json -Depth 99 | Out-File -FilePath "$($outputFile.Replace(".xml",".json"))"
    # Remove-Item -Path $outputFile
}

Write-Host "Connection Monitor"

$monitorConfigs = Get-Content -Path ./monitor.json | ConvertFrom-Json

Start-Monitor -destination 192.168.0.1 -port 53 -protocol TCP
# Start-Monitor -destination 192.168.0.1 -port 53 -protocol UDP