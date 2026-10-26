import { useState } from "react";
import { FileImageOutlined, ScissorOutlined, SwapOutlined } from "@ant-design/icons";
import { AppShell, ThemeToggle, type NavItem } from "@/ui-kit";
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
          <ThemeToggle />
        </>
      }
    >
      {activeTab === "convert" && <ImageConvertTab />}
      {activeTab === "crop" && <ImageCropTab />}
      {activeTab === "jd" && <JdDetailsTab />}
    </AppShell>
  );
}

export default App;
