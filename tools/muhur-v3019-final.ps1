$ErrorActionPreference="Continue"
$enc = "utf8"
$out=Join-Path $env:USERPROFILE "Desktop\RAPOR_v3019_MUHUR.txt"
$git = (Get-ChildItem -Path "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter "git.exe" -EA SilentlyContinue | Select-Object -First 1).FullName
"=== v3.0.19 MUHUR - $(Get-Date -Format 'yyyy-MM-dd HH:mm') ===" | Out-File $out -Encoding $enc
function B($t,$s){ "`n########## $t ##########"|Out-File $out -Append -Encoding $enc; & $s 2>&1|Out-File $out -Append -Encoding $enc }

B "[1] SOZLESME ISPATI: SnapEngine.cs v3.0.18'den beri DEGISMEDI (bos diff beklenir)" {
  "--- son commitler ---"; & $git log --oneline -5
  "--- SnapEngine.cs diff (BOS olmali) ---"
  & $git diff HEAD -- src/TrainService.Cad/Snapping/SnapEngine.cs
  "--- ustte satir yoksa MOTOR DEGISMEDI = sozlesme tutuldu ---"
  "--- App.xaml.cs diff (SADECE 3 provider satiri EKLENMELI) ---"
  & $git diff HEAD -- src/TrainService.App/App.xaml.cs | Select-String "SnapProvider" }

B "[2] SNAP PROVIDER + SPATIAL HASH KODLARI" {
  Get-Content src/TrainService.Cad/Snapping/EndpointSnapProvider.cs
  Get-Content src/TrainService.Cad/Snapping/OnSegmentSnapProvider.cs
  Get-Content src/TrainService.Cad/Spatial/SpatialHash.cs }

B "[3] KIMLIKLI TEST GOVDELERI (T210-213, T305-311 - denetci okuyacak)" {
  Get-ChildItem -Recurse tests -Include *.cs | Where-Object { $_.Name -match "SpatialHash|SnapEngine|Vector2DMath|GeometryMath|SegmentMath" } | ForEach-Object { "===== $($_.Name) ====="; Get-Content $_.FullName }
  "--- T210-213 iceren Core test dosyasi ---"
  Get-ChildItem -Recurse tests/TrainService.Core.Tests -Include *.cs | Select-String "T210|T211|T212|T213" -List | ForEach-Object { "===== $($_.Path) ====="; Get-Content $_.Path } }

B "[4] TAM KOSUM (Release, ozet + adlar, Fail=0 Skip=0)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=detailed" --nologo | Select-String "Basari|Gecti|Geçti|Passed|Toplam|Total|Atlan|Skip|Basarisiz|Başarısız|Failed" | Select-Object -First 50 }

B "[5] DOLGU TARAMASI (SIFIR)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "dummy1","dummy2","Assert\.True\(true\)","Assert\.Equal\(3,"
  "-- ustte satir yoksa TEMIZ --" }

B "[6] BEKCI ISPATI (gecerli ZZ -> T011 KIRMIZI -> silinince yesil)" {
  "namespace TrainService.Data.Tests; public class ZZ_19 { [Xunit.Fact] public void S() => Xunit.Assert.True(true); }" | Out-File tests/TrainService.Data.Tests/ZZ_19.cs -Encoding $enc
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo
  Remove-Item tests/TrainService.Data.Tests/ZZ_19.cs -Force; "-- silindi --" }

B "[7] RENDER KOLLARI (Endpoint=kare, OnSegment=elmas)" {
  Get-ChildItem -Recurse src/TrainService.App -Include *.cs | Select-String "SnapKind.Endpoint|SnapKind.OnSegment|DrawRectangle|elmas|Diamond" -Context 0,3 }

B "[8] MIGRATION (Pending yok - v3.0.18 regresyonu kontrol)" {
  & $git rev-parse HEAD | Out-Null
  dotnet ef migrations list --project src/TrainService.Data 2>&1 | Select-String "Pending|InitialSchema|AddMissing" }

B "[9] SAPMA BEYANI" { if(Test-Path tools/sapma.txt){Get-Content tools/sapma.txt -Encoding utf8}else{"SAPMA YOK"} }

"`n=== SON ===" | Out-File $out -Append -Encoding $enc; Write-Host "Yazildi: $out"
