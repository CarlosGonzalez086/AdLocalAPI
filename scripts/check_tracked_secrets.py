"""Reject credential literals in tracked configuration without printing values."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SECRET_KEY = re.compile(r"^(ApiKey|AccessKeyId|SecretAccessKey|SecretKey|WebhookSecret|Password)$", re.I)
TOKEN = re.compile(r"(?:sk_(?:live|test)_[A-Za-z0-9]{16,}|whsec_[A-Za-z0-9]{16,}|SG\.[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{16,})")


def config_issues(value, prefix=""):
    if not isinstance(value, dict):
        return []
    issues = []
    for key, child in value.items():
        field = f"{prefix}{key}"
        sensitive = (field.startswith("ConnectionStrings:") or SECRET_KEY.fullmatch(key)
                     or field.lower() in {"jwt:key", "email:key", "supabase:key"})
        if sensitive and isinstance(child, str) and child.strip():
            issues.append(field)
        else:
            issues.extend(config_issues(child, f"{field}:"))
    return issues


def main():
    paths = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT).decode().split("\0")
    failures = []
    for relative in filter(None, paths):
        path = ROOT / relative
        if not path.is_file():
            continue
        if path.name in {"appsettings.Local.json", "secrets-migration.private.json"} or path.name.startswith(".env") and path.name != ".env.example":
            failures.append(f"{relative}: archivo privado rastreado")
        if path.suffix.lower() not in {".json", ".cs", ".yml", ".yaml", ".md", ".ps1", ".sh"} and path.name != "Dockerfile":
            continue
        content = path.read_text(encoding="utf-8-sig", errors="replace")
        if path.name.startswith("appsettings") and path.suffix == ".json":
            for field in config_issues(json.loads(content)):
                failures.append(f"{relative}: configuración sensible no vacía ({field})")
        if "AdLocalAPI.Tests/" not in relative:
            for number, line in enumerate(content.splitlines(), 1):
                if TOKEN.search(line):
                    failures.append(f"{relative}:{number}: patrón de credencial")
                if path.name == "Dockerfile" and re.match(r"\s*(?:ENV|ARG)\s+(?:JWT__Key|SUPABASE__KEY|DB_CONNECTION|SUPABASE_DB_CONNECTION|STRIPE_WEBHOOK_SECRET)\s*=\s*\S", line, re.I):
                    failures.append(f"{relative}:{number}: credencial embebida en imagen")
    if failures:
        print("\n".join(failures))
        return 1
    print("Sin literales de credenciales detectados por este control en archivos rastreados.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
