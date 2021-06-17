# Paths
$packFolder = (Get-Item -Path "./" -Verbose).FullName
$rootFolder = Join-Path $packFolder "../"

# List of solutions
$solutions = (
    "MT.MiniIoc"
)

# List of projects
$projects = (
    "MT.MiniIoc/net40",
    "MT.MiniIoc/netcore",
    "MT.MiniIoc/netuap"
)


