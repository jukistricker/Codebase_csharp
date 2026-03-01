# Di chuyển lên 2 cấp để tìm file .env ở gốc dự án
$envFile = Join-Path $PSScriptRoot "../../.env"
if (Test-Path $envFile) {
    Get-Content $envFile | Foreach-Object {
        if ($_ -match "^([^#=]+)=(.*)$") {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim()
            [System.Environment]::SetEnvironmentVariable($name, $value)
        }
    }
}

$SQL_DIR = Join-Path $PSScriptRoot "Rollback"
$files = Get-ChildItem -Path $SQL_DIR -Filter *.sql | Sort-Object Name -Descending

foreach ($file in $files) {
    Write-Host "--- Applying: $($file.Name) ---" -ForegroundColor Cyan
    $env:PGPASSWORD = [System.Environment]::GetEnvironmentVariable("DB_PASSWORD")
    psql -h [System.Environment]::GetEnvironmentVariable("DB_HOST") `
         -p [System.Environment]::GetEnvironmentVariable("DB_PORT") `
         -U [System.Environment]::GetEnvironmentVariable("DB_USER") `
         -d [System.Environment]::GetEnvironmentVariable("DB_NAME") `
         -f $file.FullName
}