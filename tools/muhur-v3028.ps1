$ErrorActionPreference = "Continue"
$out = Join-Path ([Environment]::GetFolderPath("Desktop")) "RAPOR_MUHUR_v3028.txt"
"=== v3.0.28 MUHUR RAPORU (Feature Tree) - $($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ===" | Out-File $out -Encoding utf8
function B($t,$s){ "
########## $t ##########"|Out-File $out -Append -Encoding utf8; & $s 2>&1|Out-File $out -Append -Encoding utf8 }

B "1. DOLGU TARAMASI (beklenen: 0 eslesme)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "Assert\.True\(true\)","Assert\.Equal\(1,\s*1\)","dummy1\s*\+\s*dummy2","Assert\.Equal\(3,"
  "-- ustte satir yoksa TEMIZ --" }

B "2. BEKCI ISPATI: T011 sahte test (KIRMIZI beklenir)" {
  'namespace TrainService.Cad.Tests; public class ZZ_Kanit { [Xunit.Fact] public void Sahte() => Xunit.Assert.True(true); }' | Out-File tests/TrainService.Cad.Tests/ZZ_Kanit.cs -Encoding utf8
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo
  Remove-Item tests/TrainService.Cad.Tests/ZZ_Kanit.cs -Force; "-- sahte silindi --" }

B "3. BEKCI ISPATI: T010 kapsam (144, YESIL beklenir)" {
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T010" --logger "console;verbosity=normal" --nologo }

B "4. TAM KOSUM (Release)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=normal" --nologo }

B "5. KIMLIKLI TEST GOVDELERI (T310-T319c FeatureTree)" {
  Get-Content tests/TrainService.Cad.Tests/T310_FeatureTreeTests.cs -EA SilentlyContinue }

B "6. FeatureTree KAYNAK KODU" {
  Get-Content src/TrainService.Cad/FeatureTree/FeatureTreeItem.cs -EA SilentlyContinue }

B "7. FeatureTreeViewModel KAYNAK KODU" {
  Get-Content src/TrainService.Cad/FeatureTree/FeatureTreeViewModel.cs -EA SilentlyContinue }

B "8. CadDocument.FeatureTree KAYNAK KODU" {
  Get-Content src/TrainService.Cad/CadDocument.FeatureTree.cs -EA SilentlyContinue }

B "9. FeatureTreeControl XAML" {
  Get-Content src/TrainService.App/Controls/FeatureTree/FeatureTreeControl.xaml -EA SilentlyContinue }

B "10. FeatureTreeControl code-behind" {
  Get-Content src/TrainService.App/Controls/FeatureTree/FeatureTreeControl.xaml.cs -EA SilentlyContinue }

B "11. CadViewportControl.ZoomToEntity" {
  Get-Content src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs -EA SilentlyContinue | Select-String "ZoomToEntity|GetEntityBounds" -Context 5,30 }

B "12. TEST SAYILARI (proje bazinda)" {
  dotnet test TrainService.sln -c Release --nologo | Select-String "Passed|Failed|Total tests" }

B "13. SAPMA BEYANI (v3.0.28)" {
  Get-Content tools/sapma.txt -EA SilentlyContinue | Select-String "v3.0.28" -Context 0,20 }

"
=== SON ===" | Out-File $out -Append -Encoding utf8
Write-Host "Yazildi: $out"
