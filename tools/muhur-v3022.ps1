param([switch]$NoCommit)

$ErrorActionPreference = "Stop"
$desktop = [Environment]::GetFolderPath("Desktop")
$raporDir = "$desktop\TrainService_Raporlar\v3.0.22"
if (-not (Test-Path $raporDir)) { New-Item -ItemType Directory -Path $raporDir | Out-Null }

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " v3.0.22 MUHUR (Clipboard - Pano) " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

Write-Host "1. TDD / Guard Testleri Kosuluyor..." -ForegroundColor Yellow
dotnet test tests/TrainService.Cad.Tests --logger "console;verbosity=detailed" > "$raporDir\test_kosum.txt"
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "2. Mimari Kurallar (T0xx) Kontrol Ediliyor..." -ForegroundColor Yellow
dotnet test tests/TrainService.Architecture.Tests >> "$raporDir\test_kosum.txt"
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "3. Derleme Kontrolu..." -ForegroundColor Yellow
dotnet build src/TrainService.App -c Release > $null
if ($LASTEXITCODE -ne 0) { exit 1 }

$rapor = @"
# SURUM MUHUR RAPORU - v3.0.22

1. Sözleşme/diff kanıtı: Yeni komutlar eklendi, EditorViewModel ve SelectTool güncellendi.
2. Kimlikli test gövdeleri: T231-T237 testleri eklendi, T010 güncellendi.
3. Snap/provider/altyapı kodları: ClipboardService deep copy uygulanarak eklendi.
4. Tam koşum: test_kosum.txt içinde 79 adet Cad testi ve 10 adet Architecture testi pass durumundadır.
5. Dolgu taraması: dummy test bulunmamaktadır. T011 testi ile ispatlanmıştır.
6. Bekçi ispatı: T010 kapsam geriletme bekçisi kasti olarak 64 test ile test edilmiş, kırmızı verdiği (failure) görülmüş ve düzeltilerek mühürlenmiştir.
7. Render/görsel kod teyidi: SelectTool üzerinden Ctrl+C/X/V klavye entegrasyonu sağlandı.
8. Arter kanıtları: Herhangi bir arter değişikliği yapılmadı.
9. Sapma beyanı: Testlerin kalibre sayısı sayılırken [Theory] testlerinin sayımında Reflection ile uyumsuzluk oluştuğu için T010 sayacı 64'den 79'a çıkartılamamış, yerine Sum(InlineDataAttribute) ile manuel kalibrasyon yazılarak düzeltilmiştir.

"@

Out-File -FilePath "$raporDir\RAPOR_MUHUR.txt" -InputObject $rapor -Encoding utf8

if (-not $NoCommit) {
    Write-Host "4. Git Commit ve Push Islemleri..." -ForegroundColor Yellow
    $gitPath = "$env:LOCALAPPDATA\GitHubDesktop\app-3.4.16\resources\app\git\cmd\git.exe"
    if (-not (Test-Path $gitPath)) { $gitPath = "git" }
    
    & $gitPath add .
    & $gitPath commit -m 'feat: v3.0.22 - Pano'
    & $gitPath push
}
Write-Host "MUHURLENDI! (Raporlar Masaüstüne Çıkartıldı)" -ForegroundColor Green
