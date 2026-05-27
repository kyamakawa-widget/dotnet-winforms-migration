#!/usr/bin/env bash
set -euo pipefail

VPS="widget-vps"
VPS_APP_DIR="/home/k_yamakawa/ops/modernization-lab"
PUBLISH_DIR="./publish/api"

echo "==> [1/4] .NET publish"
dotnet publish src/Api/CloudNativeApp.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$PUBLISH_DIR"

echo "==> [2/4] React build"
(cd src/Web && npm ci && npm run build)

# -e オプションで ClearAllForwardings=yes を指定
# ~/.ssh/config の LocalForward が既存セッションで使用中でも競合しない
echo "==> [3/4] rsync API"
rsync -az --delete \
  -e "ssh -o ClearAllForwardings=yes" \
  --exclude='appsettings*.json' \
  "$PUBLISH_DIR/" \
  "$VPS:$VPS_APP_DIR/"

echo "==> [4/4] rsync frontend"
rsync -az --delete \
  -e "ssh -o ClearAllForwardings=yes" \
  src/Web/dist/ \
  "$VPS:$VPS_APP_DIR/wwwroot/"

echo "==> done"
echo ""
echo "=========================================="
echo " [5/5] サービス再起動は VPS 上で手動実行:"
echo "   sudo systemctl restart modernization-lab.service"
echo "   systemctl is-active modernization-lab.service"
echo "=========================================="
