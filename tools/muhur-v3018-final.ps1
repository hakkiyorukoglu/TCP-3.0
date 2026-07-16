$ErrorActionPreference="Continue"
$out=Join-Path $env:USERPROFILE "Desktop\RAPOR_v3018_MUHUR.txt"
"=== v3.0.18 SON MUHUR - $(Get-Date -Format 'yyyy-MM-dd HH:mm') ===" | Out-File $out -Encoding utf8
function B($t,$s){ "`n########## $t ##########"|Out-File $out -Append -Encoding utf8; & $s 2>&1|Out-File $out -Append -Encoding utf8 }

B "[1] MIGRATION LISTESI (artik CALISMALI, HICBIRI Pending olmamali)" {
  dotnet build TrainService.sln -c Release --nologo | Select-String "Hata|error|Basari|Build succeeded"
  dotnet ef migrations list --project src/TrainService.Data }

B "[2] AddMissingTables ICERIGI (hangi tablolari ekliyor)" {
  Get-ChildItem src/TrainService.Data/Migrations -Filter "*AddMissingTables*.cs" | ForEach-Object { Get-Content $_.FullName } }

B "[3] DESIGN-TIME FACTORY KODU" { Get-Content src/TrainService.Data/DesignTimeDbContextFactory.cs }

B "[4] DOLGU TARAMASI (SIFIR)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "dummy1","dummy2","Assert\.True\(true\)","Assert\.Equal\(3,"
  "-- ustte satir yoksa TEMIZ --" }

B "[5] BEKCI ISPATI (gecerli ZZ -> T011 KIRMIZI -> silinince yesil)" {
  @"
namespace TrainService.Data.Tests;
public class ZZ_M { [Xunit.Fact] public void S() => Xunit.Assert.True(true); }
"@ | Out-File tests/TrainService.Data.Tests/ZZ_M.cs -Encoding utf8
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo
  Remove-Item tests/TrainService.Data.Tests/ZZ_M.cs -Force; "-- silindi --" }

B "[6] YENI DESIGN-TIME TESTLERI (T541-543) GOVDELERI" {
  Get-Content tests/TrainService.Data.Tests/T5xx_DesignTimeTests.cs }

B "[7] TAM KOSUM (Release, ozet + adlar)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=detailed" --nologo | Select-String "Basari|Gecti|Passed|Toplam|Total|Atlan|Skip|Basarisiz|Failed"|Select-Object -First 40 }

B "[8] F9 KISAYOL TEYIDI (snap olmali, F1 yok)" {
  Get-ChildItem -Recurse src/TrainService.App -Include *.xaml,*.cs | Select-String "Key=.F9|Key\.F9|Key=.F1|Key\.F1" }

B "[9] SAPMA BEYANI" { if(Test-Path tools/sapma.txt){Get-Content tools/sapma.txt}else{"SAPMA YOK"} }

"`n=== SON ===" | Out-File $out -Append -Encoding utf8; Write-Host "Yazildi: $out"
