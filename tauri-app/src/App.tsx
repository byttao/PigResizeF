import { useState } from "react";
import { FileImageOutlined, ScissorOutlined, SwapOutlined } from "@ant-design/icons";
import { AppShell, ThemeMenu, ThemeToggle, type NavItem } from "@/ui-kit";
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

function App() {
  const [activeTab, setActiveTab] = useState<TabKey>("convert");

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
