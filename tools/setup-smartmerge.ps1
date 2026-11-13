# Налаштовує Unity Smart Merge для цього репозиторію (Windows).
# Запускати один раз після клонування, з кореня репозиторію:
#   powershell -ExecutionPolicy Bypass -File tools/setup-smartmerge.ps1

$hubEditors = "C:\Program Files\Unity\Hub\Editor"

# Шукаємо встановлений Unity 6000.3 (беремо найновіший патч)
$editor = Get-ChildItem $hubEditors -Directory -Filter "6000.3*" -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending | Select-Object -First 1

if (-not $editor) {
    Write-Host "Unity 6000.3 не знайдено в $hubEditors. Перевір, куди Unity Hub ставить редактори."
    exit 1
}

$tool = Join-Path $editor.FullName "Editor\Data\Tools\UnityYAMLMerge.exe"
if (-not (Test-Path $tool)) {
    Write-Host "Не знайдено $tool"
    exit 1
}

# Git любить прямі слеші в шляхах
$toolPath = $tool -replace '\\', '/'

git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'$toolPath' merge -h -p --force %O %B %A %A"
git config merge.unityyamlmerge.recursive binary

Write-Host "Smart Merge налаштовано: $toolPath"
