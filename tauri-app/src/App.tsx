import { useEffect, useState } from "react";
import { invoke, isTauri } from "@tauri-apps/api/core";
import { App as AntApp } from "antd";
import { FileImageOutlined, ScissorOutlined, SwapOutlined } from "@ant-design/icons";
import { AppShell, ThemeMenu, ThemeToggle, type NavItem } from "@/ui-kit";
import type { UpdateInfo } from "@/types";
import { ImageConvertTab } from "./components/ImageConvertTab";
import { ImageCropTab } from "./components/ImageCropTab";
import { JdDetailsTab } from "./components/JdDetailsTab";
import { version } from "../package.json";

type TabKey = "convert" | "crop" | "jd";

const NAV: NavItem<TabKey>[] = [
  { key: "convert", label: "图片转换", icon: <SwapOutlined />, section: "图片处理" },
  { key: "crop", label: "图片裁切", icon: <ScissorOutlined /> },
  { key: "jd", label: "京东详情页", icon: <FileImageOutlined /> },
];

const PAGES: { key: TabKey; View: (props: { active: boolean }) => React.ReactNode }[] = [
  { key: "convert", View: ImageConvertTab },
  { key: "crop", View: ImageCropTab },
  { key: "jd", View: JdDetailsTab },
];

/** 启动时检查 GitHub 最新 Release，有新版本时询问是否打开发布页；检查失败静默忽略。 */
function useUpdateCheck() {
  const { modal } = AntApp.useApp();

  useEffect(() => {
    if (!isTauri()) return;
    let cancelled = false;

    invoke<UpdateInfo | null>("check_update")
      .then((info) => {
        if (cancelled || !info) return;
        modal.confirm({
          title: "发现新版本",
          content: (
            <>
              <p>发现新版本 {info.latest}。</p>
              <p>当前版本：{info.current}</p>
              <p>是否打开 GitHub Release 页面？</p>
            </>
          ),
          okText: "打开",
          cancelText: "稍后",
          onOk: () => invoke("open_release_page", { url: info.url }).catch(() => {}),
        });
      })
      .catch(() => {});

    // StrictMode 下 effect 会执行两次，只让最后一次弹窗
    return () => {
      cancelled = true;
    };
  }, [modal]);
}

function App() {
  const [activeTab, setActiveTab] = useState<TabKey>("convert");
  useUpdateCheck();

  return (
    <AppShell
      brand={{ mark: "P", name: "PigResize", subtitle: "图片批量处理" }}
      nav={NAV}
      active={activeTab}
      onNavigate={setActiveTab}
      sidebarFooter={
        <>
          <span>版本 V{version}</span>
          <span className="flex items-center gap-1">
            <ThemeMenu />
            <ThemeToggle />
          </span>
        </>
      }
    >
      {/* 三个页签一直挂载、叠放在一起只切换可见性，切走再回来时列表、日志和滚动位置都还在 */}
      <div className="relative h-full">
        {PAGES.map(({ key, View }) => (
          <div
            key={key}
            className={key === activeTab ? "absolute inset-0" : "invisible absolute inset-0"}
            inert={key !== activeTab}
          >
            <View active={key === activeTab} />
          </div>
        ))}
      </div>
    </AppShell>
  );
}

export default App;
