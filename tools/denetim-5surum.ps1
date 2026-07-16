$ErrorActionPreference="Continue"
$enc="utf8"
$dir = Join-Path ([Environment]::GetFolderPath("Desktop")) "TrainService_Raporlar\v3.0.20"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$out = Join-Path $dir "VERSIYON_KONTROL_DENETIMI.txt"
"=== v3.0.15-v3.0.19 GERIYE-DONUK DENETIM - $(Get-Date -Format 'yyyy-MM-dd HH:mm') ===" | Out-File $out -Encoding $enc
function B($t,$s){ "`n########## $t ##########"|Out-File $out -Append -Encoding $enc; & $s 2>&1|Out-File $out -Append -Encoding $enc }

B "[A] ARTER BUTUNLUGU: Core bagimsiz mi, Cad WPF'siz mi (mimari bekciler gercek kosum)" {
  dotnet test tests/TrainService.Architecture.Tests -c Release --logger "console;verbosity=detailed" --nologo | Select-String "T00|Basari|Gecti|Passed|Basarisiz|Failed" }

B "[B] v3.0.15 VIEWPORT: ViewportTransform + ZoomAt var mi (dosya + T301-304)" {
  Get-ChildItem -Recurse src/TrainService.App -Include *.cs | Select-String "class ViewportTransform|ZoomAt|WorldToScreen" | Select-Object -First 5
  Get-ChildItem -Recurse tests -Include *.cs | Select-String "T301|T302|T303|T304" }

B "[C] v3.0.16 CAD CORE: CadDocument/CommandStack/SelectionService (mutasyon internal mi)" {
  Get-ChildItem -Recurse src/TrainService.Cad -Include *.cs | Select-String "internal void AddEntity|internal void RemoveEntity|class CommandStack|class SelectionService" }

B "[D] v3.0.17 SNAP v1: GridSnapProvider AwayFromZero mu (ToEven tuzagi)" {
  Get-ChildItem -Recurse src/TrainService.Cad -Include *.cs | Select-String "AwayFromZero|MidpointRounding" }

B "[E] v3.0.18 TRACKTOOL + Ctrl+S: iliskisel kayit mi (CadProjectEntity=0), migration Pending yok" {
  Get-ChildItem -Recurse src,tests -Include *.cs | Select-String "CadProjectEntity"
  "-- ustte satir yoksa JSON-blob YOK (temiz) --"
  dotnet ef migrations list --project src/TrainService.Data 2>&1 | Select-String "Pending|Initial|AddMissing" }

B "[F] v3.0.19 SNAP v2: 3 provider + SpatialHash + DistanceSquaredToSegment guard'li mi" {
  Get-ChildItem -Recurse src/TrainService.Cad -Include *.cs | Select-String "EndpointSnapProvider|OnSegmentSnapProvider|class SpatialHash|DistanceSquaredToSegment" | Select-Object -First 6 }

B "[G] TUM PROJE DOLGU TARAMASI (sadece son surum degil, HEPSI)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "dummy1","dummy2","Assert\.True\(true\)","Assert\.Equal\(3,"
  "-- ustte satir yoksa TUM PROJE TEMIZ --" }

B "[H] TEST SAYILARI (proje proje, geriye gidis kontrolu)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String "\[Fact\]|\[Theory\]" | Group-Object { ($_.Path -split '\\tests\\')[1].Split('\')[0] } | ForEach-Object { "$($_.Name): $($_.Count)" } }

B "[I] TEKNIK BORC ENVANTERI" {
  "1. Fluent Assertions lisansi (ticari kullanim uyarisi) - cozulecek"
  "2. Class1.cs (Simulation/Firmware) - Faz F/H'de anlamli iskelete donusecek"
  "3. Git commit disiplini - v3.0.16+ tek yiginda, ayri commit'lenecek (AGENTS.md 2/6)" }

"`n=== DENETIM SONU ===" | Out-File $out -Append -Encoding $enc
Write-Host "Denetim yazildi: $out"
