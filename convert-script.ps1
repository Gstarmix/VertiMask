param(
    [Parameter(Position = 0)][string]$Path,
    [string]$Name,
    [switch]$ShowPrompt
)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$PROMPT = @'
Convertis le script ci-dessous en script de TELEPROMPTEUR pour VertiMask,
pense pour etre LU A VOIX HAUTE face camera.
Garde UNIQUEMENT ce que la personne doit DIRE, dans l'ordre.
Mise en forme :
- mets un court titre de section entre [crochets] avant chaque partie (ex: [Accroche]).
  Ces lignes sont des reperes, elles ne sont PAS lues.
- une ligne vide entre les sections. Accents francais corrects. Pas de markdown (**, #, >).
- n'inclus PAS les indications de mise en scene (ACTION, ECRAN, plans, durees, notes).
Style oral (important) :
- PAS de deux-points, ni de guillemets, ni de parentheses, ni de tiret cadratin (le
  tiret long). C'est dur a lire a voix haute. Reformule en phrases naturelles
  (virgule, point, ou tiret simple si besoin).
- les enumerations se disent naturellement (ex: "comme le stuff, l'XP ou la SP"),
  jamais introduites par un deux-points.
- phrases courtes, faciles a enchainer face cam ; coupe celles qui sont trop longues.
- ajoute de petites transitions entre les sections pour que ca coule
  (Concretement, Et c'est pas tout, Bref...).
- amene le nom du site ou l'URL en douceur apres une courte accroche (ne pas
  l'attaquer comme premier mot), et repete-le dans l'appel final.
- ne te limite pas au mot "video" si le contenu peut aussi etre du texte, un post
  ou un thread (X, Reddit...) : dis plutot "contenu".
Rends uniquement le texte final, rien d'autre.
SCRIPT A CONVERTIR :
'@
if ($ShowPrompt) { Write-Output $PROMPT; return }
if (-not $Path -or -not (Test-Path $Path)) {
    Write-Host "Fichier introuvable. Exemple : .\convert-script.ps1 ..\RoleplayOverlay\scripts\ABOEKA_SCRIPT_30s.md" -ForegroundColor Yellow
    return
}
$lines = Get-Content -Path $Path -Encoding UTF8
$result = New-Object System.Collections.Generic.List[string]
$scene = ''
$collecting = $false
$buf = New-Object System.Collections.Generic.List[string]
function Flush {
    if ($buf.Count -gt 0) {
        if ($scene) { $result.Add("[$scene]") }
        $result.Add(($buf -join ' ').Trim())
        $result.Add('')
        $buf.Clear()
    }
}
foreach ($raw in $lines) {
    $t = $raw.Trim()
    if ($t -match '^#{2,}\s*(.+)$') {
        $h = $Matches[1]
        if ($h -match 'SC[EÈ]NE') {
            Flush
            $scene = ($h -replace '^SC[EÈ]NE\s*\d+\s*[·:.\-]*\s*', '' -replace '\(.*?\)', '').Trim()
        }
        continue
    }
    if ($t -match '^\*\*\s*PAROLE') { $collecting = $true; continue }
    if ($collecting) {
        if ($t -eq '' -or $t -match '^\*\*' -or $t -match '^---' -or $t -match '^#') {
            $collecting = $false
            Flush
            continue
        }
        $buf.Add(($t -replace '^>\s*', ''))
    }
}
Flush
if ($result.Count -eq 0) {
    foreach ($raw in $lines) {
        $t = $raw.Trim()
        if ($t -eq '') { $result.Add(''); continue }
        if ($t -match '^#{1,6}\s*(.+)$') { $result.Add('[' + ($Matches[1].Trim()) + ']'); continue }
        if ($t -match '^---') { continue }
        $result.Add(($t -replace '^>\s*', '' -replace '\*\*', ''))
    }
}
if (-not $Name) { $Name = [IO.Path]::GetFileNameWithoutExtension($Path) }
$ro = Join-Path $PSScriptRoot "..\RoleplayOverlay\scripts"
$outDir = if (Test-Path $ro) { (Resolve-Path $ro).Path } else { Join-Path $PSScriptRoot "Scripts" }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$outPath = Join-Path $outDir ($Name + ".txt")
$text = ($result -join "`r`n").Trim() + "`r`n"
[IO.File]::WriteAllText($outPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "OK -> $outPath" -ForegroundColor Green
Write-Host "Il apparait maintenant dans la liste du teleprompteur (rouvre la liste)." -ForegroundColor Gray