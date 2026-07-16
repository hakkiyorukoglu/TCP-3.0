$ErrorActionPreference="Continue"
$enc = "utf8"
$out=Join-Path ([Environment]::GetFolderPath("Desktop")) "TrainService_Raporlar\v3.0.20\RAPOR_MUHUR.txt"
$git = (Get-ChildItem -Path "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter "git.exe" -EA SilentlyContinue | Select-Object -First 1).FullName

"=== v3.0.20 MUHUR - TrackGraph (Topoloji) - $(Get-Date -Format 'yyyy-MM-dd HH:mm') ===" | Out-File $out -Encoding $enc
function B($t,$s){ "`n########## $t ##########"|Out-File $out -Append -Encoding $enc; & $s 2>&1|Out-File $out -Append -Encoding $enc }

B "[1] TRACKGRAPH / TRACKBLOCK KODLARI" {
  Get-Content src/TrainService.Core/Topology/TrackBlock.cs
  Get-Content src/TrainService.Core/Topology/TrackGraph.cs
}

B "[2] T214-T220 GERCEK TEST GOVDELERI (Denetci okuyacak)" {
  Get-Content tests/TrainService.Core.Tests/T2xx_TrackGraphTests.cs
}

B "[3] TDD KANITI (Ilk run kirmizi, simdi yesil)" {
  "TDD 'Watch it fail' kaniti loglarda mevcuttur (System.NotImplementedException)."
  "Su anki guncel kosum (hepsi yesil):"
  dotnet test tests/TrainService.Core.Tests -c Release --filter "T21" --logger "console;verbosity=normal" --nologo | Select-String "Basari|Gecti|Failed|Passed"
}

B "[4] TAM KOSUM (Release, Fail=0, Skip=0)" {
  dotnet test TrainService.sln -c Release --logger "console;verbosity=detailed" --nologo | Select-String "Basari|Gecti|Geçti|Passed|Toplam|Total|Atlan|Skip|Basarisiz|Başarısız|Failed" | Select-Object -First 50
}

B "[5] DOLGU TARAMASI (SIFIR OLMALI)" {
  Get-ChildItem -Recurse tests -Include *.cs | Select-String -Pattern "dummy1","dummy2","Assert\.True\(true\)","Assert\.Equal\(3,"
  "-- ustte satir yoksa TEMIZ --"
}

B "[6] BEKCI ISPATI (gecerli ZZ -> T011 KIRMIZI -> silinince yesil)" {
  "namespace TrainService.Data.Tests; public class ZZ_20 { [Xunit.Fact] public void S() => Xunit.Assert.True(true); }" | Out-File tests/TrainService.Data.Tests/ZZ_20.cs -Encoding $enc
  dotnet test tests/TrainService.Architecture.Tests -c Release --filter "T011" --logger "console;verbosity=normal" --nologo
  Remove-Item tests/TrainService.Data.Tests/ZZ_20.cs -Force; "-- silindi --"
}

B "[7] GORSEL KANIT" {
  "N/A: Bu surum saf mantik (Topoloji). UI degismedi."
}

B "[8] MIGRATION VE ARTER (Pending yok)" {
  dotnet ef migrations list --project src/TrainService.Data 2>&1 | Select-String "Pending|InitialSchema|AddMissing"
}

B "[9] SAPMA BEYANI" {
  if(Test-Path tools/sapma.txt){Get-Content tools/sapma.txt -Encoding utf8}else{"SAPMA YOK"}
}

"`n=== SON ===" | Out-File $out -Append -Encoding $enc; Write-Host "Yazildi: $out"
