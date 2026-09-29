Add-Type -Path 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.WindowsRuntime.dll'
Add-Type -AssemblyName System.Drawing
[Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null
[Windows.Graphics.Imaging.BitmapDecoder, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.StorageFile, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null

function Await-Async($asyncOp) {
    $asTaskGeneric = [System.WindowsRuntimeSystemExtensions].GetMethods() | 
        Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.IsGenericMethod } | 
        Select-Object -First 1
    $targetType = $asyncOp.GetType().GetInterfaces() | 
        Where-Object { $_.IsGenericType -and $_.GetGenericTypeDefinition().FullName -eq 'Windows.Foundation.IAsyncOperation`1' } | 
        ForEach-Object { $_.GetGenericArguments()[0] } | Select-Object -First 1
    if ($targetType) {
        $asTaskMethod = $asTaskGeneric.MakeGenericMethod($targetType)
        $task = $asTaskMethod.Invoke($null, @($asyncOp))
        return $task.GetAwaiter().GetResult()
    }
    return $null
}

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if (-not $engine) {
    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage([Windows.Globalization.Language]::new("en-US"))
}

$images = Get-ChildItem "Assets\Art\concept\oneshot_notes\*.png"
foreach ($img in $images) {
    Write-Output "=== FILE: $($img.Name) ==="
    try {
        $fileOp = [Windows.Storage.StorageFile]::GetFileFromPathAsync($img.FullName)
        $file = Await-Async $fileOp
        
        $streamOp = $file.OpenAsync([Windows.Storage.FileAccessMode]::Read)
        $stream = Await-Async $streamOp
        
        $decoderOp = [Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)
        $decoder = Await-Async $decoderOp
        
        $bmpOp = $decoder.GetSoftwareBitmapAsync()
        $softwareBmp = Await-Async $bmpOp
        
        $ocrOp = $engine.RecognizeAsync($softwareBmp)
        $ocrResult = Await-Async $ocrOp
        
        Write-Output $ocrResult.Text
    } catch {
        Write-Output "Error: $_"
    }
    Write-Output "`n-----------------------------------------`n"
}
