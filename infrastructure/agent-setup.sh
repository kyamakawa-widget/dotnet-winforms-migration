#!/usr/bin/env bash
# VPS 上での Agent 初回セットアップ（一度だけ実行）
# 前提: agent/ ディレクトリが $VPS_APP_DIR/agent/ に rsync 済みであること
set -euo pipefail

AGENT_DIR="/home/k_yamakawa/ops/modernization-lab/agent"

echo "==> Python venv セットアップ"
python3 -m venv "$AGENT_DIR/.venv"
"$AGENT_DIR/.venv/bin/pip" install --upgrade pip
"$AGENT_DIR/.venv/bin/pip" install -r "$AGENT_DIR/requirements.txt"

echo "==> .env 配置（GEMINI_API_KEY を設定すること）"
if [ ! -f "$AGENT_DIR/.env" ]; then
  cat > "$AGENT_DIR/.env" <<EOF
DATABASE_URL=postgresql://postgres:p@ssw0rd@localhost:5432/HANBAI
GEMINI_API_KEY=your_api_key_here
EOF
  echo "  [!] $AGENT_DIR/.env を編集して GEMINI_API_KEY を設定してください"
fi

echo "==> systemd unit 作成"
sudo tee /etc/systemd/system/modernization-agent.service > /dev/null <<EOF
[Unit]
Description=Modernization Lab Agent (FastAPI)
After=network.target

[Service]
User=k_yamakawa
WorkingDirectory=$AGENT_DIR
EnvironmentFile=$AGENT_DIR/.env
ExecStart=$AGENT_DIR/.venv/bin/uvicorn main:app --host 0.0.0.0 --port 8001
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable modernization-agent.service
sudo systemctl start modernization-agent.service
echo "==> Agent service started"
systemctl is-active modernization-agent.service
