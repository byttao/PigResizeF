import { useState, useEffect } from 'react';
import { checkUpdate } from './api';
import { open as openUrl } from '@tauri-apps/plugin-opener';
import { ResizeTab } from './components/ResizeTab';
import { CropTab } from './components/CropTab';
import { JdTab } from './components/JdTab';
import './App.css';

type TabType = 'resize' | 'crop' | 'jd';

function App() {
  const [activeTab, setActiveTab] = useState<TabType>('resize');
  const [updateUrl, setUpdateUrl] = useState<string | null>(null);

  useEffect(() => {
    checkUpdate().then((url) => {
      if (url) {
        setUpdateUrl(url);
      }
    }).catch(() => {
      // 忽略更新检查错误
    });
  }, []);

  const handleOpenUpdate = () => {
    if (updateUrl) {
      openUrl(updateUrl);
    }
  };

  return (
    <div className="app-container">
      <div className="app-header">
        <h1>图片批量裁切工具</h1>
        {updateUrl && (
          <button className="update-button" onClick={handleOpenUpdate}>
            发现新版本
          </button>
        )}
      </div>

      <div className="tab-bar">
        <button
          className={`tab-button ${activeTab === 'resize' ? 'active' : ''}`}
          onClick={() => setActiveTab('resize')}
        >
          图片转换
        </button>
        <button
          className={`tab-button ${activeTab === 'crop' ? 'active' : ''}`}
          onClick={() => setActiveTab('crop')}
        >
          图片裁切
        </button>
        <button
          className={`tab-button ${activeTab === 'jd' ? 'active' : ''}`}
          onClick={() => setActiveTab('jd')}
        >
          京东详情页裁切
        </button>
      </div>

      <div className="tab-content-container">
        {activeTab === 'resize' && <ResizeTab />}
        {activeTab === 'crop' && <CropTab />}
        {activeTab === 'jd' && <JdTab />}
      </div>
    </div>
  );
}

export default App;
