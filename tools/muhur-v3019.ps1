$ErrorActionPreference = "Continue"
$out = Join-Path ([Environment]::GetFolderPath("Desktop")) "RAPOR_v3019.txt"
"=== v3.0.19 MUHUR RAPORU (SnapEngine v2 & SpatialHash) - $($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ===" | Out-File $out -Encoding utf8
function B($t,$s){ "
########## $t ##########"|Out-File $out -Append -Encoding utf8; & $s 2>&1|Out-File $out -Append -Encoding utf8 }

B "[1] TAM KOSUM (Release, test ADLARIYLA, verbosity=detailed)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=detailed" --nologo }

B "[2] VECTOR2D MATH & TESTLERI (T210-T213)" {
  "--- Vector2DMath.cs ---"; Get-Content src/TrainService.Core/Geometry/Vector2DMath.cs -EA SilentlyContinue
  "--- T2xx_Vector2DMathTests.cs ---"; Get-Content tests/TrainService.Core.Tests/T2xx_Vector2DMathTests.cs -EA SilentlyContinue }

B "[3] SPATIAL HASH & SNAP ENGINE KODLARI" {
  "--- SpatialHash.cs ---"; Get-Content src/TrainService.Cad/Spatial/SpatialHash.cs -EA SilentlyContinue
  "--- SnapEngine.cs ---"; Get-Content src/TrainService.Cad/Snapping/SnapEngine.cs -EA SilentlyContinue
  "--- EndpointSnapProvider.cs ---"; Get-Content src/TrainService.Cad/Snapping/EndpointSnapProvider.cs -EA SilentlyContinue
  "--- OnSegmentSnapProvider.cs ---"; Get-Content src/TrainService.Cad/Snapping/OnSegmentSnapProvider.cs -EA SilentlyContinue }

B "[4] SPATIAL HASH & SNAP TESTLERI (T305-T311)" {
  "--- T3xx_SpatialHashTests.cs ---"; Get-Content tests/TrainService.Cad.Tests/T3xx_SpatialHashTests.cs -EA SilentlyContinue
  "--- T3xx_SnapEngineTests.cs ---"; Get-Content tests/TrainService.Cad.Tests/T3xx_SnapEngineTests.cs -EA SilentlyContinue
  "--- T311_SnapEngineDITests.cs ---"; Get-Content tests/TrainService.App.Tests/T311_SnapEngineDITests.cs -EA SilentlyContinue }

B "[5] VIEWPORT CONTROL (Snap İkonları)" {
  Get-Content src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs -EA SilentlyContinue }

B "[6] SAPMA BEYANI" { if(Test-Path tools/sapma.txt){Get-Content tools/sapma.txt}else{"SAPMA YOK"} }

"
=== SON ===" | Out-File $out -Append -Encoding utf8
Write-Host "Yazildi: $out"
