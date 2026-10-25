#!/bin/bash
# 图片批量裁切工具 macOS 安装脚本。
# 应用未经过 Apple 公证，从网络下载后会被 Gatekeeper 拦截；
# 本脚本把应用复制到“应用程序”文件夹并移除隔离属性，之后即可正常双击打开。
set -euo pipefail

APP_NAME="图片批量裁切工具.app"
SOURCE_DIR="$(cd "$(dirname "$0")" && pwd)"
SOURCE_APP="$SOURCE_DIR/$APP_NAME"

if [ ! -d "$SOURCE_APP" ]; then
  echo "未找到 $APP_NAME，请确认脚本与应用位于同一文件夹。"
  exit 1
fi

# 优先安装到系统“应用程序”，没有写入权限时退回到当前用户的“应用程序”。
TARGET_DIR="/Applications"
if [ ! -w "$TARGET_DIR" ]; then
  TARGET_DIR="$HOME/Applications"
  mkdir -p "$TARGET_DIR"
fi
TARGET_APP="$TARGET_DIR/$APP_NAME"

# 正在运行的旧版本先退出，避免覆盖失败。
osascript -e 'tell application id "com.byttao.pig-resize" to quit' >/dev/null 2>&1 || true

echo "正在安装到 $TARGET_DIR ..."
rm -rf "$TARGET_APP"
ditto "$SOURCE_APP" "$TARGET_APP"
xattr -dr com.apple.quarantine "$TARGET_APP" 2>/dev/null || true

echo "安装完成，正在打开应用。"
open "$TARGET_APP"
