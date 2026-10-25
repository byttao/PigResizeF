#!/bin/bash
# 把 tauri build 生成的 .app 与安装脚本打成发布用 zip。
# 用法：scripts/package-macos.sh <.app 路径> <输出 zip 路径>
set -euo pipefail

APP_PATH="$1"
OUTPUT_ZIP="$2"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

STAGING="$(mktemp -d)"
PACKAGE_DIR="$STAGING/图片批量裁切工具"
mkdir -p "$PACKAGE_DIR"

ditto "$APP_PATH" "$PACKAGE_DIR/$(basename "$APP_PATH")"
cp "$SCRIPT_DIR/macos/安装.command" "$PACKAGE_DIR/"
chmod +x "$PACKAGE_DIR/安装.command"

# 确保 ad-hoc 签名完整，否则 Apple 芯片会直接提示“已损坏”。
codesign --force --deep --sign - "$PACKAGE_DIR/$(basename "$APP_PATH")"

mkdir -p "$(dirname "$OUTPUT_ZIP")"
rm -f "$OUTPUT_ZIP"
ditto -c -k --sequesterRsrc --keepParent "$PACKAGE_DIR" "$OUTPUT_ZIP"
rm -rf "$STAGING"

echo "已生成 $OUTPUT_ZIP"
