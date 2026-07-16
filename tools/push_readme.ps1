$ErrorActionPreference="Continue"
$git = (Get-ChildItem -Path "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter "git.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
& $git add README.md
& $git commit -m "docs(v3.0.20): README.md surum gecmisi guncellendi"
& $git push
