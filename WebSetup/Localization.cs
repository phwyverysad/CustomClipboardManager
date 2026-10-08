using System;
using System.Collections.Generic;
using System.Globalization;

namespace ClipboardWebSetup
{
    public class LanguageInfo
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }

        public override string ToString() => DisplayName;
    }

    public class LocaleStrings
    {
        public string AppName { get; set; } = "Custom Clipboard Manager";
        public string AppVersion { get; set; } = "v1.0.0";
        public string WindowTitle { get; set; } = "Custom Clipboard Manager Setup";
        public string HeaderTitle { get; set; } = "Custom Clipboard Manager";
        public string HeaderSubtitle { get; set; } = "Clipboard History Manager";
        public string DestinationFolder { get; set; } = "Install Location:";
        public string Browse { get; set; } = "Browse...";
        public string OptionDesktopShortcut { get; set; } = "Create Desktop Shortcut";
        public string OptionStartMenu { get; set; } = "Create Start Menu Shortcut";
        public string OptionAutoStart { get; set; } = "Start automatically with Windows";
        public string OptionInstallService { get; set; } = "Enable Background Service";
        public string OptionLaunchApp { get; set; } = "Launch app after installation";
        public string InstallButton { get; set; } = "Install";
        public string CancelButton { get; set; } = "Cancel";
        public string CloseButton { get; set; } = "Close";
        public string StatusReady { get; set; } = "Ready to install";
        public string StatusCheckingDotnet { get; set; } = "Checking .NET 10 Desktop Runtime...";
        public string StatusDownloadingDotnet { get; set; } = "Downloading .NET 10 Runtime...";
        public string StatusInstallingDotnet { get; set; } = "Installing .NET 10 Runtime...";
        public string StatusCheckingUpdate { get; set; } = "Checking for updates...";
        public string StatusDownloadingPackage { get; set; } = "Downloading application files...";
        public string StatusExtracting { get; set; } = "Extracting application files...";
        public string StatusShortcuts { get; set; } = "Creating shortcuts...";
        public string StatusRegisteringService { get; set; } = "Starting background service...";
        public string StatusComplete { get; set; } = "Installation completed!";
        public string CompleteTitle { get; set; } = "Installation Complete!";
        public string CompleteMessage { get; set; } = "Custom Clipboard Manager is ready to use.";
        public string CompleteHotkeyHint { get; set; } = "💡 Press Ctrl + Shift + V anywhere to access clipboard history";
        public string HotkeyCardTitle { get; set; } = "Hotkey Shortcut:";
        public string HotkeyChangeHint { get; set; } = "Click to record shortcut";
        public string HotkeyRecordingPrompt { get; set; } = "🔴 Press desired key combo...";
        public string HotkeyResetTooltip { get; set; } = "Reset to default (Ctrl + Shift + V)";
        public string HotkeyCustomItem { get; set; } = "Custom...";
        public string LaunchFinishButton { get; set; } = "Launch Application";
        public string ErrorTitle { get; set; } = "Installation Error";
        public string RetryButton { get; set; } = "Retry";
        public string ReinstallButton { get; set; } = "Install / Update";
        public string UninstallButton { get; set; } = "Uninstall";
        public string UninstallConfirmMessage { get; set; } = "Are you sure you want to uninstall Custom Clipboard Manager?";
        public string UninstallingTitle { get; set; } = "Uninstalling...";
        public string UninstallCompleteTitle { get; set; } = "Uninstalled Successfully";
        public string UninstallCompleteMessage { get; set; } = "Custom Clipboard Manager has been removed.";
        public string CancelConfirmMessage { get; set; } = "Installation is in progress. Are you sure you want to cancel?";
    }

    public static class LocalizationManager
    {
        public static List<LanguageInfo> SupportedLanguages { get; } = new List<LanguageInfo>
        {
            new LanguageInfo { Code = "th", DisplayName = "🇹🇭 ภาษาไทย" },
            new LanguageInfo { Code = "en", DisplayName = "🇺🇸 English" },
            new LanguageInfo { Code = "ja", DisplayName = "🇯🇵 日本語" },
            new LanguageInfo { Code = "zh-Hans", DisplayName = "🇨🇳 简体中文" },
            new LanguageInfo { Code = "zh-Hant", DisplayName = "🇹🇼 繁體中文" },
            new LanguageInfo { Code = "de", DisplayName = "🇩🇪 Deutsch" },
            new LanguageInfo { Code = "es", DisplayName = "🇪🇸 Español" },
            new LanguageInfo { Code = "fr", DisplayName = "🇫🇷 Français" },
            new LanguageInfo { Code = "ko", DisplayName = "🇰🇷 한국어" },
            new LanguageInfo { Code = "ru", DisplayName = "🇷🇺 Русский" }
        };

        private static Dictionary<string, LocaleStrings> _locales = new Dictionary<string, LocaleStrings>(StringComparer.OrdinalIgnoreCase)
        {
            ["th"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "ติดตั้ง Custom Clipboard Manager",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "จัดการประวัติคลิปบอร์ด",
                DestinationFolder = "ตำแหน่งติดตั้ง:",
                Browse = "เลือก...",
                OptionDesktopShortcut = "สร้างทางลัดบนเดสก์ท็อป",
                OptionStartMenu = "สร้างทางลัดใน Start Menu",
                OptionAutoStart = "เริ่มทำงานพร้อมเปิดเครื่อง",
                OptionInstallService = "ทำงานเป็นบริการเบื้องหลัง",
                OptionLaunchApp = "เปิดโปรแกรมทันทีหลังติดตั้ง",
                InstallButton = "ติดตั้ง",
                CancelButton = "ยกเลิก",
                CloseButton = "ปิด",
                StatusReady = "พร้อมทำการติดตั้ง",
                StatusCheckingDotnet = "กำลังตรวจสอบ .NET 10 Desktop Runtime...",
                StatusDownloadingDotnet = "กำลังดาวน์โหลด .NET 10 Runtime...",
                StatusInstallingDotnet = "กำลังติดตั้ง .NET 10 Runtime...",
                StatusCheckingUpdate = "กำลังตรวจสอบอัปเดต...",
                StatusDownloadingPackage = "กำลังดาวน์โหลดไฟล์โปรแกรม...",
                StatusExtracting = "กำลังติดตั้งไฟล์โปรแกรม...",
                StatusShortcuts = "กำลังสร้างทางลัด...",
                StatusRegisteringService = "กำลังเริ่มต้นบริการเบื้องหลัง...",
                StatusComplete = "ติดตั้งสำเร็จเรียบร้อย!",
                CompleteTitle = "ติดตั้งเสร็จสมบูรณ์!",
                CompleteMessage = "Custom Clipboard Manager พร้อมใช้งานแล้ว",
                CompleteHotkeyHint = "💡 กด Ctrl + Shift + V เพื่อเปิดประวัติคลิปบอร์ดได้จากทุกที่",
                HotkeyCardTitle = "ปุ่มลัดเรียกใช้งาน:",
                HotkeyChangeHint = "กดเพื่อเปลี่ยนปุ่มลัด",
                HotkeyRecordingPrompt = "🔴 กดปุ่มบนคีย์บอร์ดที่ต้องการ...",
                HotkeyResetTooltip = "รีเซ็ตเป็นค่าเริ่มต้น (Ctrl + Shift + V)",
                HotkeyCustomItem = "กำหนดเอง...",
                LaunchFinishButton = "เริ่มใช้งานโปรแกรม",
                ErrorTitle = "เกิดข้อผิดพลาดในการติดตั้ง",
                RetryButton = "ลองใหม่อีกครั้ง",
                ReinstallButton = "ติดตั้ง / อัปเดต",
                UninstallButton = "ถอนการติดตั้ง",
                UninstallConfirmMessage = "ต้องการถอนการติดตั้ง Custom Clipboard Manager หรือไม่?",
                UninstallingTitle = "กำลังถอนการติดตั้ง...",
                UninstallCompleteTitle = "ถอนการติดตั้งเรียบร้อย!",
                UninstallCompleteMessage = "ถอนการติดตั้ง Custom Clipboard Manager เรียบร้อยแล้ว",
                CancelConfirmMessage = "การติดตั้งยังไม่เสร็จสิ้น คุณต้องการยกเลิกหรือไม่?",
            },
            ["en"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Custom Clipboard Manager Setup",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "Clipboard History Manager",
                DestinationFolder = "Install Location:",
                Browse = "Browse...",
                OptionDesktopShortcut = "Create Desktop Shortcut",
                OptionStartMenu = "Create Start Menu Shortcut",
                OptionAutoStart = "Start automatically with Windows",
                OptionInstallService = "Enable Background Service",
                OptionLaunchApp = "Launch app after installation",
                InstallButton = "Install",
                CancelButton = "Cancel",
                CloseButton = "Close",
                StatusReady = "Ready to install",
                StatusCheckingDotnet = "Checking .NET 10 Desktop Runtime...",
                StatusDownloadingDotnet = "Downloading .NET 10 Runtime...",
                StatusInstallingDotnet = "Installing .NET 10 Runtime...",
                StatusCheckingUpdate = "Checking for updates...",
                StatusDownloadingPackage = "Downloading application files...",
                StatusExtracting = "Extracting application files...",
                StatusShortcuts = "Creating shortcuts...",
                StatusRegisteringService = "Starting background service...",
                StatusComplete = "Installation completed!",
                CompleteTitle = "Installation Complete!",
                CompleteMessage = "Custom Clipboard Manager is ready to use.",
                CompleteHotkeyHint = "💡 Press Ctrl + Shift + V anywhere to access clipboard history",
                HotkeyCardTitle = "Hotkey Shortcut:",
                HotkeyChangeHint = "Click to record shortcut",
                HotkeyRecordingPrompt = "🔴 Press desired key combo...",
                HotkeyResetTooltip = "Reset to default (Ctrl + Shift + V)",
                HotkeyCustomItem = "Custom...",
                LaunchFinishButton = "Launch Application",
                ErrorTitle = "Installation Error",
                RetryButton = "Retry",
                ReinstallButton = "Install / Update",
                UninstallButton = "Uninstall",
                UninstallConfirmMessage = "Are you sure you want to uninstall Custom Clipboard Manager?",
                UninstallingTitle = "Uninstalling...",
                UninstallCompleteTitle = "Uninstalled Successfully",
                UninstallCompleteMessage = "Custom Clipboard Manager has been removed.",
                CancelConfirmMessage = "Installation is in progress. Are you sure you want to cancel?",
            },
            ["ja"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Custom Clipboard Manager インストール",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "クリップボード履歴マネージャー",
                DestinationFolder = "インストール先フォルダー:",
                Browse = "参照...",
                OptionDesktopShortcut = "デスクトップにショートカットを作成",
                OptionStartMenu = "スタートメニューにショートカットを作成",
                OptionAutoStart = "Windows 起動時に自動起動（推奨）",
                OptionInstallService = "CustomClipboardService（バックグラウンド）を登録",
                OptionLaunchApp = "インストール完了後にすぐに起動する",
                InstallButton = "今すぐインストール",
                CancelButton = "キャンセル",
                CloseButton = "閉じる",
                StatusReady = "インストールの準備が完了しました",
                StatusCheckingDotnet = ".NET 10 Desktop Runtime を確認中...",
                StatusDownloadingDotnet = "Microsoft から .NET 10 Runtime をダウンロード中...",
                StatusInstallingDotnet = ".NET 10 Runtime をインストール中...",
                StatusCheckingUpdate = "サーバー上の最新リリースを確認中...",
                StatusDownloadingPackage = "最新のプログラムファイルをダウンロード中...",
                StatusExtracting = "ファイルを展開して配置中...",
                StatusShortcuts = "ショートカットとレジストリを設定中...",
                StatusRegisteringService = "Windows サービスを登録して開始中...",
                StatusComplete = "インストールが正常に完了しました！",
                CompleteTitle = "インストール完了！",
                CompleteMessage = "Custom Clipboard Manager のセットアップが完了しました。",
                CompleteHotkeyHint = "💡 どの画面からでも Ctrl + Shift + V でクリップボードを呼び出せます",
                HotkeyCardTitle = "ショートカットキー:",
                HotkeyChangeHint = "クリックして変更",
                HotkeyRecordingPrompt = "🔴 キーボードのキーを押してください...",
                HotkeyResetTooltip = "デフォルトに戻す (Ctrl + Shift + V)",
                HotkeyCustomItem = "カスタム...",
                LaunchFinishButton = "完了して起動",
                ErrorTitle = "インストール中にエラーが発生しました",
                RetryButton = "再試行",
                ReinstallButton = "再インストール / 更新",
                UninstallButton = "アンインストール",
                UninstallConfirmMessage = "Custom Clipboard Manager をアンインストールしてもよろしいですか？",
                UninstallingTitle = "アンインストールしています...",
                UninstallCompleteTitle = "アンインストール完了！",
                UninstallCompleteMessage = "Custom Clipboard Manager は正常に削除されました。",
                CancelConfirmMessage = "インストールが完了していません。キャンセルしてもよろしいですか？",
            },
            ["zh-Hans"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Custom Clipboard Manager 安装程序",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "剪贴板历史记录管理",
                DestinationFolder = "安装目标路径:",
                Browse = "浏览...",
                OptionDesktopShortcut = "创建桌面快捷方式",
                OptionStartMenu = "在开始菜单中创建快捷方式",
                OptionAutoStart = "随 Windows 开机自动启动（推荐）",
                OptionInstallService = "安装 CustomClipboardService（后台服务）",
                OptionLaunchApp = "安装完成后立即启动程序",
                InstallButton = "立即安装",
                CancelButton = "取消",
                CloseButton = "关闭",
                StatusReady = "准备安装",
                StatusCheckingDotnet = "正在检查 .NET 10 Desktop Runtime...",
                StatusDownloadingDotnet = "正在从微软下载 .NET 10 Runtime...",
                StatusInstallingDotnet = "正在安装 .NET 10 Runtime...",
                StatusCheckingUpdate = "正在从服务器获取最新版本...",
                StatusDownloadingPackage = "正在下载应用程序包...",
                StatusExtracting = "正在解压并部署程序文件...",
                StatusShortcuts = "正在创建快捷方式及配置注册表...",
                StatusRegisteringService = "正在注册并启动 Windows 服务...",
                StatusComplete = "安装圆满完成！",
                CompleteTitle = "安装完成！",
                CompleteMessage = "Custom Clipboard Manager 已成功安装到您的电脑。",
                CompleteHotkeyHint = "💡 在任何地方按下 Ctrl + Shift + V 即可随时唤出剪贴板历史",
                HotkeyCardTitle = "全局唤出热键:",
                HotkeyChangeHint = "点击修改热键",
                HotkeyRecordingPrompt = "🔴 请按下要设置的快捷键...",
                HotkeyResetTooltip = "重置为默认值 (Ctrl + Shift + V)",
                HotkeyCustomItem = "自定义...",
                LaunchFinishButton = "完成并启动",
                ErrorTitle = "安装过程中出现错误",
                RetryButton = "重试",
                ReinstallButton = "重新安装 / 更新",
                UninstallButton = "卸载",
                UninstallConfirmMessage = "您确定要卸载 Custom Clipboard Manager 吗？",
                UninstallingTitle = "正在卸载 Custom Clipboard Manager...",
                UninstallCompleteTitle = "卸载完成！",
                UninstallCompleteMessage = "Custom Clipboard Manager 及其后台服务已成功从电脑中移除。",
                CancelConfirmMessage = "安装尚未完成，确定要取消吗？",
            },
            ["zh-Hant"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Custom Clipboard Manager 安裝程式",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "剪貼簿歷程記錄管理",
                DestinationFolder = "安裝目標路徑:",
                Browse = "瀏覽...",
                OptionDesktopShortcut = "建立桌面捷徑",
                OptionStartMenu = "在開始功能表中建立捷徑",
                OptionAutoStart = "隨 Windows 開機自動啟動（推薦）",
                OptionInstallService = "安裝 CustomClipboardService（背景服務）",
                OptionLaunchApp = "安裝完成後立即啟動程式",
                InstallButton = "立即安裝",
                CancelButton = "取消",
                CloseButton = "關閉",
                StatusReady = "準備安裝",
                StatusCheckingDotnet = "正在檢查 .NET 10 Desktop Runtime...",
                StatusDownloadingDotnet = "正在從微軟下載 .NET 10 Runtime...",
                StatusInstallingDotnet = "正在安裝 .NET 10 Runtime...",
                StatusCheckingUpdate = "正在從伺服器取得最新版本...",
                StatusDownloadingPackage = "正在下載應用程式套件...",
                StatusExtracting = "正在解壓縮並部署程式檔案...",
                StatusShortcuts = "正在建立捷徑及設定註冊表...",
                StatusRegisteringService = "正在註冊並啟動 Windows 服務...",
                StatusComplete = "安裝圓滿完成！",
                CompleteTitle = "安裝完成！",
                CompleteMessage = "Custom Clipboard Manager 已成功安裝至您的電腦。",
                CompleteHotkeyHint = "💡 在任何畫面按下 Ctrl + Shift + V 即可隨時喚出剪貼簿歷史",
                HotkeyCardTitle = "全域快捷鍵:",
                HotkeyChangeHint = "按一下以變更快捷鍵",
                HotkeyRecordingPrompt = "🔴 請按下要設定的快捷鍵...",
                HotkeyResetTooltip = "重設為預設值 (Ctrl + Shift + V)",
                HotkeyCustomItem = "自訂...",
                LaunchFinishButton = "完成並啟動",
                ErrorTitle = "安裝過程中發生錯誤",
                RetryButton = "重試",
                ReinstallButton = "重新安裝 / 更新",
                UninstallButton = "解除安裝",
                UninstallConfirmMessage = "您確定要解除安裝 Custom Clipboard Manager 嗎？",
                UninstallingTitle = "正在解除安裝 Custom Clipboard Manager...",
                UninstallCompleteTitle = "解除安裝完成！",
                UninstallCompleteMessage = "Custom Clipboard Manager 及其後台服務已成功從電腦中移除。",
                CancelConfirmMessage = "安裝尚未完成，確定要取消嗎？",
            },
            ["de"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Custom Clipboard Manager Installation",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "Zwischenablage-Verlauf-Manager",
                DestinationFolder = "Installationsordner:",
                Browse = "Durchsuchen...",
                OptionDesktopShortcut = "Desktop-Verknüpfung erstellen",
                OptionStartMenu = "Startmenü-Verknüpfung erstellen",
                OptionAutoStart = "Mit Windows automatisch starten (Empfohlen)",
                OptionInstallService = "CustomClipboardService (Hintergrunddienst) installieren",
                OptionLaunchApp = "Nach der Installation sofort starten",
                InstallButton = "Jetzt installieren",
                CancelButton = "Abbrechen",
                CloseButton = "Schließen",
                StatusReady = "Bereit zur Installation",
                StatusCheckingDotnet = ".NET 10 Desktop Runtime wird geprüft...",
                StatusDownloadingDotnet = ".NET 10 Runtime wird von Microsoft heruntergeladen...",
                StatusInstallingDotnet = ".NET 10 Runtime wird installiert...",
                StatusCheckingUpdate = "Prüfe auf neuesten Release...",
                StatusDownloadingPackage = "Programmpaket wird heruntergeladen...",
                StatusExtracting = "Dateien werden entpackt und bereitgestellt...",
                StatusShortcuts = "Verknüpfungen und Registrierung werden eingerichtet...",
                StatusRegisteringService = "Windows-Dienst wird registriert und gestartet...",
                StatusComplete = "Installation erfolgreich abgeschlossen!",
                CompleteTitle = "Installation abgeschlossen!",
                CompleteMessage = "Custom Clipboard Manager wurde erfolgreich auf Ihrem PC installiert.",
                CompleteHotkeyHint = "💡 Drücken Sie jederzeit Strg + Umschalt + V, um den Zwischenablage-Verlauf aufzurufen",
                HotkeyCardTitle = "Globaler Hotkey:",
                HotkeyChangeHint = "Klicken zum Ändern",
                HotkeyRecordingPrompt = "🔴 Gewünschte Tastenkombination drücken...",
                HotkeyResetTooltip = "Auf Standard zurücksetzen (Ctrl + Shift + V)",
                HotkeyCustomItem = "Benutzerdefiniert...",
                LaunchFinishButton = "Fertigstellen & Starten",
                ErrorTitle = "Fehler bei der Installation aufgetreten",
                RetryButton = "Wiederholen",
                ReinstallButton = "Neu installieren / Aktualisieren",
                UninstallButton = "Deinstallieren",
                UninstallConfirmMessage = "Möchten Sie Custom Clipboard Manager wirklich deinstallieren?",
                UninstallingTitle = "Custom Clipboard Manager wird deinstalliert...",
                UninstallCompleteTitle = "Deinstallation abgeschlossen!",
                UninstallCompleteMessage = "Custom Clipboard Manager wurde erfolgreich von Ihrem PC entfernt.",
                CancelConfirmMessage = "Die Installation ist noch nicht abgeschlossen. Möchten Sie wirklich abbrechen?",
            },
            ["es"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Instalador de Custom Clipboard Manager",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "Gestor de historial del portapapeles",
                DestinationFolder = "Carpeta de instalación:",
                Browse = "Examinar...",
                OptionDesktopShortcut = "Crear acceso directo en el escritorio",
                OptionStartMenu = "Crear acceso directo en el Menú Inicio",
                OptionAutoStart = "Iniciar con Windows automáticamente (Recomendado)",
                OptionInstallService = "Instalar CustomClipboardService (Servicio en segundo plano)",
                OptionLaunchApp = "Iniciar la aplicación al finalizar",
                InstallButton = "Instalar ahora",
                CancelButton = "Cancelar",
                CloseButton = "Cerrar",
                StatusReady = "Listo para instalar",
                StatusCheckingDotnet = "Comprobando .NET 10 Desktop Runtime...",
                StatusDownloadingDotnet = "Descargando .NET 10 Runtime desde Microsoft...",
                StatusInstallingDotnet = "Instalando .NET 10 Runtime...",
                StatusCheckingUpdate = "Comprobando última versión en el servidor...",
                StatusDownloadingPackage = "Descargando paquete de la aplicación...",
                StatusExtracting = "Extrayendo y copiando archivos...",
                StatusShortcuts = "Configurando accesos directos y registro...",
                StatusRegisteringService = "Registrando e iniciando servicio de Windows...",
                StatusComplete = "¡Instalación completada con éxito!",
                CompleteTitle = "¡Instalación Completa!",
                CompleteMessage = "Custom Clipboard Manager se ha instalado correctamente.",
                CompleteHotkeyHint = "💡 Pulsa Ctrl + Shift + V en cualquier lugar para abrir el portapapeles",
                HotkeyCardTitle = "Atajo de teclado:",
                HotkeyChangeHint = "Haz clic para cambiar",
                HotkeyRecordingPrompt = "🔴 Presione la combinación de teclas deseada...",
                HotkeyResetTooltip = "Restablecer por defecto (Ctrl + Shift + V)",
                HotkeyCustomItem = "Personalizado...",
                LaunchFinishButton = "Iniciar y Salir",
                ErrorTitle = "Se produjo un error durante la instalación",
                RetryButton = "Reintentar",
                ReinstallButton = "Reinstalar / Actualizar",
                UninstallButton = "Desinstalar",
                UninstallConfirmMessage = "¿Está seguro de que desea desinstalar Custom Clipboard Manager?",
                UninstallingTitle = "Desinstalando Custom Clipboard Manager...",
                UninstallCompleteTitle = "¡Desinstalación completada!",
                UninstallCompleteMessage = "Custom Clipboard Manager y su servicio en segundo plano se han eliminado correctamente de su PC.",
                CancelConfirmMessage = "La instalación aún no ha finalizado. ¿Está seguro de que desea cancelar?",
            },
            ["fr"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Installation de Custom Clipboard Manager",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "Gestionnaire d'historique du presse-papiers",
                DestinationFolder = "Dossier d'installation :",
                Browse = "Parcourir...",
                OptionDesktopShortcut = "Créer un raccourci sur le Bureau",
                OptionStartMenu = "Créer un raccourci dans le Menu Démarrer",
                OptionAutoStart = "Lancer au démarrage de Windows (Recommandé)",
                OptionInstallService = "Installer CustomClipboardService (Service en arrière-plan)",
                OptionLaunchApp = "Lancer l'application après l'installation",
                InstallButton = "Installer maintenant",
                CancelButton = "Annuler",
                CloseButton = "Fermer",
                StatusReady = "Prêt pour l'installation",
                StatusCheckingDotnet = "Vérification de .NET 10 Desktop Runtime...",
                StatusDownloadingDotnet = "Téléchargement de .NET 10 Runtime depuis Microsoft...",
                StatusInstallingDotnet = "Installation de .NET 10 Runtime...",
                StatusCheckingUpdate = "Vérification des mises à jour sur le serveur...",
                StatusDownloadingPackage = "Téléchargement des fichiers du programme...",
                StatusExtracting = "Extraction et déploiement des fichiers...",
                StatusShortcuts = "Création des raccourcis et configuration...",
                StatusRegisteringService = "Enregistrement et démarrage du service Windows...",
                StatusComplete = "Installation réussie avec succès !",
                CompleteTitle = "Installation Terminée !",
                CompleteMessage = "Custom Clipboard Manager a été installé avec succès.",
                CompleteHotkeyHint = "💡 Appuyez sur Ctrl + Maj + V n'importe où pour ouvrir l'historique",
                HotkeyCardTitle = "Raccourci clavier :",
                HotkeyChangeHint = "Cliquer pour modifier",
                HotkeyRecordingPrompt = "🔴 Appuyez sur la combinaison de touches...",
                HotkeyResetTooltip = "Réinitialiser par défaut (Ctrl + Shift + V)",
                HotkeyCustomItem = "Personnalisé...",
                LaunchFinishButton = "Terminer et lancer",
                ErrorTitle = "Une erreur est survenue lors de l'installation",
                RetryButton = "Réessayer",
                ReinstallButton = "Réinstaller / Mettre à jour",
                UninstallButton = "Désinstaller",
                UninstallConfirmMessage = "Êtes-vous sûr de vouloir désinstaller Custom Clipboard Manager ?",
                UninstallingTitle = "Désinstallation de Custom Clipboard Manager...",
                UninstallCompleteTitle = "Désinstallation terminée !",
                UninstallCompleteMessage = "Custom Clipboard Manager a été supprimé de votre ordinateur avec succès.",
                CancelConfirmMessage = "L'installation n'est pas terminée. Êtes-vous sûr de vouloir annuler ?",
            },
            ["ko"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Custom Clipboard Manager 설치 프로그램",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "클립보드 기록 관리자",
                DestinationFolder = "설치 위치:",
                Browse = "찾아보기...",
                OptionDesktopShortcut = "바탕 화면에 바로 가기 만들기",
                OptionStartMenu = "시작 메뉴에 바로 가기 만들기",
                OptionAutoStart = "Windows 시작 시 자동 실행 (권장)",
                OptionInstallService = "CustomClipboardService (백그라운드 서비스) 등록",
                OptionLaunchApp = "설치 완료 후 즉시 프로그램 시작",
                InstallButton = "지금 설치",
                CancelButton = "취소",
                CloseButton = "닫기",
                StatusReady = "설치 준비 완료",
                StatusCheckingDotnet = ".NET 10 Desktop Runtime 확인 중...",
                StatusDownloadingDotnet = "Microsoft에서 .NET 10 Runtime 다운로드 중...",
                StatusInstallingDotnet = ".NET 10 Runtime 설치 중...",
                StatusCheckingUpdate = "서버에서 최신 릴리스 확인 중...",
                StatusDownloadingPackage = "프로그램 패키지 다운로드 중...",
                StatusExtracting = "파일 압축 해제 및 복사 중...",
                StatusShortcuts = "바로 가기 및 레지스트리 설정 중...",
                StatusRegisteringService = "Windows 서비스 등록 및 시작 중...",
                StatusComplete = "설치가 성공적으로 완료되었습니다!",
                CompleteTitle = "설치 완료!",
                CompleteMessage = "Custom Clipboard Manager가 컴퓨터에 설치되었습니다.",
                CompleteHotkeyHint = "💡 어디서든 Ctrl + Shift + V를 누르면 클립보드 창이 열립니다",
                HotkeyCardTitle = "실행 단축키:",
                HotkeyChangeHint = "클릭하여 변경",
                HotkeyRecordingPrompt = "🔴 원하는 키를 누르세요...",
                HotkeyResetTooltip = "기본값으로 재설정 (Ctrl + Shift + V)",
                HotkeyCustomItem = "사용자 지정...",
                LaunchFinishButton = "완료 및 실행",
                ErrorTitle = "설치 중 오류가 발생했습니다",
                RetryButton = "다시 시도",
                ReinstallButton = "재설치 / 업데이트",
                UninstallButton = "제거",
                UninstallConfirmMessage = "Custom Clipboard Manager를 제거하시겠습니까?",
                UninstallingTitle = "Custom Clipboard Manager 제거 중...",
                UninstallCompleteTitle = "제거 완료!",
                UninstallCompleteMessage = "Custom Clipboard Manager 및 백그라운드 서비스가 PC에서 성공적으로 제거되었습니다.",
                CancelConfirmMessage = "설치가 아직 완료되지 않았습니다. 취소하시겠습니까?",
            },
            ["ru"] = new LocaleStrings
            {
                AppName = "Custom Clipboard Manager",
                AppVersion = "v1.0.0",
                WindowTitle = "Установка Custom Clipboard Manager",
                HeaderTitle = "Custom Clipboard Manager",
                HeaderSubtitle = "Менеджер истории буфера обмена",
                DestinationFolder = "Папка установки:",
                Browse = "Обзор...",
                OptionDesktopShortcut = "Создать ярлык на рабочем столе",
                OptionStartMenu = "Создать ярлык в меню «Пуск»",
                OptionAutoStart = "Запускать автоматически при входе в Windows",
                OptionInstallService = "Установить CustomClipboardService (Фоновая служба)",
                OptionLaunchApp = "Запустить приложение после завершения установки",
                InstallButton = "Установить сейчас",
                CancelButton = "Отмена",
                CloseButton = "Закрыть",
                StatusReady = "Готово к установке",
                StatusCheckingDotnet = "Проверка .NET 10 Desktop Runtime...",
                StatusDownloadingDotnet = "Загрузка .NET 10 Runtime с серверов Microsoft...",
                StatusInstallingDotnet = "Установка .NET 10 Runtime...",
                StatusCheckingUpdate = "Проверка наличия обновлений на сервере...",
                StatusDownloadingPackage = "Загрузка пакета приложения...",
                StatusExtracting = "Извлечение и копирование файлов...",
                StatusShortcuts = "Создание ярлыков и настройка реестра...",
                StatusRegisteringService = "Регистрация и запуск службы Windows...",
                StatusComplete = "Установка успешно завершена!",
                CompleteTitle = "Установка завершена!",
                CompleteMessage = "Custom Clipboard Manager успешно установлен на ваш компьютер.",
                CompleteHotkeyHint = "💡 Нажмите Ctrl + Shift + V в любом месте, чтобы открыть буфер обмена",
                HotkeyCardTitle = "Горячая клавиша:",
                HotkeyChangeHint = "Нажмите для изменения",
                HotkeyRecordingPrompt = "🔴 Нажмите нужные клавиши на клавиатуре...",
                HotkeyResetTooltip = "Сбросить по умолчанию (Ctrl + Shift + V)",
                HotkeyCustomItem = "Пользовательский...",
                LaunchFinishButton = "Завершить и запустить",
                ErrorTitle = "Произошла ошибка при установке",
                RetryButton = "Повторить",
                ReinstallButton = "Переустановить / Обновить",
                UninstallButton = "Удалить",
                UninstallConfirmMessage = "Вы действительно хотите удалить Custom Clipboard Manager?",
                UninstallingTitle = "Удаление Custom Clipboard Manager...",
                UninstallCompleteTitle = "Удаление завершено!",
                UninstallCompleteMessage = "Custom Clipboard Manager и его фоновая служба успешно удалены с вашего ПК.",
                CancelConfirmMessage = "Установка не завершена. Вы действительно хотите отменить?",
            }
        };

        public static string CurrentLanguageCode { get; private set; } = "en";
        public static LocaleStrings Current { get; private set; } = _locales["en"];

        public static event Action LanguageChanged;

        static LocalizationManager()
        {
            DetectSystemLanguage();
        }

        public static void DetectSystemLanguage()
        {
            string uiLang = CultureInfo.CurrentUICulture.Name.ToLowerInvariant();
            string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

            if (twoLetter == "th") SetLanguage("th");
            else if (twoLetter == "ja") SetLanguage("ja");
            else if (uiLang.StartsWith("zh-tw") || uiLang.StartsWith("zh-hk") || uiLang.StartsWith("zh-hant")) SetLanguage("zh-Hant");
            else if (twoLetter == "zh") SetLanguage("zh-Hans");
            else if (twoLetter == "de") SetLanguage("de");
            else if (twoLetter == "es") SetLanguage("es");
            else if (twoLetter == "fr") SetLanguage("fr");
            else if (twoLetter == "ko") SetLanguage("ko");
            else if (twoLetter == "ru") SetLanguage("ru");
            else SetLanguage("en");
        }

        public static void SetLanguage(string code)
        {
            if (_locales.TryGetValue(code, out var loc))
            {
                CurrentLanguageCode = code;
                Current = loc;
                LanguageChanged?.Invoke();
            }
            else if (_locales.TryGetValue("en", out var enLoc))
            {
                CurrentLanguageCode = "en";
                Current = enLoc;
                LanguageChanged?.Invoke();
            }
        }
    }
}
