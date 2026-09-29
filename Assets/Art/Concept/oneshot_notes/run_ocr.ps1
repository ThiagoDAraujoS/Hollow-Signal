Add-Type -AssemblyName System.Drawing
[Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null
[Windows.Graphics.Imaging.BitmapDecoder, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.StorageFile, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if (-not $engine) {
    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage([Windows.Globalization.Language]::new("en-US"))
}

$images = Get-ChildItem "Assets\Art\concept\oneshot_notes\*.png"
foreach ($img in $images) {
    Write-Output "=== FILE: $($img.Name) ==="
    $fileTask = [Windows.Storage.StorageFile]::GetFileFromPathAsync($img.FullName)
    $file = $fileTask.AsTask().GetAwaiter().GetResult()
    $streamTask = $file.OpenAsync([Windows.Storage.FileAccessMode]::Read)
    $stream = $streamTask.AsTask().GetAwaiter().GetResult()
    $decoderTask = [Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)
    $decoder = $decoderTask.AsTask().GetAwaiter().GetResult()
    $bmpTask = $decoder.GetSoftwareBitmapAsync()
    $softwareBmp = $bmpTask.AsTask().GetAwaiter().GetResult()
    $ocrResultTask = $engine.RecognizeAsync($softwareBmp)
    $ocrResult = $ocrResultTask.AsTask().GetAwaiter().GetResult()
    Write-Output $ocrResult.Text
    Write-Output "`n-----------------------------------------`n"
}
