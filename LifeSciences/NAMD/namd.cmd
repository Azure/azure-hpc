@echo off

rem You can update the recipe to 'NAMD-Infiniband-IntelMPI' for MPI jobs
set recipe=NAMD-TCP

set script_dir=%~dp0.
set powershell=C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
set python=python.exe

rem Check for Python in path
cmd /c where %python% > nul 2>&1
if %errorlevel% neq 0 (
    rem Check for Python in PYTHONPATH
    set python=%PYTHONPATH%\python.exe
    cmd /c where %python% > nul 2>&1
    if %errorlevel% neq 0 (
        echo Please install python 3.5+
        exit /b 1
    )
)

cmd /c where pip3.exe > nul 2>&1
if %errorlevel% neq 0 (
    echo Please install pip
    exit /b 1
)

cmd /c where blobxfer.exe > nul 2>&1
if %errorlevel% neq 0 (
    echo Please install blobxfer
    exit /b 1
)

set namd_conf=%1
set namd_args=%2
set pool_args=""

if "%1" == "-poolId" (
    set pool_args="%1 %2"
    set namd_conf=%3
    set namd_args=%4
)

%powershell% -exec bypass -file %script_dir%\namd.ps1 -namdConfFilePath %namd_conf% -namdArgs "%namd_args%" -recipe %recipe% %pool_args%
