# Icon 4: Röd cirkel (som icon 1) men med nedladdningspil istället för play.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outDir = $PSScriptRoot
$S = 256
$M = 16
$Inner = $S - 2 * $M

function Save-AsIco {
    param([System.Drawing.Bitmap]$Source, [string]$IcoPath)
    $sizes = @(16, 32, 48, 64, 128, 256)
    $images = @()
    foreach ($sz in $sizes) {
        $tmp = New-Object System.Drawing.Bitmap -ArgumentList $sz, $sz
        $tg = [System.Drawing.Graphics]::FromImage($tmp)
        $tg.SmoothingMode = 'AntiAlias'
        $tg.InterpolationMode = 'HighQualityBicubic'
        $tg.PixelOffsetMode = 'HighQuality'
        $tg.DrawImage($Source, 0, 0, $sz, $sz)
        $tg.Dispose()
        $ms = New-Object System.IO.MemoryStream
        $tmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $images += [pscustomobject]@{ Size = $sz; Bytes = $ms.ToArray() }
        $tmp.Dispose(); $ms.Dispose()
    }
    $fs = [System.IO.File]::Open($IcoPath, 'Create')
    $bw = New-Object System.IO.BinaryWriter -ArgumentList $fs
    try {
        $bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$images.Count)
        $offset = 6 + ($images.Count * 16)
        foreach ($img in $images) {
            $w = if ($img.Size -ge 256) { 0 } else { $img.Size }
            $bw.Write([byte]$w); $bw.Write([byte]$w)
            $bw.Write([byte]0); $bw.Write([byte]0)
            $bw.Write([uint16]1); $bw.Write([uint16]32)
            $bw.Write([uint32]$img.Bytes.Length); $bw.Write([uint32]$offset)
            $offset += $img.Bytes.Length
        }
        foreach ($img in $images) { $bw.Write($img.Bytes) }
    } finally { $bw.Dispose(); $fs.Dispose() }
}

$bmp = New-Object System.Drawing.Bitmap -ArgumentList $S, $S
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.InterpolationMode = 'HighQualityBicubic'
$g.PixelOffsetMode = 'HighQuality'

# Röd gradient-cirkel (samma som icon 1)
$rect = New-Object System.Drawing.Rectangle -ArgumentList $M, $M, $Inner, $Inner
$c1 = [System.Drawing.Color]::FromArgb(255, 230, 33, 33)
$c2 = [System.Drawing.Color]::FromArgb(255, 170, 10, 10)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush -ArgumentList $rect, $c1, $c2, 90.0
$g.FillEllipse($brush, $rect)

# Subtil inner ring
$hiCol = [System.Drawing.Color]::FromArgb(60, 255, 255, 255)
$hi = New-Object System.Drawing.Pen -ArgumentList $hiCol, 4
$hiRect = New-Object System.Drawing.Rectangle -ArgumentList ($M + 8), ($M + 8), ($Inner - 16), ($Inner - 16)
$g.DrawEllipse($hi, $hiRect)

# Vit nedladdningspil
$cx = $S / 2.0; $cy = $S / 2.0
$arrowPen = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::White), 22
$arrowPen.StartCap = 'Round'; $arrowPen.EndCap = 'Round'
$g.DrawLine($arrowPen, [single]$cx, [single]($cy - 55), [single]$cx, [single]($cy + 20))

# Pilspets
$ap1 = New-Object System.Drawing.PointF -ArgumentList ($cx - 48), ($cy - 5)
$ap2 = New-Object System.Drawing.PointF -ArgumentList $cx, ($cy + 55)
$ap3 = New-Object System.Drawing.PointF -ArgumentList ($cx + 48), ($cy - 5)
$g.FillPolygon([System.Drawing.Brushes]::White, @($ap1, $ap2, $ap3))

$bmp.Save("$outDir\icon4-red-download.png", [System.Drawing.Imaging.ImageFormat]::Png)
Save-AsIco $bmp "$outDir\icon4-red-download.ico"
$g.Dispose(); $bmp.Dispose()
Write-Host "Icon 4 (Röd nedladdning) genererad."
