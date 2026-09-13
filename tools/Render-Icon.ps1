# Render the project-owned SVG geometry as an antialiased 256px Thunderstore icon.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$bitmap = [Drawing.Bitmap]::new(1024, 1024)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$small = [Drawing.Bitmap]::new(256, 256)
$smallGraphics = [Drawing.Graphics]::FromImage($small)
$pen = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#F4F7F9'), 24)
try {
    $graphics.Clear([Drawing.ColorTranslator]::FromHtml('#122433'))
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform(4, 4)
    $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    foreach ($x in @(66, 138)) {
        $points = [Drawing.PointF[]]@(
            [Drawing.PointF]::new($x, 76),
            [Drawing.PointF]::new(($x + 52), 128),
            [Drawing.PointF]::new($x, 180)
        )
        $graphics.DrawLines($pen, $points)
    }
    $smallGraphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $smallGraphics.DrawImage($bitmap, [Drawing.Rectangle]::new(0, 0, 256, 256))
    $small.Save((Join-Path $root 'icon.png'), [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $pen.Dispose()
    $smallGraphics.Dispose()
    $small.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
