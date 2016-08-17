
if((Test-Path -Path "nuget.exe") -eq $false)
{
	Write-Host "Downloading Nuget.exe"
	
	#$sourceNugetExe = "http://nuget.org/nuget.exe" 
	$sourceNugetExe = "http://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
	$targetNugetExe = "nuget.exe"
	Invoke-WebRequest $sourceNugetExe -OutFile $targetNugetExe
}
else
{
	Write-Host "Nuget.exe already downloaded"
}