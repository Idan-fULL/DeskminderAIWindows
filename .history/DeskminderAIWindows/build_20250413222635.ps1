# Build script for DeskminderAI Windows application
param (
    [switch]$Release,
    [switch]$CreatePackage
)

# Set paths
$projectDir = $PSScriptRoot
$slnPath = Join-Path (Split-Path -Parent $projectDir) "DeskminderAIWindows.sln"
$buildConfig = if ($Release) { "Release" } else { "Debug" }
$outputDir = Join-Path $projectDir "bin" $buildConfig "net6.0-windows"

# Ensure the Images directory exists
$imagesDir = Join-Path $projectDir "Images"
if (!(Test-Path $imagesDir)) {
    New-Item -ItemType Directory -Path $imagesDir | Out-Null
    Write-Host "Created Images directory"
}

# Check for icon.svg and attempt to create icon.ico if missing
$svgPath = Join-Path $imagesDir "icon.svg"
$icoPath = Join-Path $imagesDir "icon.ico"

if ((Test-Path $svgPath) -and !(Test-Path $icoPath)) {
    Write-Host "SVG icon found but ICO missing. Attempting to convert..."
    $convertScript = Join-Path $projectDir "Tools\ConvertSvgToIco.ps1"
    if (Test-Path $convertScript) {
        & $convertScript
    } else {
        Write-Host "ConvertSvgToIco.ps1 not found. Please create icon.ico manually."
    }
}

# Build the solution
Write-Host "Building solution in $buildConfig configuration..."
dotnet build $slnPath -c $buildConfig

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Build completed successfully"

# Create Windows Store package if requested
if ($CreatePackage -and $Release) {
    # Check if makeappx is available
    $makeappxPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe"
    
    if (Test-Path $makeappxPath) {
        Write-Host "Creating Windows Store package..."
        
        # Create necessary package directories
        $packageDir = Join-Path $projectDir "Package"
        $packageBuildDir = Join-Path $packageDir "Build"
        
        if (!(Test-Path $packageDir)) {
            New-Item -ItemType Directory -Path $packageDir | Out-Null
        }
        
        if (Test-Path $packageBuildDir) {
            Remove-Item -Path $packageBuildDir -Recurse -Force
        }
        
        New-Item -ItemType Directory -Path $packageBuildDir | Out-Null
        
        # Copy files to package build directory
        Copy-Item -Path (Join-Path $outputDir "*") -Destination $packageBuildDir -Recurse
        Copy-Item -Path (Join-Path $projectDir "Package.appxmanifest") -Destination $packageBuildDir
        
        # Create the package
        $appxPath = Join-Path $packageDir "DeskminderAI.appx"
        & $makeappxPath pack /d $packageBuildDir /p $appxPath
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Package created at: $appxPath"
        } else {
            Write-Host "Failed to create package"
        }
    } else {
        Write-Host "makeappx.exe not found. Please install the Windows 10 SDK."
        Write-Host "You can download it from: https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk/"
    }
}

Write-Host "Done." 