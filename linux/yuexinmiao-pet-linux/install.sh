#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"

python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt

chmod +x run.sh
mkdir -p "$HOME/.local/share/applications"
desktop_file="$HOME/.local/share/applications/YueXinMiaoPetLinux.desktop"
project_dir="$(pwd)"
sed "s#__PROJECT_DIR__#$project_dir#g" YueXinMiaoPetLinux.desktop > "$desktop_file"
chmod +x "$desktop_file"

echo "安装完成。"
echo "如需同步 Windows 版 GIF，请执行：python3 sync_assets_from_windows_project.py"
echo "运行：./run.sh"
