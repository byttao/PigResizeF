#!/bin/bash
# 仅重启本项目的开发进程，绝不触碰 3080 / 3443 等其他端口的服务。
set -u
cd "$(dirname "$0")"

# 精确匹配本项目路径下的 vite / tauri 进程。
pkill -f "PigResizeF/tauri-app/node_modules/.bin/vite" 2>/dev/null
pkill -f "PigResizeF/tauri-app/node_modules/.bin/tauri" 2>/dev/null
pkill -f "target/debug/pig-resize" 2>/dev/null

sleep 1
exec npm run tauri dev
