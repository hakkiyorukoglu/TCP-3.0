$desk = Join-Path $env:USERPROFILE "Desktop\Denetim_Dokumu_v3019"
if (-not (Test-Path $desk)) { New-Item -ItemType Directory -Force -Path $desk | Out-Null }

$root = (Get-Location).Path
$ts = Get-Date -Format 'yyyy-MM-dd HH:mm'

function DumpFiles($cikti, $baslik, $klasorler, $desenler) {
    "########## $baslik - $ts ##########" | Out-File $cikti -Encoding utf8
    foreach ($k in $klasorler) {
        if (-not (Test-Path $k)) { "### KLASOR YOK: $k" | Out-File $cikti -Append -Encoding utf8; continue }
        Get-ChildItem -Path $k -Recurse -Include $desenler -File |
            Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' } |
            Sort-Object FullName | ForEach-Object {
                $rel = $_.FullName.Substring($root.Length).TrimStart('\')
                "`n===== FILE: $rel ($($_.Length) B) =====" | Out-File $cikti -Append -Encoding utf8
                Get-Content $_.FullName -Encoding utf8 | Out-File $cikti -Append -Encoding utf8
            }
    }
    Write-Host "OK: $cikti"
}

# D1 — CEKIRDEK: Core (A2 domain+geometri) + Cad (CAD motoru, snap, tool, komut, spatial)
DumpFiles "$desk\D1_cekirdek.txt" "CORE + CAD (cekirdek)" @("src\TrainService.Core","src\TrainService.Cad") @("*.cs")

# D2 — VERI+MESAJ: Data (A4 SQLite+EF), Messaging (A3 MQTT)
DumpFiles "$desk\D2_veri_mesaj.txt" "DATA + MESSAGING (A3/A4 arterleri)" @("src\TrainService.Data","src\TrainService.Messaging") @("*.cs")

# D3 — TESTLER: tum test projeleri (dolgu avi + gercek govde denetimi)
DumpFiles "$desk\D3_testler.txt" "TUM TESTLER" @("tests") @("*.cs")

# D4 — APP+DI+XAML: DI kurulumu, ViewModel, XAML (kisayollar/F9), csproj
DumpFiles "$desk\D4_app.txt" "APP + DI + XAML" @("src\TrainService.App") @("*.cs","*.xaml")

# D5 — DIGER PROJELER: Simulation, Firmware (iskelet mi, erken doldurulmus mu?)
DumpFiles "$desk\D5_diger.txt" "SIMULATION + FIRMWARE (iskelet kontrolu)" @("src\TrainService.Simulation","src\TrainService.Firmware") @("*.cs")

# D6 — YAPISAL KANITLAR (grep + ef + agac)
$c6 = "$desk\D6_yapisal.txt"
"########## YAPISAL KANITLAR - $ts ##########" | Out-File $c6 -Encoding utf8
"`n--- [a] SOLUTION AGACI (proje listesi) ---" | Out-File $c6 -Append -Encoding utf8
dotnet sln TrainService.sln list 2>&1 | Out-File $c6 -Append -Encoding utf8
"`n--- [b] MIGRATION LISTESI (Pending olmamali) ---" | Out-File $c6 -Append -Encoding utf8
dotnet build
dotnet ef migrations list --project src\TrainService.Data --no-build 2>&1 | Out-File $c6 -Append -Encoding utf8
"`n--- [c] DOLGU TARAMASI (SIFIR olmali) ---" | Out-File $c6 -Append -Encoding utf8
Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "dummy1","dummy2","Assert\.True\(true\)","Assert\.Equal\(1,\s*1\)","Assert\.Equal\(3," 2>&1 | Out-File $c6 -Append -Encoding utf8
"--- ustte satir yoksa TEMIZ ---" | Out-File $c6 -Append -Encoding utf8
"`n--- [d] KISAYOL TARAMASI (F9=snap, F1=yok olmali) ---" | Out-File $c6 -Append -Encoding utf8
Get-ChildItem -Recurse src\TrainService.App -Include *.xaml,*.cs | Select-String -Pattern "Key=.F9|Key\.F9|Key=.F1|Key\.F1|ToggleSnap|SetTool" 2>&1 | Out-File $c6 -Append -Encoding utf8
"`n--- [e] ARTER IHLAL TARAMASI (JSON kayit, yasak bagimlilik) ---" | Out-File $c6 -Append -Encoding utf8
Get-ChildItem -Recurse src,tests -Include *.cs | Select-String -Pattern "CadProjectEntity","JsonSerializer","TypeNameHandling","Point2D" 2>&1 | Out-File $c6 -Append -Encoding utf8
"--- ustte JSON/Point2D satiri VARSA arter ihlali ---" | Out-File $c6 -Append -Encoding utf8
"`n--- [f] TEST SAYISI (attribute bazli, proje proje) ---" | Out-File $c6 -Append -Encoding utf8
Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "\[Fact\]|\[Theory\]" | Group-Object { ($_.Path -split '\\tests\\')[1].Split('\')[0] } | ForEach-Object { "$($_.Name): $($_.Count) test-attribute" } 2>&1 | Out-File $c6 -Append -Encoding utf8
Write-Host "OK: $c6"

# D7 — TAM KOSUM + BEKCI ISPATI
$c7 = "$desk\D7_kosum.txt"
"########## TAM KOSUM + BEKCI ISPATI - $ts ##########" | Out-File $c7 -Encoding utf8
"`n--- [a] BEKCI ISPATI: gecerli sahte test EKLI iken T011 (KIRMIZI beklenir) ---" | Out-File $c7 -Append -Encoding utf8
@"
namespace TrainService.Data.Tests;
public class ZZ_DenetimKanit { [Xunit.Fact] public void Sahte() => Xunit.Assert.True(true); }
"@ | Out-File "tests\TrainService.Data.Tests\ZZ_DenetimKanit.cs" -Encoding utf8
dotnet test tests\TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo 2>&1 | Out-File $c7 -Append -Encoding utf8
Remove-Item "tests\TrainService.Data.Tests\ZZ_DenetimKanit.cs" -Force
"--- sahte silindi ---" | Out-File $c7 -Append -Encoding utf8
"`n--- [b] TAM KOSUM (Release, ozet + test adlari) ---" | Out-File $c7 -Append -Encoding utf8
dotnet test TrainService.sln -c Release --logger "console;verbosity=detailed" --nologo 2>&1 | Out-File $c7 -Append -Encoding utf8
Write-Host "OK: $c7"

Write-Host "`nBITTI. 7 dosya Masaustunde Denetim_Dokumu_v3019 klasorunde. Denetci once D6+D7, sonra D3, sonra D1 isteyecek."
