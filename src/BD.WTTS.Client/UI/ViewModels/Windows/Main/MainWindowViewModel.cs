// ReSharper disable once CheckNamespace
namespace BD.WTTS.UI.ViewModels;

public sealed partial class MainWindowViewModel : WindowViewModel
{
    #region 更改通知

    //[Reactive]
    //public bool Topmost { get; set; }

    [Reactive]
    public ViewModelBase? SelectedItem { get; set; }

    [Reactive]
    public bool IsOpenUserMenu { get; set; }

    //public ICommand OpenUserMenu { get; }

    #endregion

    public StartPageViewModel StartPage => GetTabItemVM<StartPageViewModel>();

    public CommunityProxyPageViewModel CommunityProxyPage => GetTabItemVM<CommunityProxyPageViewModel>();

    public ProxyScriptManagePageViewModel ProxyScriptPage => GetTabItemVM<ProxyScriptManagePageViewModel>();

    public SteamAccountPageViewModel SteamAccountPage => GetTabItemVM<SteamAccountPageViewModel>();

    public GameListPageViewModel GameListPage => GetTabItemVM<GameListPageViewModel>();

    public LocalAuthPageViewModel LocalAuthPage => GetTabItemVM<LocalAuthPageViewModel>();

    public SteamIdlePageViewModel SteamIdlePage => GetTabItemVM<SteamIdlePageViewModel>();

    public ArchiSteamFarmPlusPageViewModel ASFPage => GetTabItemVM<ArchiSteamFarmPlusPageViewModel>();

    public GameRelatedPageViewModel GameRelatedPage => GetTabItemVM<GameRelatedPageViewModel>();

    public OtherPlatformPageViewModel OtherPlatformPage => GetTabItemVM<OtherPlatformPageViewModel>();

    static readonly IPlatformService platformService = IPlatformService.Instance;

    public MainWindowViewModel()
    {
        if (IApplication.IsDesktop())
        {
            var adminTag = platformService.IsAdministrator ? (OperatingSystem.IsWindows() ? " (Administrator)" : " (Root)") : string.Empty;
            var title = $"{AssemblyInfo.Trademark} {RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()} v{ThisAssembly.VersionDisplay} for {DeviceInfo2.OSName()}{adminTag}";
#if DEBUG
            title = $"[Debug] {title}";
#endif
            Title = title;

            IUserManager.Instance.OnSignOut += () =>
            {
                IsOpenUserMenu = false;
            };

            //OpenUserMenu = ReactiveCommand.Create(() =>
            //{
            //    IsOpenUserMenu = UserService.Current.IsAuthenticated;
            //    if (!IsOpenUserMenu)
            //    {
            //        UserService.Current.ShowWindow(CustomWindow.LoginOrRegister);
            //    }
            //});
        }

        #region InitTabItems

        List<TabItemViewModel.TabItemId> tabIdItems = new();
        List<TabItemViewModel.TabItemId> footerTabIdItems = new();

        var showProxyScript = !OperatingSystem.IsWindows() || ResourceService.IsChineseSimplified;

        //tabIdItems.Add(TabItemViewModel.TabItemId.StartPage);
        if (showProxyScript)
        {
            // Android 目前底部菜单实现要隐藏需要改多个地方
            // 主要是初始化时不依赖 TabItems，两边逻辑暂不能同步一套代码
            tabIdItems.Add(TabItemViewModel.TabItemId.CommunityProxy);
        }
        if (IApplication.IsDesktop())
        {
            if (showProxyScript)
            {
                tabIdItems.Add(TabItemViewModel.TabItemId.ProxyScriptManage);
            }
            tabIdItems.Add(TabItemViewModel.TabItemId.SteamAccount);
            tabIdItems.Add(TabItemViewModel.TabItemId.GameList);
        }
        tabIdItems.Add(TabItemViewModel.TabItemId.LocalAuth);
        tabIdItems.Add(TabItemViewModel.TabItemId.ArchiSteamFarmPlus);
        //tabIdItems.Add(TabItemViewModel.TabItemId.SteamIdle);

#if !TRAY_INDEPENDENT_PROGRAM
        if (OperatingSystem.IsWindows())
            tabIdItems.Add(TabItemViewModel.TabItemId.GameRelated);
#endif

#if !TRAY_INDEPENDENT_PROGRAM && DEBUG
        if (IApplication.EnableDevtools && IApplication.IsDesktop())
        {
            footerTabIdItems.Add(TabItemViewModel.TabItemId.Debug);
        }
#endif
        footerTabIdItems.Add(TabItemViewModel.TabItemId.Settings);
        footerTabIdItems.Add(TabItemViewModel.TabItemId.About);

        TabIdItems = tabIdItems.ToArray();
        FooterTabIdItems = footerTabIdItems.ToArray();
        AllTabIdItems = new HashSet<TabItemViewModel.TabItemId>(TabIdItems.Concat(FooterTabIdItems));
        AllTabLazyItems = AllTabIdItems.ToDictionary(TabItemViewModel.GetType, v => new Lazy<TabItemViewModel>(() => TabItemViewModel.Create(v)));

        #endregion

        SelectedItem = AllTabLazyItems.First().Value.Value;
    }

    public override void Initialize()
    {
        Task.Run(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            if (!IsInitialized)
            {
                Task.Run(async () =>
                {
                    if (ResourceService.IsChineseSimplified)
                    {
                        await ProxyService.Current.Initialize();
                    }
                    if (ASFSettings.AutoRunArchiSteamFarm.Value)
                    {
                        if (platformService.UsePlatformForegroundService)
                        {
                            await platformService.StartOrStopForegroundServiceAsync(nameof(ASFService), true);
                        }
                        else
                        {
                            await ASFService.Current.InitASF();
                        }
                    }
                });

                if (IApplication.IsDesktop())
                {
                    SteamConnectService.Current.Initialize();
                    SteamConnectService.Current.RefreshSteamUsers();
                }

                Parallel.ForEach(TabItems, item =>
                {
                    item.Initialize();
                    //Task.Run(item.Initialize).ForgetAndDispose();
                });
                IsInitialized = true;
            }
        }).ForgetAndDispose();
    }

    //public async override void Activation()
    //{
    //    if (IsFirstActivation)
    //    {
    //        if (UISettings.DoNotShowMessageBoxs.Value?.Contains(MessageBox.DontPromptType.Donate) == false)
    //        {
    //            //INotificationService.Instance.Notify("如果你觉得 Watt Toolkit 好用，你可以考虑给我们一些捐助以支持我们继续开发，谢谢！", NotificationType.Message);
    //            await MessageBox.ShowAsync("如果你觉得 Watt Toolkit 好用，你可以考虑给我们一些捐助以支持我们继续开发，谢谢！", button: MessageBox.Button.OK,
    //                rememberChooseKey: MessageBox.DontPromptType.Donate);
    //        }
    //    }
    //    base.Activation();
    //}
}