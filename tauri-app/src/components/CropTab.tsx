import { useState, useEffect } from 'react';
import { open } from '@tauri-apps/plugin-dialog';
import { batchCrop, detectImageCropMode } from '../api';
import { useLogStore } from '../store';
import type { CropMode, CropFileItem } from '../types';
import { LogPanel } from './LogPanel';

const modeLabels: Record<CropMode, string> = {
  auto: '自动识别',
  randomcorners: '四角乱切',
  grid2: '四宫格',
  grid3: '九宫格',
};

export function CropTab() {
  const [files, setFiles] = useState<CropFileItem[]>([]);
  const [globalMode, setGlobalMode] = useState<CropMode>('auto');
  const [quickMode, setQuickMode] = useState(false);
  const [useCustomOutput, setUseCustomOutput] = useState(false);
  const [customOutputDir, setCustomOutputDir] = useState('');
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
      const newFiles: CropFileItem[] = [];

      for (const path of selected) {
        const name = path.split(/[/\\]/).pop() || path;
        let mode = globalMode;

        if (globalMode === 'auto') {
          try {
            const result = await detectImageCropMode(path);
            mode = result.mode;
          } catch {
            mode = 'randomcorners';
          }
        }

        newFiles.push({ path, name, mode });
      }

      setFiles((prev) => [...prev, ...newFiles]);
      addLog(`添加 ${newFiles.length} 张图片`, 'info');

      if (quickMode && newFiles.length > 0) {
        await processCrop(newFiles);
      }
    }
  };

  const handleSelectOutputDir = async () => {
    const selected = await open({
      directory: true,
    });

    if (selected && typeof selected === 'string') {
      setCustomOutputDir(selected);
    }
  };

  const processCrop = async (filesToProcess: CropFileItem[]) => {
    setProcessing(true);
    addLog(`开始裁切 ${filesToProcess.length} 张图片...`, 'processing');

    try {
      const jobs: Array<[string, CropMode]> = filesToProcess.map((f) => [
        f.path,
        f.mode,
      ]);

      const result = await batchCrop(
        jobs,
        useCustomOutput ? customOutputDir : undefined
      );

      if (result.success) {
        addLog(result.message, 'success');
        setFiles((prev) =>
          prev.filter((f) => !filesToProcess.some((fp) => fp.path === f.path))
        );
      } else {
        addLog(result.message, 'warning');
        setFiles((prev) =>
          prev.filter((f) => result.failed_files.includes(f.path))
        );
      }
    } catch (error) {
      addLog(`裁切失败: ${error}`, 'error');
    } finally {
      setProcessing(false);
    }
  };

  const handleCrop = async () => {
    if (files.length === 0) {
      addLog('请先选择图片', 'warning');
      return;
    }

    if (useCustomOutput && !customOutputDir) {
      addLog('请选择自定义保存路径', 'warning');
      return;
    }

    await processCrop(files);
  };

  const handleModeChange = (index: number, mode: CropMode) => {
    setFiles((prev) =>
      prev.map((f, i) => (i === index ? { ...f, mode } : f))
    );
  };

  const handleGlobalModeChange = async (mode: CropMode) => {
    setGlobalMode(mode);

    if (mode !== 'auto') {
      setFiles((prev) => prev.map((f) => ({ ...f, mode })));
    } else {
      // 重新检测所有文件
      const updated = await Promise.all(
        files.map(async (f) => {
          try {
            const result = await detectImageCropMode(f.path);
            return { ...f, mode: result.mode };
          } catch {
            return { ...f, mode: 'randomcorners' as CropMode };
          }
        })
      );
      setFiles(updated);
    }
  };

  const handleClear = () => {
    setFiles([]);
    addLog('已清空列表', 'info');
  };

  return (
    <div className="tab-content">
      <div className="main-area">
        <div className="control-panel">
          <div className="control-group">
            <label>裁切模式</label>
            <div className="radio-group">
              <label>
                <input
                  type="radio"
                  checked={globalMode === 'auto'}
                  onChange={() => handleGlobalModeChange('auto')}
                />
                自动识别
              </label>
              <label>
                <input
                  type="radio"
                  checked={globalMode === 'randomcorners'}
                  onChange={() => handleGlobalModeChange('randomcorners')}
                />
                四角乱切
              </label>
              <label>
                <input
                  type="radio"
                  checked={globalMode === 'grid2'}
                  onChange={() => handleGlobalModeChange('grid2')}
                />
                四宫格
              </label>
              <label>
                <input
                  type="radio"
                  checked={globalMode === 'grid3'}
                  onChange={() => handleGlobalModeChange('grid3')}
                />
                九宫格
              </label>
            </div>
          </div>

          <div className="control-group">
            <label>
              <input
                type="checkbox"
                checked={quickMode}
                onChange={(e) => setQuickMode(e.target.checked)}
              />
              快速模式
            </label>
            <label>
              <input
                type="checkbox"
                checked={useCustomOutput}
                onChange={(e) => setUseCustomOutput(e.target.checked)}
              />
              自定义保存路径
            </label>
            {useCustomOutput && (
              <div className="path-selector">
                <input
                  type="text"
                  value={customOutputDir}
                  placeholder="选择保存目录"
                  readOnly
                />
                <button onClick={handleSelectOutputDir}>浏览</button>
              </div>
            )}
          </div>

          <div className="button-group">
            <button onClick={handleSelectFiles} disabled={processing}>
              选择图片
            </button>
            <button
              onClick={handleCrop}
              disabled={processing || files.length === 0}
              className="btn-primary"
            >
              {processing ? '裁切中...' : '开始裁切'}
            </button>
            <button onClick={handleClear} disabled={processing || files.length === 0}>
              清空列表
            </button>
          </div>
        </div>

        <div className="file-list">
          <div className="file-list-header">
            已选择 {files.length} 张图片
          </div>
          <div className="crop-file-list">
            <table>
              <thead>
                <tr>
                  <th>文件名</th>
                  <th>裁切模式</th>
                </tr>
              </thead>
              <tbody>
                {files.map((file, index) => (
                  <tr key={index}>
                    <td>{file.name}</td>
                    <td>
                      <select
                        value={file.mode}
                        onChange={(e) =>
                          handleModeChange(index, e.target.value as CropMode)
                        }
                        disabled={processing}
                      >
                        <option value="auto">自动识别</option>
                        <option value="randomcorners">四角乱切</option>
                        <option value="grid2">四宫格</option>
                        <option value="grid3">九宫格</option>
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <LogPanel />
    </div>
  );
}
