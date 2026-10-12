import { useState } from 'react';
import { open } from '@tauri-apps/plugin-dialog';
import { batchResize } from '../api';
import { useLogStore } from '../store';
import type { ResizeStyle, ImageFile } from '../types';
import { LogPanel } from './LogPanel';

export function ResizeTab() {
  const [files, setFiles] = useState<ImageFile[]>([]);
  const [style, setStyle] = useState<ResizeStyle>('Square1440');
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
      const newFiles = selected.map((path) => ({
        path,
        name: path.split(/[/\\]/).pop() || path,
      }));
      setFiles((prev) => [...prev, ...newFiles]);
      addLog(`添加 ${newFiles.length} 张图片`, 'info');
    }
  };

  const handleConvert = async () => {
    if (files.length === 0) {
      addLog('请先选择图片', 'warning');
      return;
    }

    setProcessing(true);
    addLog(`开始转换 ${files.length} 张图片...`, 'processing');

    try {
      const result = await batchResize(
        files.map((f) => f.path),
        style
      );

      if (result.success) {
        addLog(result.message, 'success');
        setFiles([]);
      } else {
        addLog(result.message, 'warning');
        setFiles((prev) =>
          prev.filter((f) => result.failed_files.includes(f.path))
        );
      }
    } catch (error) {
      addLog(`转换失败: ${error}`, 'error');
    } finally {
      setProcessing(false);
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
            <label>转换样式</label>
            <div className="radio-group">
              <label>
                <input
                  type="radio"
                  checked={style === 'Square1440'}
                  onChange={() => setStyle('Square1440')}
                />
                1440 方图
              </label>
              <label>
                <input
                  type="radio"
                  checked={style === 'Long1440'}
                  onChange={() => setStyle('Long1440')}
                />
                1440 长图
              </label>
              <label>
                <input
                  type="radio"
                  checked={style === 'Square800'}
                  onChange={() => setStyle('Square800')}
                />
                800 方图
              </label>
              <label>
                <input
                  type="radio"
                  checked={style === 'ProductLong'}
                  onChange={() => setStyle('ProductLong')}
                />
                商品长图
              </label>
            </div>
          </div>

          <div className="button-group">
            <button onClick={handleSelectFiles} disabled={processing}>
              选择图片
            </button>
            <button
              onClick={handleConvert}
              disabled={processing || files.length === 0}
              className="btn-primary"
            >
              {processing ? '转换中...' : '开始转换'}
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
          <div className="file-list-content">
            {files.map((file, index) => (
              <div key={index} className="file-item">
                {file.name}
              </div>
            ))}
          </div>
        </div>
      </div>

      <LogPanel />
    </div>
  );
}
