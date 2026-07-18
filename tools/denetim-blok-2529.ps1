$enc="utf8"
$dir = Join-Path ([Environment]::GetFolderPath("Desktop")) "TrainService_Raporlar\BLOK_2529"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
function Dok($ad, $s){ $p=Join-Path $dir $ad; & $s 2>&1 | Out-File $p -Encoding $enc; Write-Host "OK: $p" }

# D1 — YENİ ARAÇLAR + UI (ham kaynak, tam dosyalar)
Dok "D1_araclar.txt" {
  $files = @(
    "src/TrainService.Cad/Tools/HybridTool.cs",
    "src/TrainService.Cad/Tools/RampTool.cs",
    "src/TrainService.Cad/Tools/SwitchTool.cs",
    "src/TrainService.Cad/Tools/ITool.cs",
    "src/TrainService.Core/Topology/TrackGraph.cs",
    "src/TrainService.Core/Entities/DomainEntities.cs",
    "src/TrainService.Cad/RampDefaults.cs",
    "src/TrainService.Cad/SwitchDefaults.cs",
    "src/TrainService.Cad/FeatureTree/FeatureTreeItem.cs",
    "src/TrainService.Cad/FeatureTree/FeatureTreeViewModel.cs",
    "src/TrainService.Cad/CadDocument.FeatureTree.cs",
    "src/TrainService.App/Controls/FeatureTree/FeatureTreeControl.xaml",
    "src/TrainService.App/Controls/FeatureTree/FeatureTreeControl.xaml.cs",
    "src/TrainService.App/Controls/RadialMenu/RadialMenuItem.cs",
    "src/TrainService.App/Controls/RadialMenu/RadialMenuControl.xaml",
    "src/TrainService.App/Controls/RadialMenu/RadialMenuControl.xaml.cs",
    "src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs",
    "src/TrainService.App/Views/Pages/EditorView.xaml",
    "src/TrainService.App/Views/Pages/EditorView.xaml.cs"
  )
  foreach($f in $files){
    $fullPath = Join-Path "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService" $f
    if(Test-Path $fullPath){
      "===== FILE: $f ====="; Get-Content $fullPath
    } else {
      "===== FILE: $f (NOT FOUND) ====="
    }
  }
}

# D2 — TEST GÖVDELERİ (HAM dosyalar)
Dok "D2_govdeler.txt" {
  $testFiles = @(
    "tests/TrainService.Cad.Tests/Tools/T260_HybridToolTests.cs",
    "tests/TrainService.Cad.Tests/Tools/T270_SwitchToolTests.cs",
    "tests/TrainService.Cad.Tests/Tools/T280_RampToolTests.cs",
    "tests/TrainService.Cad.Tests/Tools/T280_SwitchToolTests.cs",
    "tests/TrainService.Cad.Tests/T310_FeatureTreeTests.cs",
    "tests/TrainService.App.Tests/T320_RadialMenuTests.cs"
  )
  foreach($f in $testFiles){
    $fullPath = Join-Path "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService" $f
    if(Test-Path $fullPath){
      "===== FILE: $f ====="; Get-Content $fullPath
    } else {
      "===== FILE: $f (NOT FOUND) ====="
    }
  }
  # Data tests for persistence
  "===== DATA TESTS ====="
  Get-ChildItem "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Data.Tests" -Include *.cs -Recurse -File |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object { "===== FILE: $($_.Name) ====="; Get-Content $_.FullName }
}

# D3 — PERSISTENCE (en kritik borç: Ramp/RailSwitch save/load)
Dok "D3_persistence.txt" {
  "===== CadDocumentStore.cs (TAM) ====="
  $storePath = "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\src\TrainService.App\Services\CadDocumentStore.cs"
  if(Test-Path $storePath){ Get-Content $storePath } else { "NOT FOUND" }
  "===== MIGRATION LIST ====="
  dotnet ef migrations list --project "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\src\TrainService.Data"
  "===== Data.Tests dosya listesi ====="
  Get-ChildItem "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Data.Tests" -Include *.cs -Recurse -File |
    Where-Object { $_.FullName -notlike "*\obj\*" } | Select-Object Name
}

# D4 — YAPISAL TARAMALAR
Dok "D4_yapisal.txt" {
  $srcBase = "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService"
  $testsBase = "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests"
  
  "--- [a] DOLGU (0 beklenir) ---"
  Get-ChildItem $testsBase -Recurse -Include *.cs -File | Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "Assert\.True\(true\)","dummy1\s*\+\s*dummy2","Assert\.Equal\(1,\s*1\)"
  
  "--- [b] SabitKatmanlar (11111111 beklenir) ---"
  Get-ChildItem "$srcBase\src" -Recurse -Include *.cs -File | Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "SabitKatmanlar|11111111-0000"
  
  "--- [c] Inline renk taraması (App-CadColors DIŞINDA Color.FromArgb) ---"
  Get-ChildItem "$srcBase\src" -Recurse -Include *.cs -File |
    Where-Object { $_.FullName -notlike "*\obj\*" -and $_.Name -ne "CadColors.cs" } |
    Select-String "Color\.FromArgb"
  
  "--- [d] Hot-path LINQ (render/move icinde .First(/.Where( olmamalı) ---"
  $vpPath = "$srcBase\src\TrainService.App\Controls\CadCanvas\CadViewportControl.cs"
  if(Test-Path $vpPath){ Get-Content $vpPath | Select-String "\.First\(|\.Where\(|\.Select\(" }
  
  "--- [e] F9 + kısayollar ---"
  Get-ChildItem "$srcBase\src\TrainService.App" -Recurse -Include *.xaml,*.cs -File |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "Key=.F9|Key\.F9|ToggleSnap"
  
  "--- [f] Radyal menü Idle kuralı (araç meşgulken açılmamalı) ---"
  Get-ChildItem "$srcBase\src\TrainService.App" -Recurse -Include *.cs -File |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "Radial|Radyal" -Context 2,4 | Select-Object -First 60
}

# D5 — GÜNCEL TAM KOŞUM
Dok "D5_tamkosum.txt" {
  dotnet test "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\TrainService.sln" -c Release --nologo --logger "console;verbosity=normal"
}

# D6 — BEKÇİ İSPATLARI (T010 + T011)
Dok "D6_bekci.txt" {
  $t260Path = "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Cad.Tests\Tools\T260_HybridToolTests.cs"
  $zzPath = "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Cad.Tests\ZZ_D.cs"
  
  "===== T010: bir testin [Fact]'i yorumlanıyor ====="
  (Get-Content $t260Path) -replace '^\s*\[Fact\] public void T260','    //[Fact] public void T260' | Set-Content $t260Path
  dotnet test "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Architecture.Tests" -c Release --filter "T010" --nologo
  
  "===== T010 restore ====="
  (Get-Content $t260Path) -replace '^\s*//\[Fact\] public void T260','    [Fact] public void T260' | Set-Content $t260Path
  dotnet test "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Architecture.Tests" -c Release --filter "T010" --nologo
  
  "===== T011: ZZ sahte test ekleniyor ====="
  "namespace TrainService.Cad.Tests; public class ZZ_D { [Xunit.Fact] public void S() => Xunit.Assert.True(true); }" | Out-File $zzPath -Encoding $enc
  dotnet test "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Architecture.Tests" -c Release --filter "T011" --nologo
  Remove-Item $zzPath -Force
  
  "===== ZZ silindi, T011 yeşil ====="
  dotnet test "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tests\TrainService.Architecture.Tests" -c Release --filter "T011" --nologo
}

# D7 — SÜREÇ: git + sapma
Dok "D7_surec.txt" {
  $git = Get-ChildItem "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter git.exe -EA 0 | Select-Object -First 1 | ForEach-Object FullName
  if(-not $git){ $git = "git" }
  "--- git log son 15 ---"
  & $git -C "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService" log --oneline -15
  "--- sapma.txt (GÜNCEL olmalı) ---"
  Get-Content "C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\tools\sapma.txt"
}

Write-Host "========================================"
Write-Host "TUM RAPORLAR OLUSTURULDU: $dir"
Write-Host "========================================"
