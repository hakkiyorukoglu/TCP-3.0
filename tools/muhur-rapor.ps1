$ErrorActionPreference = "Continue"
$out = Join-Path ([Environment]::GetFolderPath("Desktop")) "RAPOR_MUHUR_v3018.txt"
"=== v3.0.18 MUHUR RAPORU - $($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ===" | Out-File $out -Encoding utf8
function B($t,$s){ "
########## $t ##########"|Out-File $out -Append -Encoding utf8; & $s 2>&1|Out-File $out -Append -Encoding utf8 }

B "[1] DOLGU ENVANTERI (temizlik ONCESI ne vardi)" { Get-Content tools/dolgu_envanteri.txt -EA SilentlyContinue; Get-Content tools/dolgu_karar.txt -EA SilentlyContinue }

B "[2] BEKCI ISPATI-A: bos sahte test EKLI iken T011 (KIRMIZI beklenir)" {
  'namespace TrainService.Data.Tests; public class ZZ_Kanit { [Xunit.Fact] public void Sahte() => Xunit.Assert.True(true); }' | Out-File tests/TrainService.Data.Tests/ZZ_Kanit.cs -Encoding utf8
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo
  Remove-Item tests/TrainService.Data.Tests/ZZ_Kanit.cs -Force; "-- bos sahte silindi --" }

B "[3] KAYNAK-ICI DOLGU TARAMASI (beklenen: 0 eslesme)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "Assert\.True\(true\)","Assert\.Equal\(1,\s*1\)","Assert\.NotNull\(this\)","Assert\.Equal\(3,\s*dummy1\s*\+\s*dummy2\)"
  "-- ustte satir yoksa TEMIZ --" }

B "[4] TAM KOSUM (Release, test ADLARIYLA, verbosity=detailed)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=detailed" --nologo }

B "[5] KIMLIKLI TEST GOVDELERI (denetci bunlari OKUYACAK - kod-once kurali)" {
  "--- T2xx/T3xx (Core+Cad) ---"; Get-Content tests/TrainService.Core.Tests/T2xx_EntityTests.cs -EA SilentlyContinue
  Get-Content tests/TrainService.Cad.Tests/T3xx_ViewportTests.cs -EA SilentlyContinue
  "--- T5xx (Data artery) ---";  Get-Content tests/TrainService.Data.Tests/T5xx_ArteryTests.cs -EA SilentlyContinue
  "--- T18xx (persistence) ---"; Get-Content tests/TrainService.Data.Tests/T18xx_PersistenceTests.cs -EA SilentlyContinue
  Get-Content tests/TrainService.Cad.Tests/IsDirtyTests.cs -EA SilentlyContinue
  "--- T011 bekci ---";          Get-Content tests/TrainService.Architecture.Tests/T011_*.cs -EA SilentlyContinue }

B "[6] JSON SOKUM + KISAYOL + MIGRATION" {
  "-- CadProjectEntity (0 beklenir) --"; Get-ChildItem -Recurse src,tests -Include *.cs | Select-String "CadProjectEntity"
  "-- F9/F1 (F9=snap, F1=yok) --";       Get-ChildItem -Recurse src/TrainService.App -Include *.xaml,*.cs | Select-String "Key=.F9|Key\.F9|Key=.F1|Key\.F1"
  "-- migrations --";                     dotnet ef migrations list --project src/TrainService.Data --startup-project src/TrainService.App --no-build }

B "[7] TEST SAYILARI (her proje, attribute-bazli degil ham kosum ozeti)" {
  dotnet test TrainService.sln -c Release --nologo | Select-String "Basari|Gecti|Passed|Toplam|Total" }

B "[8] GUID CONVERTER MUTABAKATI (Gemini cevabi)" { Get-Content tools/guid_cevap.txt -EA SilentlyContinue }

B "[9] SAPMA BEYANI" { if(Test-Path tools/sapma.txt){Get-Content tools/sapma.txt}else{"SAPMA YOK"} }

"
=== SON ===" | Out-File $out -Append -Encoding utf8
Write-Host "Yazildi: $out"
