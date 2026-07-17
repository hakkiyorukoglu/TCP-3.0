$ErrorActionPreference = "Continue"
$out = Join-Path ([Environment]::GetFolderPath("Desktop")) "RAPOR_MUHUR_v3026.txt"
"=== v3.0.26 MUHUR RAPORU (SwitchTool) - $($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ===" | Out-File $out -Encoding utf8
function B($t,$s){ "
 ########## $t ##########"|Out-File $out -Append -Encoding utf8; & $s 2>&1|Out-File $out -Append -Encoding utf8 }

B "1. DOLGU TARAMASI (beklenen: 0 eslesme)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "Assert\.True\(true\)","Assert\.Equal\(1,\s*1\)","dummy1\s*\+\s*dummy2","Assert\.Equal\(3,"
  "-- ustte satir yoksa TEMIZ --" }

B "2. BEKCI ISPATI: T011 sahte test (KIRMIZI beklenir)" {
  'namespace TrainService.Cad.Tests; public class ZZ_Kanit { [Xunit.Fact] public void Sahte() => Xunit.Assert.True(true); }' | Out-File tests/TrainService.Cad.Tests/ZZ_Kanit.cs -Encoding utf8
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo
  Remove-Item tests/TrainService.Cad.Tests/ZZ_Kanit.cs -Force; "-- sahte silindi --" }

B "3. BEKCI ISPATI: T010 kapsam (YESIL beklenir)" {
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T010" --logger "console;verbosity=normal" --nologo }

B "4. TAM KOSUM (Release)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=normal" --nologo }

B "5. KIMLIKLI TEST GOVDELERI (T270-T279 SwitchTool)" {
  Get-Content tests/TrainService.Cad.Tests/Tools/T270_SwitchToolTests.cs -EA SilentlyContinue }

B "6. SwitchTool KAYNAK KODU + SwitchDefaults" {
  "=== SwitchTool.cs ==="
  Get-Content src/TrainService.Cad/Tools/SwitchTool.cs -EA SilentlyContinue
  "`n=== SwitchDefaults.cs ==="
  Get-Content src/TrainService.Cad/SwitchDefaults.cs -EA SilentlyContinue }

B "7. PreviewSwitchPlace record (ITool.cs)" {
  Get-Content src/TrainService.Cad/Tools/ITool.cs -EA SilentlyContinue | Select-String "PreviewSwitchPlace" -Context 20,0 }

B "8. TEST SAYILARI (proje bazinda)" {
  dotnet test TrainService.sln -c Release --nologo | Select-String "Passed|Failed|Total tests" }

B "9. SAPMA BEYANI (v3.0.26)" {
  Get-Content tools/sapma.txt -EA SilentlyContinue | Select-String "v3.0.26" -Context 0,20 }

"
=== SON ===" | Out-File $out -Append -Encoding utf8
Write-Host "Yazildi: $out"
