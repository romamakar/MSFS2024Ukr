$DestinationRoot='C:\Users\roman\OneDrive\Desktop\flight\MSFS2024Ukr\MSFS2024Ukr\newestdata\Packages'
$SourceRoot='C:\XboxGames\Microsoft Flight Simulator 2024\Content\Packages'
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) {
	throw "Source root folder does not exist: $SourceRoot"
}

if (-not (Test-Path -LiteralPath $DestinationRoot)) {
	New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
}

$sourceRootFull = [System.IO.Path]::GetFullPath($SourceRoot)
$destinationRootFull = [System.IO.Path]::GetFullPath($DestinationRoot)

$locPakFiles = Get-ChildItem -LiteralPath $sourceRootFull -Recurse -File |
	Where-Object { $_.Name -ieq 'ru-RU.locPak' }

$locFiles = Get-ChildItem -LiteralPath $sourceRootFull -Recurse -File |
	Where-Object { $_.Extension -ieq '.loc' }

foreach ($locPakFile in $locPakFiles) {
	$relativeDirectory = [System.IO.Path]::GetRelativePath($sourceRootFull, $locPakFile.DirectoryName)

	if ([string]::IsNullOrWhiteSpace($relativeDirectory) -or $relativeDirectory -eq '.') {
		$targetDirectory = $destinationRootFull
	}
	else {
		$targetDirectory = Join-Path $destinationRootFull $relativeDirectory
	}

	New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null

	$targetLocPakPath = Join-Path $targetDirectory $locPakFile.Name
	Copy-Item -LiteralPath $locPakFile.FullName -Destination $targetLocPakPath -Force

	$layoutPath = Join-Path $locPakFile.DirectoryName 'layout.json'
	if (Test-Path -LiteralPath $layoutPath -PathType Leaf) {
		$targetLayoutPath = Join-Path $targetDirectory 'layout.json'
		Copy-Item -LiteralPath $layoutPath -Destination $targetLayoutPath -Force
	}
}

foreach ($locFile in $locFiles) {
	$relativeDirectory = [System.IO.Path]::GetRelativePath($sourceRootFull, $locFile.DirectoryName)

	if ([string]::IsNullOrWhiteSpace($relativeDirectory) -or $relativeDirectory -eq '.') {
		$targetDirectory = $destinationRootFull
	}
	else {
		$targetDirectory = Join-Path $destinationRootFull $relativeDirectory
	}

	New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null

	$targetLocPath = Join-Path $targetDirectory $locFile.Name
	Copy-Item -LiteralPath $locFile.FullName -Destination $targetLocPath -Force
}

Write-Host "Copied $($locPakFiles.Count) ru-RU.locPak file(s) and $($locFiles.Count) .loc file(s) to '$destinationRootFull'."
