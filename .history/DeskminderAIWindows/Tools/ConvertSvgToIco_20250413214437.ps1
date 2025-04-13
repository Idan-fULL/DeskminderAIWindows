# This script requires Inkscape to be installed
# It converts the SVG icon to PNG files of various sizes, then uses ImageMagick to convert them to an ICO file

# Path to Inkscape - adjust this for your system
$inkscapePath = "C:\Program Files\Inkscape\bin\inkscape.exe"

# Path to ImageMagick convert tool - adjust this for your system
$convertPath = "C:\Program Files\ImageMagick-7.0.8-Q16\convert.exe"

# Set paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$svgPath = Join-Path $rootPath "Images\icon.svg"
$imagesPath = Join-Path $rootPath "Images"

# Create Images directory if it doesn't exist
if (!(Test-Path $imagesPath)) {
    New-Item -ItemType Directory -Path $imagesPath | Out-Null
}

# Define sizes to generate
$sizes = @(16, 32, 48, 64, 128, 256)

# Check if Inkscape is installed
if (!(Test-Path $inkscapePath)) {
    Write-Host "Inkscape not found at $inkscapePath. Please install Inkscape or update the path in this script."
    Write-Host "You can download Inkscape from: https://inkscape.org/release/"
    Exit
}

# Convert SVG to PNG at different sizes
foreach ($size in $sizes) {
    $outputPng = Join-Path $imagesPath "icon$size.png"
    
    # Use Inkscape to convert SVG to PNG
    & $inkscapePath --export-filename="$outputPng" -w $size -h $size "$svgPath"
    
    if (Test-Path $outputPng) {
        Write-Host "Created $outputPng"
    } else {
        Write-Host "Failed to create $outputPng"
    }
}

# Check if ImageMagick is installed
if (Test-Path $convertPath) {
    # Create ICO file with all sizes
    $icoPath = Join-Path $imagesPath "icon.ico"
    $pngPaths = $sizes | ForEach-Object { Join-Path $imagesPath "icon$_.png" }
    
    & $convertPath $pngPaths $icoPath
    
    if (Test-Path $icoPath) {
        Write-Host "Created $icoPath"
    } else {
        Write-Host "Failed to create $icoPath"
    }
} else {
    Write-Host "ImageMagick not found at $convertPath. ICO file not created."
    Write-Host "You can manually create an ICO file from the PNG files."
    Write-Host "You can download ImageMagick from: https://imagemagick.org/script/download.php"
}

Write-Host "Done." 