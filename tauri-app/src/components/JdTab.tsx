import { useState } from 'react';
import { open } from '@tauri-apps/plugin-dialog';
import { jdExport } from '../api';
import { useLogStore } from '../store';
import type { ImageFile } from '../types';
import { LogPanel } from './LogPanel';

export function JdTab() {
  const [files, setFiles] = useState<ImageFile[]>([]);
  const [selectedIndices, setSelectedIndices] = useState<Set<number>>(new Set());
  const [processing, setProcessing] = useState(false);
  const addLog = useLogStore((state) => state.addLog);

  const handleSelectFiles = async () => {
    const selected = await open({
      multiple: true,
      filters: [
        {
          name: 'Images',
          extensions: ['jpg', 'jpeg', 'png', 'bmp', 'gif'],
        },
      ],
    });

    if (selected && Array.isArray(selected)) {
      const newFiles = selected
        .map((path) => ({
          path,
          name: path.split(/[/\\]/).pop() || path,
        }))
        .sort((a, b) => a.name.localeCompare(b.name));

      setFiles((prev) => [...prev, ...newFiles]);
      addLog(`添加 ${newFiles.length} 张图片`, 'info');
    }
  };

  const handleMoveUp = () => {
    if (selectedIndices.size === 0) return;

    const indices = Array.from(selectedIndices).sort((a, b) => a - b);
    if (indices[0] === 0) return;

    const newFiles = [...files];
    const newSelected = new Set<number>();

    for (const index of indices) {
      [newFiles[index - 1], newFiles[index]] = [newFiles[index], newFiles[index - 1]];
      newSelected.add(index - 1);
    }

    setFiles(newFiles);
    setSelectedIndices(newSelected);
  };

  const handleMoveDown = () => {
    if (selectedIndices.size === 0) return;

    const indices = Array.from(selectedIndices).sort((a, b) => b - a);
    if (indices[0] === files.length - 1) return;

    const newFiles = [...files];
    const newSelected = new Set<number>();

    for (const index of indices) {
      [newFiles[index], newFiles[index + 1]] = [newFiles[index + 1], newFiles[index]];
      newSelected.add(index + 1);
    }

    setFiles(newFiles);
    setSelectedIndices(newSelected);
  };

  const handleDelete = () => {
    if (selectedIndices.size === 0) return;

    const newFiles = files.filter((_, index) => !selectedIndices.has(index));
    setFiles(newFiles);
    setSelectedIndices(new Set());
    addLog(`删除 ${selectedIndices.size} 张图片`, 'info');
  };

  const handleExport = async () => {
    if (files.length === 0) {
      addLog('请先选择图片', 'warning');
      return;
    }

    const firstFileDir = files[0].path.split(/[/\\]/).slice(0, -1).join('/');

    setProcessing(true);
    addLog('开始拼接导出...', 'processing');

    try {
      const result = await jdExport(
        files.map((f) => f.path),
        firstFileDir
      );

      if (result.success) {
        addLog(result.message, 'success');
        setFiles([]);
        setSelectedIndices(new Set());
      } else {
        addLog('导出失败', 'error');
      }
    } catch (error) {
      addLog(`导出失败: ${error}`, 'error');
    } finally {
      setProcessing(false);
    }
  };

  const handleSelect = (index: number, multi: boolean) => {
    if (multi) {
      const newSelected = new Set(selectedIndices);
      if (newSelected.has(index)) {
        newSelected.delete(index);
      } else {
        newSelected.add(index);
      }
      setSelectedIndices(newSelected);
    } else {
      setSelectedIndices(new Set([index]));
    }
  };

  const handleClear = () => {
    setFiles([]);
    setSelectedIndices(new Set());
    addLog('已清空列表', 'info');
  };

  return (
    <div className="tab-content">
      <div className="main-area">
        <div className="control-panel">
          <div className="button-group">
            <button onClick={handleSelectFiles} disabled={processing}>
              选择图片
            </button>
            <button
              onClick={handleMoveUp}
              disabled={processing || selectedIndices.size === 0}
            >
              上移
            </button>
            <button
              onClick={handleMoveDown}
              disabled={processing || selectedIndices.size === 0}
            >
              下移
            </button>
            <button
              onClick={handleDelete}
              disabled={processing || selectedIndices.size === 0}
            >
              删除
            </button>
            <button
              onClick={handleExport}
              disabled={processing || files.length === 0}
              className="btn-primary"
            >
              {processing ? '导出中...' : '导出'}
            </button>
            <button onClick={handleClear} disabled={processing || files.length === 0}>
              清空列表
            </button>
          </div>
        </div>

        <div className="jd-list-container">
          <div className="file-list-header">
            已选择 {files.length} 张图片
          </div>
          <div className="jd-file-list">
            {files.map((file, index) => (
              <div
                key={index}
                className={`jd-file-item ${
                  selectedIndices.has(index) ? 'selected' : ''
                }`}
                onClick={(e) => handleSelect(index, e.ctrlKey || e.metaKey)}
              >
                <span className="file-index">{index + 1}</span>
                <span className="file-name">{file.name}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="jd-preview">
          <div className="jd-preview-header">预览区域</div>
          <div className="jd-preview-content">
            <p>拼接后的长图预览将显示在这里</p>
            <p className="hint">提示：导出宽度固定为 790 像素，每 1500 像素裁切一张</p>
          </div>
        </div>
      </div>

      <LogPanel />
    </div>
  );
}
