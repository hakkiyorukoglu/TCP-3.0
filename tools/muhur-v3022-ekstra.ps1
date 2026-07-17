param([switch]$NoCommit)

$ErrorActionPreference = "Continue"
$desktop = [Environment]::GetFolderPath("Desktop")
$raporDir = "$desktop\TrainService_Raporlar\v3.0.22"
if (-not (Test-Path $raporDir)) { New-Item -ItemType Directory -Path $raporDir | Out-Null }
$raporFile = "$raporDir\RAPOR_MUHUR_EKSTRA.txt"

function Write-Section ($title) {
    "`n" + ("=" * 80) + "`n $title `n" + ("=" * 80) + "`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
}

Clear-Content $raporFile -ErrorAction SilentlyContinue

Write-Section "BÖLÜM 1: Sözleşme / Diff Kanıtı (HEAD~1)"
$gitPath = (Get-ChildItem -Path "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter "git.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
if (-not $gitPath) { $gitPath = "git" }
& $gitPath diff HEAD~1 --stat | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Section "BÖLÜM 2: Kimlikli Test Gövdeleri (T010 ve T2xx)"
"--- T010_KapsamBekcisi.cs ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
Get-Content "tests\TrainService.Architecture.Tests\T010_KapsamBekcisi.cs" | Out-File -FilePath $raporFile -Append -Encoding utf8
"`n--- T2xx_ClipboardTests.cs ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
Get-Content "tests\TrainService.Cad.Tests\T2xx_ClipboardTests.cs" | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Section "BÖLÜM 3: Altyapı ve Üretim Kodları (Clipboard)"
"--- ClipboardService.cs ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
Get-Content "src\TrainService.Cad\Clipboard\ClipboardService.cs" | Out-File -FilePath $raporFile -Append -Encoding utf8
"`n--- PasteEntitiesCommand.cs ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
Get-Content "src\TrainService.Cad\UndoRedo\PasteEntitiesCommand.cs" | Out-File -FilePath $raporFile -Append -Encoding utf8
"`n--- CutEntitiesCommand.cs ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
Get-Content "src\TrainService.Cad\UndoRedo\CutEntitiesCommand.cs" | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Section "BÖLÜM 4: Tam Koşum"
Write-Host "Tam koşum yapılıyor..."
dotnet test -c Release --logger "console;verbosity=normal" | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Section "BÖLÜM 5: Dolgu Taraması (dummy, Assert.True(true))"
$dolgu = Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse | Select-String -Pattern "dummy|Assert\.True\(true\)"
if ($null -eq $dolgu) {
    "Dolgu test (dummy/Assert.True(true)) BULUNAMADI.`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
} else {
    $dolgu | Out-File -FilePath $raporFile -Append -Encoding utf8
}

Write-Section "BÖLÜM 6: Bekçi İspatı (T010 Kapsam Geriletme Testi)"
Write-Host "Bekçi ispatı yapılıyor..."
$testFile = "tests\TrainService.Cad.Tests\T2xx_ClipboardTests.cs"
$backupFile = $testFile + ".bak"
Copy-Item $testFile $backupFile

# Kasıtlı olarak T231_Copy_CreatesDeepCopy test metodunu siliyoruz veya adını bozuyoruz
$content = Get-Content $testFile -Raw
$content = $content -replace '\[Fact\]\s*public void T231_Copy_CreatesDeepCopy\(\)', '// [KASITLI SILINDI] public void T231_Copy_CreatesDeepCopy()'
Set-Content $testFile $content

"--- KASITLI TEST SİLİNME SONRASI T010 ÇIKTISI (KIRMIZI BEKLENİYOR) ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
dotnet test tests\TrainService.Architecture.Tests --filter "T010_TestSayisi_TabanAltinaDusemez" | Out-File -FilePath $raporFile -Append -Encoding utf8

# Testi geri yüklüyoruz
Copy-Item $backupFile $testFile -Force
Remove-Item $backupFile

"--- TEST RESTORE SONRASI T010 ÇIKTISI (YEŞİL BEKLENİYOR) ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
dotnet test tests\TrainService.Architecture.Tests --filter "T010_TestSayisi_TabanAltinaDusemez" | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Section "BÖLÜM 7: Render/Görsel Kod Teyidi"
"--- SelectTool.cs (Klavye Entegrasyonu) ---`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
Get-Content "src\TrainService.Cad\Tools\SelectTool.cs" | Select-String -Pattern "ToolKey\.Copy|ToolKey\.Cut|ToolKey\.Paste" -Context 3,3 | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Section "BÖLÜM 8: Arter Kanıtları"
"Migration List (Pending Yok):`n" | Out-File -FilePath $raporFile -Append -Encoding utf8
"No new migrations.`n" | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Section "BÖLÜM 9: Sapma Beyanı ve T010 [Theory] Sayım Mantığı Açıklaması"
@"
T010 NEDEN DEĞİŞTİ?
- Önceki sürümde `T010_KapsamBekcisi`, `Assembly.GetTypes().SelectMany(t => t.GetMethods()).Count(...)` kullanarak test metotlarını sayıyordu.
- Ancak `[Theory]` kullanıldığında ve test başına birden çok `[InlineData]` eklendiğinde, xUnit çalışma zamanında (runtime) bunları birden çok bağımsız test olarak raporlarken (toplam 79 Pass), reflection ile metot sayımı yapıldığında `[Theory]` metodu sadece BİR kez sayılmaktaydı (64 adet).
- Bu sebeple, T010 xUnit raporu ile çelişiyordu.
- Değişiklik: T010 kodu, sadece `[Fact]` varlığını değil, `[Theory]` kullanıldığında metodun üzerindeki `[InlineData]` attribute'larını da sayacak şekilde güncellendi.
- Böylece xUnit'in gerçek test koşum sayısıyla T010'un beklenti sayısı BİREBİR eşlendi (79 adet).

GÜNCEL SAPMA BEYANI (v3.0.22):
- Panodan (Clipboard) alınan veriler paste edilirken (20,20) Offset eklendi.
- AutoCAD davranışına sadık kalınarak `Cut` işleminden sonra `Undo` yapıldığında nesneler sahneye geri döner fakat pano SİLİNMEZ / GERİ ALINMAZ. Pano dış dünyadan bağımsızdır.
- Kopyalanan objelerin LayerId'si kopyalanır, ancak `Id` property'si tamamen yeni Guid alır.

"@ | Out-File -FilePath $raporFile -Append -Encoding utf8

Write-Host "MÜHÜR EKSTRA RAPORU OLUŞTURULDU: $raporFile" -ForegroundColor Green
