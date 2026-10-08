import os
import sys
import shutil
import zipfile
import subprocess

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PUBLISH_DIR = os.path.join(BASE_DIR, "bin", "Publish_FrameworkDependent")
APP_EXE = os.path.join(PUBLISH_DIR, "CustomClipboardManager.exe")
PAYLOAD_ZIP = os.path.join(BASE_DIR, "WebSetup", "Resources", "Payload.zip")
WEBSETUP_PROJ = os.path.join(BASE_DIR, "WebSetup", "Clipboard_WebSetup.csproj")
WEBSETUP_OUTPUT_EXE = os.path.join(BASE_DIR, "WebSetup", "bin", "Release", "net48", "Clipboard_WebSetup.exe")
TARGET_EXES = [
    os.path.join(BASE_DIR, "Clipboard_WebSetup.exe"),
    os.path.join(BASE_DIR, "Clipboard _WebSetup.exe"),
    os.path.join(BASE_DIR, "installer_output", "Clipboard_WebSetup.exe")
]
THUMBPRINT = "2EECE9026EE712802AEBF8BF299827D68054C817"

def run_cmd(cmd, cwd=BASE_DIR):
    print(f"Running: {cmd}")
    env = os.environ.copy()
    dotnet_dir = r"D:\Program Files\dotnet"
    if os.path.exists(dotnet_dir) and dotnet_dir not in env.get("PATH", ""):
        env["PATH"] = dotnet_dir + os.pathsep + env.get("PATH", "")
    res = subprocess.run(cmd, cwd=cwd, shell=True, capture_output=True, text=True, env=env)
    if res.returncode != 0:
        print(f"Error ({res.returncode}):\nSTDOUT: {res.stdout}\nSTDERR: {res.stderr}")
        raise RuntimeError(f"Command failed: {cmd}")
    print(res.stdout)
    return res.stdout

def sign_file(file_path):
    print(f"Signing: {file_path}")
    ps_script = f"""
    $cert = Get-ChildItem Cert:\\CurrentUser\\My | Where-Object {{ $_.Thumbprint -eq '{THUMBPRINT}' }}
    if (-not $cert) {{
        $cert = Get-ChildItem Cert:\\LocalMachine\\My | Where-Object {{ $_.Thumbprint -eq '{THUMBPRINT}' }}
    }}
    if (-not $cert) {{
        $cert = Get-ChildItem Cert:\\CurrentUser\\My | Where-Object {{ $_.Subject -like "*Custom Clipboard Manager*" }} | Select-Object -First 1
    }}
    if ($cert) {{
        $sig = $null
        $servers = @('http://timestamp.digicert.com', 'http://timestamp.sectigo.com')
        foreach ($server in $servers) {{
            try {{
                $sig = Set-AuthenticodeSignature -Certificate $cert -FilePath '{file_path}' -HashAlgorithm SHA256 -TimestampServer $server
                if ($sig.Status -eq 'Valid') {{ break }}
            }} catch {{ }}
        }}
        if (-not $sig -or $sig.Status -ne 'Valid') {{
            $sig = Set-AuthenticodeSignature -Certificate $cert -FilePath '{file_path}' -HashAlgorithm SHA256
        }}
        $sig | Format-Table Status, Path
    }} else {{
        Write-Warning 'Cert not found, skipping signature'
    }}
    """
    tmp_ps = os.path.join(BASE_DIR, "_tmp_sign.ps1")
    with open(tmp_ps, "w", encoding="utf-8") as f:
        f.write(ps_script)
    try:
        run_cmd(f"powershell -ExecutionPolicy Bypass -File \"{tmp_ps}\"")
    finally:
        if os.path.exists(tmp_ps):
            os.remove(tmp_ps)

APP_PROJ = os.path.join(BASE_DIR, "CustomClipboardManager.csproj")

def main():
    print("=== Step 0: Publish CustomClipboardManager ===")
    run_cmd(f"dotnet publish \"{APP_PROJ}\" -c Release -r win-x64 --self-contained false -o \"{PUBLISH_DIR}\"")

    print("=== Step 1: Sign Published App Executable ===")
    sign_file(APP_EXE)

    print("=== Step 2: Repackage Payload.zip ===")
    if os.path.exists(PAYLOAD_ZIP):
        os.remove(PAYLOAD_ZIP)
    os.makedirs(os.path.dirname(PAYLOAD_ZIP), exist_ok=True)
    
    with zipfile.ZipFile(PAYLOAD_ZIP, 'w', zipfile.ZIP_DEFLATED) as zf:
        for root, dirs, files in os.walk(PUBLISH_DIR):
            for file in files:
                full_path = os.path.join(root, file)
                rel_path = os.path.relpath(full_path, PUBLISH_DIR)
                zf.write(full_path, rel_path)
    print(f"Payload.zip created: {os.path.getsize(PAYLOAD_ZIP)} bytes")

    print("=== Step 3: Build WebSetup Installer ===")
    run_cmd(f"dotnet build \"{WEBSETUP_PROJ}\" -c Release")

    print("=== Step 4: Sign Installer Executable ===")
    sign_file(WEBSETUP_OUTPUT_EXE)

    print("=== Step 5: Distribute to Target Locations ===")
    for target in TARGET_EXES:
        os.makedirs(os.path.dirname(target), exist_ok=True)
        shutil.copy2(WEBSETUP_OUTPUT_EXE, target)
        print(f"Copied to: {target}")

    print("=== Step 6: Verify Signatures ===")
    ps_verify = f"""
    Get-AuthenticodeSignature '{TARGET_EXES[0]}' | Format-List Status, StatusMessage, SignerCertificate
    """
    tmp_verify = os.path.join(BASE_DIR, "_tmp_verify.ps1")
    with open(tmp_verify, "w", encoding="utf-8") as f:
        f.write(ps_verify)
    try:
        run_cmd(f"powershell -ExecutionPolicy Bypass -File \"{tmp_verify}\"")
    finally:
        if os.path.exists(tmp_verify):
            os.remove(tmp_verify)

    print("=== ALL BUILD & PACKAGING COMPLETED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
