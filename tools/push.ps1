$ErrorActionPreference="Continue"
$git = (Get-ChildItem -Path "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter "git.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
& $git add .
& $git commit -m "feat(v3.0.20): TrackGraph (Topoloji) ve 5-Surum Denetimi"
& $git push
