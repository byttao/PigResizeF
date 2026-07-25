namespace PigResizeF
{
    internal static class Program
    {
        /// <summary>
        /// 程序入口：初始化 WinForms 运行环境后显示主窗体。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // .NET 8 默认初始化高 DPI 与应用级视觉配置。
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
