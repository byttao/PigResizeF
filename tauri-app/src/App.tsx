import { useState } from "react";
import { ImageConvertTab } from "./components/ImageConvertTab";
import { ImageCropTab } from "./components/ImageCropTab";
import { JdDetailsTab } from "./components/JdDetailsTab";
import { ThemeProvider, useTheme } from "./contexts/ThemeContext";

type TabKey = "convert" | "crop" | "jd";

const NAV_ITEMS: { key: TabKey; label: string; icon: string }[] = [
  { key: "convert", label: "图片转换", icon: "⇄" },
  { key: "crop", label: "图片裁切", icon: "✂" },
  { key: "jd", label: "京东详情页", icon: "▤" },
];

function AppContent() {
  const [activeTab, setActiveTab] = useState<TabKey>("convert");
  const { theme, toggleTheme } = useTheme();

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-background text-foreground">
      {/* 固定侧边栏 */}
      <aside
        className="flex w-60 shrink-0 flex-col transition-colors duration-200"
        style={{ backgroundColor: `hsl(var(--sidebar-bg))` }}
      >
        <div
          className="flex h-14 shrink-0 items-center gap-2.5 px-5"
          style={{ borderBottom: `1px solid hsl(var(--border))` }}
        >
          <span className="flex h-7 w-7 items-center justify-center rounded-md bg-orange-500 text-[13px] font-bold text-white shadow-lg shadow-orange-500/20">
            P
          </span>
          <span className="text-[14px] font-semibold tracking-tight">
            PigResize
          </span>
        </div>

        <nav className="flex-1 space-y-1 p-3">
          {NAV_ITEMS.map((item) => {
            const isActive = activeTab === item.key;
            return (
              <button
                key={item.key}
                type="button"
                onClick={() => setActiveTab(item.key)}
                className={`sidebar-item ${isActive ? "sidebar-item-active" : ""}`}
              >
                <span
                  className={`flex h-5 w-5 items-center justify-center text-[13px] transition-all duration-200 ${
                    isActive ? "text-orange-500" : "opacity-60"
                  }`}
                >
                  {item.icon}
                </span>
                {item.label}
              </button>
            );
          })}
        </nav>

        <div
          className="flex shrink-0 items-center justify-between px-5 py-3"
          style={{ borderTop: `1px solid hsl(var(--border))` }}
        >
          <p className="text-[11px]" style={{ color: `hsl(var(--text-tertiary))` }}>
            版本 V1.6.1
          </p>
          <button
            type="button"
            onClick={toggleTheme}
            className="flex h-7 w-7 items-center justify-center rounded-md text-[15px] transition-all duration-200 hover:bg-orange-500/10"
            title={theme === "dark" ? "切换到浅色模式" : "切换到深色模式"}
          >
            {theme === "dark" ? "☀️" : "🌙"}
          </button>
        </div>
      </aside>

      {/* 主内容区 */}
      <main
        className="min-w-0 flex-1 overflow-hidden transition-colors duration-200"
        style={{ backgroundColor: `hsl(var(--main-bg))` }}
      >
        {activeTab === "convert" && <ImageConvertTab />}
        {activeTab === "crop" && <ImageCropTab />}
        {activeTab === "jd" && <JdDetailsTab />}
      </main>
    </div>
  );
}

function App() {
  return (
    <ThemeProvider>
      <AppContent />
    </ThemeProvider>
  );
}

export default App;
