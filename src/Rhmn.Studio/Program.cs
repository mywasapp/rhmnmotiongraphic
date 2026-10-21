using System.Globalization;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;

namespace Rhmn.Studio;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture=CultureInfo.GetCultureInfo("id-ID");
        CultureInfo.DefaultThreadCurrentUICulture=CultureInfo.GetCultureInfo("id-ID");
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException+=(_,e)=>MessageBox.Show(e.Exception.Message,"RHMN • Terjadi masalah",MessageBoxButtons.OK,MessageBoxIcon.Error);
        if(args.Length>0&&args[0]=="--smoke-test")return SmokeTest.Run(args.Length>1?args[1]:Path.Combine(AppContext.BaseDirectory,"smoke-output"));
        try { _=CoreWebView2Environment.GetAvailableBrowserVersionString(); }
        catch(WebView2RuntimeNotFoundException)
        {
            var setup=Path.Combine(AppContext.BaseDirectory,"MicrosoftEdgeWebview2Setup.exe");
            if(File.Exists(setup)&&MessageBox.Show("WebView2 Runtime diperlukan untuk menampilkan animasi. Pasang komponen Microsoft sekarang? Koneksi internet diperlukan.","RHMN • Komponen belum tersedia",MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)
                Process.Start(new ProcessStartInfo(setup){UseShellExecute=true});
            else MessageBox.Show("Pasang Microsoft WebView2 Runtime, lalu buka kembali aplikasi.","RHMN Motion Graphic Studio");
            return 2;
        }
        Application.Run(new MainForm());
        return 0;
    }
}
