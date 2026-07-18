$ErrorActionPreference = "Continue"
$out = Join-Path ([Environment]::GetFolderPath("Desktop")) "RAPOR_MUHUR_v3029.txt"
"=== v3.0.29 MUHUR RAPORU (Radyal Menu) - $($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ===" | Out-File $out -Encoding utf8
function B($t,$s){ "
########## $t ##########"|Out-File $out -Append -Encoding utf8; & $s 2>&1|Out-File $out -Append -Encoding utf8 }

B "1. DOLGU TARAMASI (beklenen: 0 eslesme)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "Assert\.True\(true\)","Assert\.Equal\(1,\s*1\)","dummy1\s*\+\s*dummy2","Assert\.Equal\(3,"
  "-- ustte satir yoksa TEMIZ --" }

B "2. BEKCI ISPATI: T011 sahte test (KIRMIZI beklenir)" {
  'namespace TrainService.Cad.Tests; public class ZZ_Kanit { [Xunit.Fact] public void Sahte() => Xunit.Assert.True(true); }' | Out-File tests/TrainService.Cad.Tests/ZZ_Kanit.cs -Encoding utf8
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo
  Remove-Item tests/TrainService.Cad.Tests/ZZ_Kanit.cs -Force; "-- sahte silindi --" }

B "3. BEKCI ISPATI: T010 kapsam (Cad.Tests 144, App.Tests 10, YESIL beklenir)" {
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T010" --logger "console;verbosity=normal" --nologo }

B "4. TAM KOSUM (Release)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=normal" --nologo }

B "5. KIMLIKLI TEST GOVDELERI (T320-T329 RadialMenu)" {
  Get-Content tests/TrainService.App.Tests/T320_RadialMenuTests.cs -EA SilentlyContinue }

B "6. RadialMenuItem KAYNAK KODU" {
  Get-Content src/TrainService.App/Controls/RadialMenu/RadialMenuItem.cs -EA SilentlyContinue }

B "7. RadialMenuControl XAML" {
  Get-Content src/TrainService.App/Controls/RadialMenu/RadialMenuControl.xaml -EA SilentlyContinue }

B "8. RadialMenuControl code-behind" {
  Get-Content src/TrainService.App/Controls/RadialMenu/RadialMenuControl.xaml.cs -EA SilentlyContinue }

B "9. CadViewportControl.BuildRadialMenuItems" {
  Get-Content src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs -EA SilentlyContinue | Select-String "BuildRadialMenuItems" -Context 0,90 }

B "10. CadViewportControl.OnMouseDown (sag tik)" {
  Get-Content src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs -EA SilentlyContinue | Select-String "RightButton" -Context 3,8 }

B "11. EditorView.CommandStack baglantisi" {
  Get-Content src/TrainService.App/Views/Pages/EditorView.xaml.cs -EA SilentlyContinue | Select-String "CommandStack" -Context 2,3 }

B "12. TEST SAYILARI (proje bazinda)" {
  dotnet test TrainService.sln -c Release --nologo | Select-String "Passed|Failed|Total tests" }

B "13. SAPMA BEYANI (v3.0.29)" {
  Get-Content tools/sapma.txt -EA SilentlyContinue | Select-String "v3.0.29" -Context 0,20 }

"
=== SON ===" | Out-File $out -Append -Encoding utf8
Write-Host "Yazildi: $out"
