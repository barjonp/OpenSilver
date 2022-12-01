

/*===================================================================================
* 
*   Copyright (c) Userware (OpenSilver.net, CSHTML5.com)
*      
*   This file is part of both the OpenSilver Simulator (https://opensilver.net), which
*   is licensed under the MIT license (https://opensource.org/licenses/MIT), and the
*   CSHTML5 Simulator (http://cshtml5.com), which is dual-licensed (MIT + commercial).
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Windows;
using System.Xaml;
using System.Xml.Serialization;
using System.Diagnostics;
using MahApps.Metro.Controls;
using System.Windows.Controls;
using System.Globalization;
using DotNetForHtml5.EmulatorWithoutJavascript.XamlInspection;
using Microsoft.Win32;
using DotNetForHtml5.EmulatorWithoutJavascript.LicensingServiceReference;
using DotNetForHtml5.EmulatorWithoutJavascript.LicenseChecking;
using DotNetForHtml5.Compiler;
using System.Threading;
using System.Windows.Threading;
using DotNetBrowser;
using DotNetBrowser.WPF;
using System.Windows.Media;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net.NetworkInformation;
using DotNetForHtml5.EmulatorWithoutJavascript.Debugging;
using DotNetBrowser.Events;
using DotNetForHtml5.EmulatorWithoutJavascript.Console;
using System.Windows.Media.Imaging;
using OpenSilver.Simulator;

namespace DotNetForHtml5.EmulatorWithoutJavascript
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        static readonly TimeSpan DelayToUpdateThePositionOfTheHighlightAfterResize = new TimeSpan(0, 0, 1);
        public const string ARBITRARY_FILE_NAME_WHEN_RUNNING_FROM_SIMULATOR = "RunningFromSimulator.html";
        public const string TipToCopyToClipboard = "TIP: You can copy the content of this message box by pressing Ctrl+C now.";
        const string NoteWhenUnableToLaunchTheGeneratedHtml = "Note: please look at the \"Output\" pane of Visual Studio for any compilation errors or warnings. We also suggest forcing a Rebuild of your Visual Studio project. If the problem persists, please contact support at: support@cshtml5.com";
        string _pathOfAssemblyThatContainsEntryPoint;
        JavaScriptExecutionHandler _javaScriptExecutionHandler;
        bool _htmlHasBeenLoaded = false;
        Assembly _entryPointAssembly;
        Action _appCreationDelegate;
        SimulatorLaunchParameters _simulatorLaunchParameters;
        string _outputRootPath;
        string _outputResourcesPath;
        WPFBrowserView MainWebBrowser;
        ChromiumDevTools _devTools;
        bool _pendingRefreshOfHighlight = false;
        string _browserUserDataDir;

        const bool IS_LICENSE_CHECKER_ENABLED = false;
        const string NAME_FOR_STORING_COOKIES = "ms_cookies_for_user_application"; // This is an arbitrary name used to store the cookies in the registry
        const string NAME_OF_TEMP_CACHE_FOLDER = "simulator-temp-cache";

        LicenseChecker LicenseChecker = null;

        public MainWindow(Action appCreationDelegate, Assembly appAssembly, SimulatorLaunchParameters simulatorLaunchParameters)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;

            InitializeComponent();
            Instance = this;

            Icon = new BitmapImage(new Uri("pack://application:,,,/OpenSilver.Simulator;component/OpenSilverIcon.ico"));

#if ENABLE_DOTNETBROWSER_LOGGING
            // Enable logging of the browser control, cf. https://dotnetbrowser.support.teamdev.com/support/solutions/articles/9000110288-logging
            LoggerProvider.Instance.LoggingEnabled = true;
            LoggerProvider.Instance.FileLoggingEnabled = true;
            LoggerProvider.Instance.OutputFile = @"C:\temp\DotNetBrowser.log";
            LoggerProvider.Instance.ChromiumLogFile = @"C:\temp\chromium.log";
            BrowserPreferences.SetChromiumSwitches("--v=1");
#endif

            _appCreationDelegate = appCreationDelegate ?? throw new ArgumentNullException(nameof(appCreationDelegate));
            _simulatorLaunchParameters = simulatorLaunchParameters;
            _entryPointAssembly = appAssembly;
            _pathOfAssemblyThatContainsEntryPoint = _entryPointAssembly.Location;

            _browserUserDataDir = Path.GetFullPath(NAME_OF_TEMP_CACHE_FOLDER);
            Directory.CreateDirectory(_browserUserDataDir);
            if (CrossDomainCallsHelper.IsBypassCORSErrors)
            {
                BrowserPreferences.SetChromiumSwitches(@"--disable-web-security");
            }

            BrowserPreferences.SetChromiumSwitches(
                @"--disable-web-security",
                @"--allow-file-access-from-files",
                @"--allow-file-access",
                @"--remote-debugging-port=9222"
            );

            BrowserContextParams parameters = new BrowserContextParams(_browserUserDataDir)
            {
                StorageType = StorageType.DISK //Note: this is needed to remember the cookies
            };
            BrowserContext context = new BrowserContext(parameters);
            context.NetworkService.NetworkDelegate = new ResourceInterceptor("http://cshtml5-simulator/");
            Browser browser = BrowserFactory.Create(context, BrowserType.LIGHTWEIGHT);
            MainWebBrowser = new WPFBrowserView(browser);

            MainWebBrowser.Width = 150;
            MainWebBrowser.Height = 200;
            MainWebBrowser.SizeChanged += MainWebBrowser_SizeChanged;
            CookiesHelper.SetCustomCookies(MainWebBrowser, simulatorLaunchParameters?.CookiesData);
            simulatorLaunchParameters?.BrowserCreatedCallback?.Invoke(MainWebBrowser);

            BrowserContainer.Child = MainWebBrowser;

            if (IS_LICENSE_CHECKER_ENABLED)
            {
                LicenseChecker = new LicenseChecker();
                LicenseCheckerContainer.Child = LicenseChecker;
                if (!IsNetworkAvailable())
                    LicenseChecker.OnNetworkNotAvailable();
            }
            else
            {
                ButtonProfil.Visibility = Visibility.Collapsed;
            }

            CheckBoxCORS.IsChecked = CrossDomainCallsHelper.IsBypassCORSErrors;
            CheckBoxCORS.Checked += CheckBoxCORS_Checked;
            CheckBoxCORS.Unchecked += CheckBoxCORS_Unchecked;

            LoadDisplaySize();

            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // Custom load handler to reload the app after redirection, for example in case of authentication scenarios (eg. Azure Active Directory login redirection):
            var customLoadHandler = new CustomLoadHandler();
            customLoadHandler.CustomResponseEvent += delegate (object sender, CustomResponseEventArgs e)
            {
                // Azure Active Directory redirects to the following URL after authentication, so we need to reload the app instead of loading this URL:
                if (e.Url.Contains(@"http://cshtml5-fbc-mm2-preview.azurewebsites.net"))
                {
                    browser.Stop();
                    string urlFragment = "";
                    int hashIndex = e.Url.IndexOf('#');
                    if (hashIndex != -1)
                        urlFragment = e.Url.Substring(hashIndex);

                    // We use a dispatcher to go back to the main thread so that the CurrentCulture remains the same (otherwise, for example on French systems we get an exception during Double.Parse when processing the <Path Data="..."/> control).
                    Dispatcher.BeginInvoke(() => ReloadAppAfterRedirect(urlFragment));
                }
            };
            browser.LoadHandler = customLoadHandler;
            NetworkChange.NetworkAvailabilityChanged += MainWindow_NetworkAvailabilityChanged;

            // Continue when the window is loaded:
            this.Loaded += MainWindow_Loaded;
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            SimulatorProxy.ShowExceptionStatic(e.Exception);
            e.Handled = true;
        }

        private void MainWindow_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            // we signal to the license checker that internet availability has changed to avoid the default error 404 page of the browser
            if (LicenseChecker != null)
                LicenseChecker.OnNetworkAvailabilityChanged(e.IsAvailable);
        }

        async void MainWebBrowser_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DisplaySize_Desktop.IsChecked == true
                && _htmlHasBeenLoaded)
            {
                // Apply the size in pixels to the root <div> inside the html page:
                ReflectBrowserSizeOnRootElementSize();

                // Update the position of the highlight rectangle (the element picker) in case that the XAML inspector is open:
                await RepositionHighlightElementIfNecessary();
            }
        }

        async Task RepositionHighlightElementIfNecessary()
        {
            // Update the position of the highlight rectangle (the element picker) in case that the XAML inspector is open:
            if (HighlightElement.Visibility == Visibility.Visible && HighlightElement.Tag != null)
            {
                if (!_pendingRefreshOfHighlight)
                {
                    _pendingRefreshOfHighlight = true;
                    await Task.Delay(DelayToUpdateThePositionOfTheHighlightAfterResize); // We give some time to the page to redraw based on the new size, so that elements are repositioned.

                    // We need to check again because in the meantime the user could have closed the inspector:
                    if (HighlightElement.Visibility == Visibility.Visible && HighlightElement.Tag != null)
                    {
                        XamlInspectionHelper.HighlightElement(HighlightElement.Tag, HighlightElement, MainWebBrowser.Browser);
                    }
                    _pendingRefreshOfHighlight = false;
                }
            }
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyLocalName = args.Name.IndexOf(',') >= 0 ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name;

            switch (assemblyLocalName)
            {
                case Constants.NAME_OF_CORE_ASSEMBLY_USING_BLAZOR:
                case "OpenSilver.Controls.Data":
                case "OpenSilver.Controls.Data.Input":
                case "OpenSilver.Controls.Data.DataForm.Toolkit":
                case "OpenSilver.Controls.DataVisualization.Toolkit":
                case "OpenSilver.Controls.Navigation":
                case "OpenSilver.Controls.Input":
                case "OpenSilver.Controls.Layout.Toolkit":
                case "OpenSilver.Interactivity":
                case "OpenSilver.Expression.Interactions":
                case "OpenSilver.Expression.Effects":
                    // If specified DLL has absolute path, look in same folder:
                    string pathOfAssemblyThatContainsEntryPoint;
                    string candidatePath;
                    if (ReflectionInUserAssembliesHelper.TryGetPathOfAssemblyThatContainsEntryPoint(out pathOfAssemblyThatContainsEntryPoint))
                    {
                        if (pathOfAssemblyThatContainsEntryPoint.Contains("\\"))
                        {
                            candidatePath = $"{Path.GetDirectoryName(pathOfAssemblyThatContainsEntryPoint)}\\{assemblyLocalName}.dll";
                            return Assembly.LoadFile(candidatePath);
                        }
                    }

                    // Otherwise look in current execution folder:
                    return Assembly.LoadFile($"{assemblyLocalName}.dll");

                default:
                    if (args.RequestingAssembly != null)
                    {
                        string assemblyFileName = $"{assemblyLocalName}.dll";
                        string invariantFullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(args.RequestingAssembly.Location), assemblyFileName));

                        string fullPath;
                        if (!File.Exists(invariantFullPath))
                        {
                            string cultureName = Thread.CurrentThread.CurrentCulture.Name;
                            fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(args.RequestingAssembly.Location), cultureName, assemblyFileName));
                        }
                        else
                        {
                            fullPath = invariantFullPath;
                        }

                        if (File.Exists(fullPath))
                        {
                            var assembly = Assembly.LoadFile(fullPath);
                            return assembly;
                        }
                        else
                        {
                            throw new FileNotFoundException($"Assembly {assemblyFileName} not found.\nSearched at:\n{invariantFullPath}\n{fullPath}");
                        }
                    }
                    return null;
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the WebBrowser control:
            if (InitializeApplication())
            {
                MainWebBrowser.DocumentLoadedInMainFrameEvent += (s1, e1) =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        //todo: verify that we are not on an outside page (eg. Azure Active Directory login page)
                        OnLoaded();
                    }, DispatcherPriority.ApplicationIdle);
                };
                MainWebBrowser.ConsoleMessageEvent += OnConsoleMessageEvent;
            }

            LoadIndexFile();
        }

        private void OnConsoleMessageEvent(object sender, ConsoleEventArgs args)
        {
            switch (args.Level)
            {
#if DEBUG
                case ConsoleEventArgs.MessageLevel.DEBUG:
#endif
                case ConsoleEventArgs.MessageLevel.LOG:
                    Console.AddMessage(new ConsoleMessage(args.Message, ConsoleMessage.MessageLevel.Log));
                    break;
                case ConsoleEventArgs.MessageLevel.WARNING:
                    if (!string.IsNullOrEmpty(args.Source))
                    {
                        Console.AddMessage(new ConsoleMessage(
                            args.Message,
                            ConsoleMessage.MessageLevel.Warning,
                            new FileSource(args.Source, args.LineNumber)
                            ));
                    }
                    else
                    {
                        Console.AddMessage(new ConsoleMessage(
                            args.Message,
                            ConsoleMessage.MessageLevel.Warning
                            ));
                    }
                    break;
                case ConsoleEventArgs.MessageLevel.ERROR:
                    if (!string.IsNullOrEmpty(args.Source))
                    {
                        Console.AddMessage(new ConsoleMessage(
                            args.Message,
                            ConsoleMessage.MessageLevel.Error,
                            new FileSource(args.Source, args.LineNumber)
                            ));
                    }
                    else
                    {
                        Console.AddMessage(new ConsoleMessage(
                            args.Message,
                            ConsoleMessage.MessageLevel.Error
                            ));
                    }
                    break;
            }
        }

        void LoadIndexFile(string urlFragment = null)
        {
            var absolutePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "simulator_root.html");

            string simulatorRootHtml = File.ReadAllText(absolutePath);

            string outputPathAbsolute = GetOutputPathAbsoluteAndReadAssemblyAttributes();

            //string outputPathAbsolute = PathsHelper.GetOutputPathAbsolute(pathOfAssemblyThatContainsEntryPoint, outputRootPath);

            // Read the "App.Config" file for future use by the ClientBase.
            string relativePathToAppConfigFolder = PathsHelper.CombinePathsWhileEnsuringEndingBackslashAndMore(_outputResourcesPath, _entryPointAssembly.GetName().Name);
            string relativePathToAppConfig = Path.Combine(relativePathToAppConfigFolder, "app.config.g.js");
            if (File.Exists(Path.Combine(outputPathAbsolute, relativePathToAppConfig)))
            {
                string scriptToReadAppConfig = "<script type=\"application/javascript\" src=\"" + Path.Combine(outputPathAbsolute, relativePathToAppConfig) + "\"></script>";
                simulatorRootHtml = simulatorRootHtml.Replace("[SCRIPT_TO_READ_APPCONFIG_GOES_HERE]", scriptToReadAppConfig);
            }
            else
            {
                simulatorRootHtml = simulatorRootHtml.Replace("[SCRIPT_TO_READ_APPCONFIG_GOES_HERE]", string.Empty);
            }

            // Read the "ServiceReferences.ClientConfig" file for future use by the ClientBase:
            string relativePathToServiceReferencesClientConfig = Path.Combine(relativePathToAppConfigFolder, "servicereferences.clientconfig.g.js");
            if (File.Exists(Path.Combine(outputPathAbsolute, relativePathToServiceReferencesClientConfig)))
            {
                string scriptToReadServiceReferencesClientConfig = "<script type=\"application/javascript\" src=\"" + Path.Combine(outputPathAbsolute, relativePathToServiceReferencesClientConfig) + "\"></script>";
                simulatorRootHtml = simulatorRootHtml.Replace("[SCRIPT_TO_READ_SERVICEREFERENCESCLIENTCONFIG_GOES_HERE]", scriptToReadServiceReferencesClientConfig);

            }
            else
            {
                simulatorRootHtml = simulatorRootHtml.Replace("[SCRIPT_TO_READ_SERVICEREFERENCESCLIENTCONFIG_GOES_HERE]", string.Empty);
            }

            simulatorRootHtml = simulatorRootHtml.Replace("..", "[PARENT]");

            // Set the base URL (it defaults to the Simulator exe location, but it can be specified in the command line arguments):
            string baseURL;
            string customBaseUrl;
            if (ReflectionInUserAssembliesHelper.TryGetCustomBaseUrl(out customBaseUrl))
            {
                baseURL = customBaseUrl;
            }
            else
            {
                baseURL = "file:///" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/');
            }

            if (_simulatorLaunchParameters?.InitParams != null)
            {
                simulatorRootHtml = simulatorRootHtml.Replace(
                    "[PARAM_INITPARAMS_GOES_HERE]",
                    $"<param name=\"InitParams\" value=\"{_simulatorLaunchParameters.InitParams}\"");
            }
            else
            {
                simulatorRootHtml = simulatorRootHtml.Replace("[PARAM_INITPARAMS_GOES_HERE]", string.Empty);
            }

            // Load the page:
            MainWebBrowser.Browser.LoadHTML(new LoadHTMLParams(simulatorRootHtml, "UTF-8", "http://cshtml5-simulator/" + ARBITRARY_FILE_NAME_WHEN_RUNNING_FROM_SIMULATOR + urlFragment)); // Note: we set the URL so that the simulator browser can find the JS files.
        }

        void OnLoaded()
        {
            if (!_htmlHasBeenLoaded)
            {
                _htmlHasBeenLoaded = true;
                UpdateWebBrowserAndWebPageSizeBasedOnCurrentState();

                // Start the app:
                ShowLoadingMessage();

                //We check if the key used by the user is still valid:
                CheckKeysValidity();

                WaitForDocumentToBeFullyLoaded(); // Note: without this, we got errors when running rokjs (with localhost as base url) without any breakpoints.

                Dispatcher.BeginInvoke((Action)(() =>
                {
                    bool success = StartApplication();

                    if (success)
                    {
                        _simulatorLaunchParameters?.AppStartedCallback?.Invoke();
                    }

                    HideLoadingMessage();

                    UpdateWebBrowserAndWebPageSizeBasedOnCurrentState();
                }), DispatcherPriority.ApplicationIdle); // We do so in order to give the time to the rendering engine to display the "Loading..." message.
            }
        }

        private void WaitForDocumentToBeFullyLoaded()
        {
            int startTime = Environment.TickCount;
            bool loaded = false;

            JSValue htmlDocument = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue(@"document");

            while (!loaded && (Environment.TickCount - startTime < 4000)) // Wait is limited to max 4 seconds.
            {
                if (htmlDocument != null)
                {
                    JSValue xamlRoot = null;
                    try
                    {
                        xamlRoot = CallJSMethodAndReturnValue(htmlDocument, "getXamlRoot");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Initialization: can not get the root. {ex.Message}");
                    }

                    if (xamlRoot != null && xamlRoot.IsObject())
                    {
                        loaded = true;
                        break;
                    }
                    else
                    {
                        const string ROOT_NAME = "opensilver-root";
                        Debug.WriteLine($"Initialization: {ROOT_NAME} was not ready on first try.");
                    }
                }
                else
                {
                    Debug.WriteLine("Initialization: htmlDocument was null on first try.");
                    htmlDocument = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue(@"document");
                }

                Thread.Sleep(50);
            }

            if (!loaded)
            {
                Debug.WriteLine("Initialization: The document was still not loaded after timeout.");
            }
        }

        private void CheckKeysValidity()
        {
            Thread thread = new Thread(() =>
            {
                bool isAllOK = CheckFeatureValidity(Constants.ENTERPRISE_EDITION_FEATURE_ID, Constants.ENTERPRISE_EDITION_FRIENDLY_NAME);
                isAllOK = isAllOK && CheckFeatureValidity(Constants.SL_MIGRATION_EDITION_FEATURE_ID, Constants.SL_MIGRATION_EDITION_FRIENDLY_NAME);
                isAllOK = isAllOK && CheckFeatureValidity(Constants.PROFESSIONAL_EDITION_FEATURE_ID, Constants.PROFESSIONAL_EDITION_FRIENDLY_NAME);
                isAllOK = isAllOK && CheckFeatureValidity(Constants.COMMERCIAL_EDITION_S_FEATURE_ID, Constants.COMMERCIAL_EDITION_S_FRIENDLY_NAME);
                isAllOK = isAllOK && CheckFeatureValidity(Constants.COMMERCIAL_EDITION_L_FEATURE_ID, Constants.COMMERCIAL_EDITION_L_FRIENDLY_NAME);
                isAllOK = isAllOK && CheckFeatureValidity(Constants.PREMIUM_SUPPORT_EDITION_FEATURE_ID, Constants.PREMIUM_SUPPORT_EDITION_FRIENDLY_NAME);
            });
            thread.Start();
        }

        private bool CheckFeatureValidity(string featureId, string editionName)
        {
            string computerName = Environment.MachineName;
            try
            {
                //We get the correponding key (or above):
                if (featureId != null)
                {
                    Guid keyGuid;
                    DateTime currentVersionReleaseDate = VersionInformation.GetCurrentVersionReleaseDate();

                    if (ActivationHelpers.IsFeatureEnabled(featureId))
                    {

                        if (Guid.TryParse(RegistryHelpers.GetSetting("Feature_" + featureId, null), out keyGuid))
                        {
                            LicensingServiceClient licensingServiceClient;
                            //licensingServiceClient = new LicensingServiceReference.LicensingServiceClient("LocalTestBinding_ILicensingService"); //Testing version.
                            //todo: BEFORE THE RELEASE put the following back and comment the line above.
                            licensingServiceClient = new LicensingServiceClient(
                    new BasicHttpBinding(BasicHttpSecurityMode.Transport),
                    new EndpointAddress(new Uri(@"https://myaccount.cshtml5.com/LicensingService.svc"))); //Normal version.

                            KeyValidity keyValidity = licensingServiceClient.CheckLicenseValidity(keyGuid, computerName, currentVersionReleaseDate.ToOADate());
                            //Note: we get an EndpointNotFoundException (which is caught and dealt with a bit lower in the code) if there is no internet.

                            //We refresh the validity date:
                            if (keyValidity.ValidityLimit != DateTime.MinValue)
                            {
                                ValidityHelpers.SetValidityLimit(featureId, keyValidity.ValidityLimit);
                            }
                            else
                            {
                                ValidityHelpers.RemoveKeyValidity(featureId);
                            }

                            switch (keyValidity.State) //NOTE: keyValidity is not supposed to be null
                            {
                                case KeyState.Valid:
                                    return true;
                                case KeyState.Expired:
                                    //licensingServiceClient.DeactivateKey(keyGuid.ToString());
                                    bool isDeactivated = licensingServiceClient.DeactivateKey(keyGuid.ToString());

                                    // We remove the key from the registry:
                                    if (isDeactivated)
                                    {
                                        if (ActivationHelpers.IsFeatureEnabled(featureId))
                                        {
                                            RegistryHelpers.DeleteSetting("Feature_" + featureId); //remove the key itself
                                            RegistryHelpers.DeleteSetting("Validity_" + featureId); //remove the validity date for the key
                                        }
                                    }

                                    Dispatcher.BeginInvoke(
                                        () => MessageBox.Show("The key for the " + editionName + " is no longer valid and has been deactivated."),
                                        DispatcherPriority.ApplicationIdle);

                                    return false;
                                case KeyState.AlmostExpired:
                                    if (!ValidityHelpers.WasTheKeyAlmostExpiredMessageAlreadyDisplayedToday)
                                    {
                                        Dispatcher.BeginInvoke(() => MessageBox.Show(keyValidity.Message), DispatcherPriority.ApplicationIdle);

                                        ValidityHelpers.RememberThatTheKeyAlmostExpiredMessageWasDisplayedToday();
                                    }
                                    return true;
                                default:
                                    throw new Exception("Invalid Key validity state."); //No idea how we would arrive here.
                            }
                        }
                        else
                        {
                            throw new FaultException("The key for the " + editionName + " could not be retrieved. Please try to reactivate the associated key and if it does not fix your issue, please contact us at support@cshtml5.com");
                            //I don't like the fact that it is a FaultException since it wasn't thrown by the WebService but the behaviour fits.
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    throw new Exception("Could not check validity: feature is null");
                }
            }
            catch (Exception ex) // Note: we have only one "catch" because C# does not allow putting multiple exception types in a single block.
            {
                if (ex is EndpointNotFoundException || (ex.Message.Contains("LicensingService"))) // Note: this second condition can catch errors obtained for example when Fiddler is running.
                {
                    //we could not get the information on the internet so we rely on the registry:
                    if (!ValidityHelpers.IsTheKeyValid(featureId))
                    {
                        Dispatcher.BeginInvoke(
                            () => MessageBox.Show("The key for the " + editionName + " is no longer considered valid on this computer and there doesn't seem to be an internet connection to refresh its validity date. If you are currently connected to the internet, please contact us at support@cshtml5.com."),
                            DispatcherPriority.ApplicationIdle);
                        return false;
                    }
                    return true; //todo: display something in the Simulator to tell the user that we were unable to connect.
                }
                else if (ex is FaultException)
                {
                    Dispatcher.BeginInvoke(
                        () => MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error), 
                        DispatcherPriority.ApplicationIdle);
                    return false;
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        MessageBox.Show("An error has occurred. Please contact support at: support@cshtml5.com\r\n\r\n-------------------\r\n\r\nError details:\r\n" + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }, DispatcherPriority.ApplicationIdle);
                    return false;
                }
            }
        }

        private void ButtonStats_Click(object sender, RoutedEventArgs e)
        {
            // Count the number of DOM elements:
            var count = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue(@"document.getElementsByTagName(""*"").length").ToString();

            // Display the result
            MessageBox.Show("Number of DOM elements: " + count
                + Environment.NewLine
                + Environment.NewLine
                + "TIPS:"
                + Environment.NewLine
                + Environment.NewLine
                + "- For best performance on mobile devices, be sure to keep the number of DOM elements low by limiting the number of UI Elements in the Visual Tree, and by using only default control templates instead of custom control templates. Also note that scrolling performance is greatly improved when the scroll bar visibility of ScrollViewers is set to 'Visible' rather than 'Auto'."
                + Environment.NewLine
                + Environment.NewLine
                + @"- If a portion of your application requires to display thousands of UI Elements, such as in a custom Chart or Calendar control, or if you need very high performance graphics, for example for games, you may want to use the ""HtmlCanvas"" control. To learn about it, please read:"
                + Environment.NewLine
                + "  http://cshtml5.com/links/how-to-use-the-html5-canvas.aspx"
                + Environment.NewLine
                + Environment.NewLine
                + "- To learn how to profile performance in order to pinpoint performance issues, please read:"
                + "  http://cshtml5.com/links/how-to-profile-performance.aspx"
                );
        }

        private string getHtmlSnapshot(bool osRootOnly = false, string htmlElementId = null, string xamlElementName = null)
        {
            string html;
            if (htmlElementId != null)
            {
                html = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue($"document.getElementById('{htmlElementId}').outerHTML").ToString();
            }
            else if (xamlElementName != null)
            {
                html = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue($"document.querySelectorAll('[dataid=\"{xamlElementName}\"]')[0].outerHTML").ToString();
            }
            else if (osRootOnly)
            {
                html = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue("document.getElementById('opensilver-root').outerHTML").ToString();
            }
            else
            {
                html = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue("document.documentElement.outerHTML").ToString();
            }
            return html ?? "";
        }

        private void ButtonSeeHtml_Click(object sender, RoutedEventArgs e)
        {
            string html = getHtmlSnapshot();
            var msgBox = new MessageBoxScrollable()
            {
                Value = html,
                Title = "Snapshot of HTML displayed in the Simulator"
            };
            msgBox.Show();
        }

        private void ButtonSaveHtml_Click(object sender, RoutedEventArgs e)
        {
            string html = getHtmlSnapshot();
            SaveFileDialog saveFileDialog = new SaveFileDialog() { FileName = "index.html" };
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, html);
        }

        private void ButtonRestart_Click(object sender, RoutedEventArgs e)
        {
            this.StartApplication();
        }

        private void ButtonDebugJavaScriptLog_Click(object sender, RoutedEventArgs e)
        {
            string destinationFolderName = "TempDebugOpenSilver";
            string info =
$@"This feature lets you debug the JavaScript code executed by the Simulator so far, which corresponds to the content of the Interop.ExecuteJavaScript(...) calls as well as the JS/C# interop calls that are specific to the Simulator.

A folder named '{destinationFolderName}' will be created on your desktop. The folder will contain a file named 'index.html' and other files. Just open that file with a browser and use the Browser Developer Tools to debug the code. In particular, you can look for errors in the browser Console output, and you can enable the 'Pause on caught exceptions' option in the Developer Tools to step into the code when an error occurs.

Click OK to continue.";
            MessageBoxResult result = MessageBox.Show(info, "Information", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (result != MessageBoxResult.Cancel)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string destinationPath = Path.Combine(desktopPath, destinationFolderName);

                try
                {
                    // Create the destination folder if it does not already exist:
                    if (!Directory.Exists(destinationPath))
                        Directory.CreateDirectory(destinationPath);

                    // Copy the html file:
                    string simulatorExePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    File.Copy(Path.Combine(simulatorExePath, "interop_debug_root.html"), Path.Combine(destinationPath, "index.html"), true);

                    string simulatorJsCssPath = Path.Combine(simulatorExePath, @"js_css");

                    File.Copy(Path.Combine(simulatorJsCssPath, "cshtml5.css"), Path.Combine(destinationPath, "cshtml5.css"), true);
                    File.Copy(Path.Combine(simulatorJsCssPath, "cshtml5.js"), Path.Combine(destinationPath, "cshtml5.js"), true);
                    File.Copy(Path.Combine(simulatorJsCssPath, "velocity.js"), Path.Combine(destinationPath, "velocity.js"), true);
                    File.Copy(Path.Combine(simulatorJsCssPath, "flatpickr.css"), Path.Combine(destinationPath, "flatpickr.css"), true);
                    File.Copy(Path.Combine(simulatorJsCssPath, "flatpickr.js"), Path.Combine(destinationPath, "flatpickr.js"), true);
                    File.Copy(Path.Combine(simulatorJsCssPath, "ResizeSensor.js"), Path.Combine(destinationPath, "ResizeSensor.js"), true);

                    // Create "interopcalls.js" which contains all the JS executed by the Simulator so far:
                    string fullLog = _javaScriptExecutionHandler.FullLogOfExecutedJavaScriptCode;
                    File.WriteAllText(Path.Combine(destinationPath, "interopcalls.js"), fullLog);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to generate the debug files in the folder:\r\n\r\n" + destinationPath + "\r\n\r\n" + ex.ToString());
                    return;
                }

                // Open the destination folder with Explorer:
                try
                {
                    Process.Start(destinationPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\r\n\r\n" + destinationPath);
                }
            }
        }

        bool InitializeApplication()
        {
            // In OpenSilver we already have the user application type passed to the constructor, so we do not need to retrieve it here
            try
            {
                // Create the JavaScriptExecutionHandler that will be called by the "Core" project to interact with the Emulator:
                _javaScriptExecutionHandler = new JavaScriptExecutionHandler(MainWebBrowser);

                // Create the HTML DOM MANAGER proxy and pass it to the "Core" project:
                JSValue htmlDocument = (JSObject)MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue("document");

                InteropHelpers.InjectDOMDocument(MainWebBrowser.Browser.GetDocument());
                InteropHelpers.InjectHtmlDocument(htmlDocument);
                InteropHelpers.InjectWebControlDispatcherBeginInvoke(MainWebBrowser);
                InteropHelpers.InjectWebControlDispatcherInvoke(MainWebBrowser);
                InteropHelpers.InjectWebControlDispatcherCheckAccess(MainWebBrowser);
                InteropHelpers.InjectConvertBrowserResult(BrowserResultConverter.CastFromJsValue);
                InteropHelpers.InjectJavaScriptExecutionHandler(_javaScriptExecutionHandler);
                InteropHelpers.InjectWpfMediaElementFactory();
                InteropHelpers.InjectWebClientFactory();
                InteropHelpers.InjectClipboardHandler();
                InteropHelpers.InjectSimulatorProxy(new SimulatorProxy(MainWebBrowser, Console));
                InteropHelpers.InjectIsRunningInTheSimulator_WorkAround();

                WpfMediaElementFactory._gridWhereToPlaceMediaElements = GridForAudioMediaElements;

                // Inject the code to display the message box in the simulator:
                InteropHelpers.InjectCodeToDisplayTheMessageBox(
                    (message, title, showCancelButton) =>
                        MessageBox.Show(message, title, showCancelButton ? MessageBoxButton.OKCancel : MessageBoxButton.OK) == MessageBoxResult.OK);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while loading the application: " + Environment.NewLine + Environment.NewLine + ex.Message);
                HideLoadingMessage();
                return false;
            }
        }

        void ReloadAppAfterRedirect(string urlFragment)
        {
            // We will need to wait for the page to finish loading before executing the app:
            MainWebBrowser.DocumentLoadedInMainFrameEvent += (s1, e1) =>
            {
                Dispatcher.BeginInvoke((Action)(async () =>
                {
                    InteropHelpers.RaiseReloadedEvent(); // to reset some static fields
                    await Task.Delay(3000); //Note: this is to ensure all the js and css files of simulator_root.html have been loaded (client_fb).
                    StartApplication();
                }));
            };

            // Load the page:
            LoadIndexFile(urlFragment);
        }

        bool StartApplication()
        {
            // Create a new instance of the application:
            try
            {
                _appCreationDelegate();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to start the application.\r\n\r\n" + ex.ToString());
                HideLoadingMessage();
                return false;
            }
        }

        /// <summary>
        /// This Method returns the absolute path to the Output folder and sets the _outputRootPath, _outputAppFilesPath, _outputLibrariesPath, _outputResourcesPath and _intermediateOutputAbsolutePath variables.
        /// </summary>
        /// <returns>The absolute path to the Output folder.</returns>
        string GetOutputPathAbsoluteAndReadAssemblyAttributes()
        {
            //--------------------------
            // Note: this method is similar to the one in the Compiler (PathsHelper).
            // IMPORTANT: If you update this method, make sure to update the other one as well.
            //--------------------------

            // Determine the output path by reading the "OutputRootPath" attribute that the compiler has injected into the entry assembly:
            if (_outputRootPath == null)
            {
                ReflectionInUserAssembliesHelper.GetOutputPathsByReadingAssemblyAttributes(_entryPointAssembly, out _outputRootPath, out _, out _, out _outputResourcesPath, out _);
            }

            string outputRootPathFixed = _outputRootPath.Replace('/', '\\');
            if (!outputRootPathFixed.EndsWith("\\") && outputRootPathFixed != "")
                outputRootPathFixed = outputRootPathFixed + '\\';

            // If the path is already ABSOLUTE, we return it directly, otherwise we concatenate it to the path of the assembly:
            string outputPathAbsolute;
            if (Path.IsPathRooted(outputRootPathFixed))
            {
                outputPathAbsolute = outputRootPathFixed;
            }
            else
            {
                outputPathAbsolute = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_pathOfAssemblyThatContainsEntryPoint)), outputRootPathFixed);

                outputPathAbsolute = outputPathAbsolute.Replace('/', '\\');

                if (!outputPathAbsolute.EndsWith("\\") && outputPathAbsolute != "")
                    outputPathAbsolute = outputPathAbsolute + '\\';
            }

            return outputPathAbsolute;
        }

        string GetOutputIndexPath()
        {
            string absoluteOutputPath = GetOutputPathAbsoluteAndReadAssemblyAttributes();
            string result = Path.Combine(absoluteOutputPath, "index.html");
            result = result.Replace('/', '\\');
            return result;
        }

        private void ButtonShowAdvancedTools_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).ContextMenu.IsOpen = true;
        }

        void ButtonClearCookiesAndCache_Click(object sender, RoutedEventArgs e)
        {
            CookiesHelper.ClearCookies(MainWebBrowser, NAME_FOR_STORING_COOKIES);
            try
            {
                if (!string.IsNullOrWhiteSpace(_browserUserDataDir)
                    && Directory.Exists(_browserUserDataDir))
                {
                    MessageBoxResult result
                        = MessageBox.Show("To fully clear the Simulator cache, please close the Simulator and manually delete the following folder:" + Environment.NewLine + Environment.NewLine + _browserUserDataDir + Environment.NewLine + Environment.NewLine + "Click OK to see this folder in Windows Explorer.", "Confirm?", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        Process.Start(_browserUserDataDir);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        void ShowLoadingMessage()
        {
            ContainerOfLoadingMessage.Visibility = Visibility.Visible;
        }

        void HideLoadingMessage()
        {
            ContainerOfLoadingMessage.Visibility = Visibility.Collapsed;
        }

        void SetWebBrowserSize(double width, double height)
        {
            try
            {
                // We take into account the "Font Size" (DPI) setting of Windows: //cf. http://answers.awesomium.com/questions/321/non-standard-dpi-rendering-is-broken-in-webcontrol.html
                double correctedWidth = ScreenCoordinatesHelper.ConvertWidthOrNaNToDpiAwareWidthOrNaN(width);
                double correctedHeight = ScreenCoordinatesHelper.ConvertHeightOrNaNToDpiAwareHeightOrNaN(height);
                MainWebBrowser.Width = correctedWidth;
                MainWebBrowser.Height = correctedHeight;

                Dispatcher.BeginInvoke(ReflectBrowserSizeOnRootElementSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        void ReflectBrowserSizeOnRootElementSize()
        {
            try
            {
                JSValue htmlDocument = MainWebBrowser.Browser.ExecuteJavaScriptAndReturnValue(@"document");

                if (htmlDocument != null)
                {
                    JSValue cshtml5DomRootElement = null;
                    try
                    {
                        cshtml5DomRootElement = CallJSMethodAndReturnValue(htmlDocument, "getXamlRoot");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Can not get the root. {ex.Message}");
                    }

                    if (cshtml5DomRootElement != null && cshtml5DomRootElement.IsObject()) // It is not an object for example if the app has navigated to another page via "System.Windows.Browser.HtmlPage.Window.Navigate(url)"
                    {
                        double width = double.IsNaN(MainWebBrowser.Width) ? MainWebBrowser.ActualWidth : MainWebBrowser.Width;
                        double height = double.IsNaN(MainWebBrowser.Height) ? MainWebBrowser.ActualHeight : MainWebBrowser.Height;

                        // Take into account screen DPI:
                        width = ScreenCoordinatesHelper.ConvertWidthOrNaNToDpiAwareWidthOrNaN(width, invert: true); // Supports "NaN"
                        height = ScreenCoordinatesHelper.ConvertWidthOrNaNToDpiAwareWidthOrNaN(height, invert: true); // Supports "NaN"

                        if (!double.IsNaN(width))
                            SetJSProperty(cshtml5DomRootElement, "style", width.ToString(CultureInfo.InvariantCulture) + "px");

                        if (!double.IsNaN(height))
                            SetJSProperty(cshtml5DomRootElement, "style", height.ToString(CultureInfo.InvariantCulture) + "px");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        static JSValue CallJSMethodAndReturnValue(JSValue instance, string methodname, params object[] args)
        {
            var function = GetJSProperty(instance, methodname);
            if (function == null || !function.IsFunction())
            {
                throw new ApplicationException($"'{methodname}' is not a function or does not exist");
            }
            var result = function.AsFunction().InvokeAndReturnValue(instance.AsObject(), args);

            return result;
        }


        static JSValue GetJSProperty(JSValue instance, string propertyName)
        {
            JSValue result = instance.AsObject().GetProperty(propertyName);

            return result;
        }

        static bool SetJSProperty(JSValue instance, string propertyName, object value)
        {
            var result = instance.AsObject().SetProperty(propertyName, value);

            return result;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Destroy the WebControl and its underlying view:
            MainWebBrowser.Browser.Dispose();
            MainWebBrowser.Dispose();
            if (IS_LICENSE_CHECKER_ENABLED)
            {
                LicenseChecker.Dispose();
            }

            // Kill the process to avoid having the Simulator process that remains open due to a MessageBox or something else:
            Application.Current.Shutdown();
        }

        string _lastExecutedJavaScript = "";
        void ButtonExecuteJS_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = new InputBox()
            {
                Value = _lastExecutedJavaScript,
                Title = "Please enter JS to execute"
            };
            inputBox.Callback = (Action<bool>)(isOK =>
            {
                if (isOK)
                {
                    _javaScriptExecutionHandler.ExecuteJavaScript(inputBox.Value);
                    _lastExecutedJavaScript = inputBox.Value;
                }
            });
            inputBox.Show();
        }

        void ButtonTestOnMobileDevices_Click(object sender, RoutedEventArgs e)
        {
            StartWebServer(useLocalhost: false);
        }

        void StartWebServer(bool useLocalhost)
        {
            try
            {
                // First, verify that "index.html" was properly created:
                string pathOfIndexFile = GetOutputIndexPath();
                if (File.Exists(pathOfIndexFile))
                {
                    string fileNameForLocalServer = "CSharpXamlForHtml5.LocalServer.exe";
                    string simulatorDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                    string fullPathToLocalServer;
                    if (File.Exists(Path.Combine(simulatorDirectory, fileNameForLocalServer)))
                    {
                        //---------------------
                        // When distributed by the MSI, the file is in the same directory as the simulator:
                        //---------------------
                        fullPathToLocalServer = Path.Combine(simulatorDirectory, fileNameForLocalServer);
                    }
                    else
                    {
                        //---------------------
                        // When in the development solution, the file is in its own project folder:
                        //---------------------
                        fullPathToLocalServer = Path.Combine(simulatorDirectory, @"..\..\..\DotNetForHtml5.LocalServer\bin\Debug\CSharpXamlForHtml5.LocalServer.exe");
                    }
                    if (File.Exists(fullPathToLocalServer))
                    {
                        string outputPathAbsolute = GetOutputPathAbsoluteAndReadAssemblyAttributes();
                        string arguments = (useLocalhost ? "uselocalhost" : "nolocalhost") + " " + "\"" + outputPathAbsolute + "\"";
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = Path.GetFileName(fullPathToLocalServer);
                        psi.WorkingDirectory = Path.GetDirectoryName(fullPathToLocalServer);
                        psi.Arguments = arguments;
                        try
                        {
                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("File not found: " + Path.Combine(Directory.GetCurrentDirectory(), fileNameForLocalServer));
                    }
                }
                else
                {
                    MessageBox.Show("There was an error generating HTML/JavaScript files." + "\r\n\r\n" + NoteWhenUnableToLaunchTheGeneratedHtml);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DisplaySize_Click(object sender, RoutedEventArgs e)
        {
            SaveDisplaySize();
            UpdateWebBrowserAndWebPageSizeBasedOnCurrentState();
        }

        void UpdateWebBrowserAndWebPageSizeBasedOnCurrentState()
        {
            if (DisplaySize_Phone.IsChecked == true)
            {
                this.ResizeMode = ResizeMode.CanMinimize;
                this.SizeToContent = SizeToContent.WidthAndHeight;
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 40; // Prevents the window from growing below the Windows task bar, cf. https://stackoverflow.com/questions/25790674/wpf-scrollbar-on-auto-and-sizetocontent-height-goes-under-windows7-toolbar
                OptionsForDisplaySize_Phone.Visibility = Visibility.Visible;
                OptionsForDisplaySize_Tablet.Visibility = Visibility.Collapsed;
                PhoneDecoration1.Visibility = Visibility.Visible;
                PhoneDecoration2.Visibility = Visibility.Visible;
                MainBorder.Background = new SolidColorBrush(Color.FromArgb(255, 34, 34, 34));
                MainBorder.HorizontalAlignment = HorizontalAlignment.Center;
                MainBorder.VerticalAlignment = VerticalAlignment.Top;
                MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                if (DisplaySize_Phone_Landscape.IsChecked == true)
                {
                    SetWebBrowserSize(480, 320);
                    ContainerForMainWebBrowserAndHighlightElement.Margin = new Thickness(60, 10, 60, 10);
                }
                else
                {
                    SetWebBrowserSize(320, 480);
                    ContainerForMainWebBrowserAndHighlightElement.Margin = new Thickness(10, 60, 10, 60);
                }
            }
            else if (DisplaySize_Tablet.IsChecked == true)
            {
                this.ResizeMode = ResizeMode.CanMinimize;
                this.SizeToContent = SizeToContent.WidthAndHeight;
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 40; // Prevents the window from growing below the Windows task bar, cf. https://stackoverflow.com/questions/25790674/wpf-scrollbar-on-auto-and-sizetocontent-height-goes-under-windows7-toolbar
                OptionsForDisplaySize_Phone.Visibility = Visibility.Collapsed;
                OptionsForDisplaySize_Tablet.Visibility = Visibility.Visible;
                PhoneDecoration1.Visibility = Visibility.Visible;
                PhoneDecoration2.Visibility = Visibility.Visible;
                MainBorder.Background = new SolidColorBrush(Color.FromArgb(255, 34, 34, 34));
                MainBorder.HorizontalAlignment = HorizontalAlignment.Center;
                MainBorder.VerticalAlignment = VerticalAlignment.Top;
                MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                if (DisplaySize_Tablet_Landscape.IsChecked == true)
                {
                    SetWebBrowserSize(1024, 768);
                    ContainerForMainWebBrowserAndHighlightElement.Margin = new Thickness(60, 10, 60, 10);
                }
                else
                {
                    SetWebBrowserSize(768, 1024);
                    ContainerForMainWebBrowserAndHighlightElement.Margin = new Thickness(10, 60, 10, 60);
                }
            }
            else if (DisplaySize_Desktop.IsChecked == true)
            {
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                this.SizeToContent = SizeToContent.Manual;
                this.MaxHeight = double.PositiveInfinity;
                OptionsForDisplaySize_Phone.Visibility = Visibility.Collapsed;
                OptionsForDisplaySize_Tablet.Visibility = Visibility.Collapsed;
                PhoneDecoration1.Visibility = Visibility.Collapsed;
                PhoneDecoration2.Visibility = Visibility.Collapsed;
                MainBorder.Background = new SolidColorBrush(Colors.Transparent);
                MainBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                MainBorder.VerticalAlignment = VerticalAlignment.Stretch;
                MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

                SetWebBrowserSize(double.NaN, double.NaN);
                ContainerForMainWebBrowserAndHighlightElement.Margin = new Thickness(0, 0, 0, 0);
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.Width = 1024;
                    this.Height = 768;
                }));
            }
            else
            {
                MessageBox.Show("Error: no display size selected. Please report this error to the authors.");
            }
        }

        private void ButtonViewXamlTree_Click(object sender, RoutedEventArgs e)
        {
            if (_entryPointAssembly != null
                && XamlInspectionTreeViewInstance.TryRefresh(_entryPointAssembly, XamlPropertiesPaneInstance, MainWebBrowser, HighlightElement))
            {
                MainGridSplitter.Visibility = Visibility.Visible;
                BorderForXamlInspection.Visibility = Visibility.Visible;
                ButtonViewXamlTree.Visibility = Visibility.Collapsed;
                ContainerForXamlInspectorToolbar.Visibility = Visibility.Visible;
                ButtonHideXamlTree.Visibility = Visibility.Visible;

                // We hide the highlight until an item is selected in the TreeView:
                HighlightElement.Visibility = Visibility.Collapsed;

                // We activate the element picker by default:
                StartElementPickerForInspection();
            }
            else
            {
                ButtonHideXamlTree_Click(sender, e);
                MessageBox.Show("The Visual Tree is not available.");
            }
        }

        private void ButtonOpenDevTools_Click(object sender, RoutedEventArgs e)
        {
            if (_devTools != null)
            {
                _devTools.Focus();
                return;
            }

            DevToolsScreencastInfoWindow infoWindow = new DevToolsScreencastInfoWindow(this);
            infoWindow.ShowDialog();

            _devTools = new ChromiumDevTools(MainWebBrowser.Browser.GetRemoteDebuggingURL());
            _devTools.Show();

            _devTools.Closing += ChromiumDevTools_Closing;
        }

        private void ChromiumDevTools_Closing(object sender, CancelEventArgs e)
        {
            _devTools = null;
        }

        private void ButtonHideXamlTree_Click(object sender, RoutedEventArgs e)
        {
            MainGridSplitter.Visibility = Visibility.Collapsed;
            BorderForXamlInspection.Visibility = Visibility.Collapsed;
            ButtonViewXamlTree.Visibility = Visibility.Visible;
            ContainerForXamlInspectorToolbar.Visibility = Visibility.Collapsed;
            ButtonHideXamlTree.Visibility = Visibility.Collapsed;
            HighlightElement.Visibility = Visibility.Collapsed;
            XamlPropertiesPaneInstance.Width = 0;

            // Reset columns in case they were modified by the GridSplitter:
            ColumnForLeftToolbar.Width = GridLength.Auto;
            ColumnForMainWebBrowser.Width = new GridLength(1, GridUnitType.Star);
            ColumnForGridSplitter.Width = GridLength.Auto;
            ColumnForXamlInspection.Width = GridLength.Auto;
            ColumnForXamlPropertiesPane.Width = GridLength.Auto;

            // Ensure that the element picker is not activated:
            StopElementPickerForInspection();
        }

        private void ButtonRefreshXamlTree_Click(object sender, RoutedEventArgs e)
        {
            ButtonViewXamlTree_Click(sender, e);
        }

        void ButtonXamlInspectorOptions_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).ContextMenu.IsOpen = true;
        }

        void ButtonExpandAllNodes_Click(object sender, RoutedEventArgs e)
        {
            XamlInspectionTreeViewInstance.ExpandAllNodes();
        }

        void SaveDisplaySize()
        {
            //-----------
            // Display size (Phone, Tablet, or Desktop)
            //-----------
            int displaySize = 0;
            if (DisplaySize_Phone.IsChecked == true)
                displaySize = 0;
            else if (DisplaySize_Tablet.IsChecked == true)
                displaySize = 1;
            else if (DisplaySize_Desktop.IsChecked == true)
                displaySize = 2;
            Properties.Settings.Default.DisplaySize = displaySize;

            //-----------
            // Phone orientation (Portrait or Landscape)
            //-----------
            int displaySize_Phone_Orientation = 0;
            if (DisplaySize_Phone_Portrait.IsChecked == true)
                displaySize_Phone_Orientation = 0;
            else if (DisplaySize_Phone_Landscape.IsChecked == true)
                displaySize_Phone_Orientation = 1;
            Properties.Settings.Default.DisplaySize_Phone_Orientation = displaySize_Phone_Orientation;

            //-----------
            // Tablet orientation (Portrait or Landscape)
            //-----------
            int displaySize_Tablet_Orientation = 0;
            if (DisplaySize_Tablet_Portrait.IsChecked == true)
                displaySize_Tablet_Orientation = 0;
            else if (DisplaySize_Tablet_Landscape.IsChecked == true)
                displaySize_Tablet_Orientation = 1;
            Properties.Settings.Default.DisplaySize_Tablet_Orientation = displaySize_Tablet_Orientation;

            // SAVE:
            Properties.Settings.Default.Save();
        }

        void LoadDisplaySize()
        {
            //-----------
            // Display size (Phone, Tablet, or Desktop)
            //-----------
            int displaySize = Properties.Settings.Default.DisplaySize;
            switch (displaySize)
            {
                case 0:
                    DisplaySize_Phone.IsChecked = true;
                    break;
                case 1:
                    DisplaySize_Tablet.IsChecked = true;
                    break;
                case 2:
                default:
                    DisplaySize_Desktop.IsChecked = true;
                    break;
            }

            //-----------
            // Phone orientation (Portrait or Landscape)
            //-----------
            int displaySize_Phone_Orientation = Properties.Settings.Default.DisplaySize_Phone_Orientation;
            switch (displaySize_Phone_Orientation)
            {
                case 1:
                    DisplaySize_Phone_Landscape.IsChecked = true;
                    break;
                case 0:
                default:
                    DisplaySize_Phone_Portrait.IsChecked = true;
                    break;
            }

            //-----------
            // Tablet orientation (Portrait or Landscape)
            //-----------
            int displaySize_Tablet_Orientation = Properties.Settings.Default.DisplaySize_Tablet_Orientation;
            switch (displaySize_Tablet_Orientation)
            {
                case 1:
                    DisplaySize_Tablet_Landscape.IsChecked = true;
                    break;
                case 0:
                default:
                    DisplaySize_Tablet_Portrait.IsChecked = true;
                    break;
            }
        }

        private class CustomResponseEventArgs : EventArgs
        {
            public string Url { get; private set; }

            public CustomResponseEventArgs(string url)
            {
                this.Url = url;
            }
        }
        private delegate void CustomResponseHandler(object sender, CustomResponseEventArgs e);

        private class CustomLoadHandler : DefaultLoadHandler
        {
            public event CustomResponseHandler CustomResponseEvent;

            public override bool OnLoad(LoadParams loadParams)
            {
                if (loadParams.Url.ToLower().StartsWith(@"http://") || loadParams.Url.ToLower().StartsWith(@"https://"))
                {
                    var customResponseEvent = CustomResponseEvent;
                    if (customResponseEvent != null)
                    {
                        customResponseEvent.Invoke(this, new CustomResponseEventArgs(loadParams.Url));
                    }
                }

                return false;
            }

            public override bool OnCertificateError(CertificateErrorParams errorParams)
            {
                Debug.WriteLine("Invalid certificate. The Simulator will ignore this error and continue. Certificate details: ");
                Certificate certificate = errorParams.Certificate;

                Debug.WriteLine("ErrorCode = " + errorParams.CertificateError);
                Debug.WriteLine("SerialNumber = " + certificate.SerialNumber);
                Debug.WriteLine("FingerPrint = " + certificate.FingerPrint);
                Debug.WriteLine("CAFingerPrint = " + certificate.CAFingerPrint);

                string subject = certificate.Subject;
                Debug.WriteLine("Subject = " + subject);

                string issuer = certificate.Issuer;
                Debug.WriteLine("Issuer = " + issuer);

                Debug.WriteLine("KeyUsages = " + String.Join(", ", certificate.KeyUsages));
                Debug.WriteLine("ExtendedKeyUsages = " + String.Join(", ", certificate.ExtendedKeyUsages));

                Debug.WriteLine("HasExpired = " + certificate.HasExpired);
                return false; //ignores the error.
            }
        }

        #region Element Picker for XAML Inspection

        void StartElementPickerForInspection()
        {
            // Show the area that is used to detect MouseMove:
            ElementPickerForInspection.Visibility = Visibility.Visible;

            if (ButtonViewHideElementPickerForInspector.IsChecked != true)
                ButtonViewHideElementPickerForInspector.IsChecked = true;

            // Show the tutorial:
            InformationAboutHowThePickerWorks.Visibility = Visibility.Visible;
        }

        void StopElementPickerForInspection()
        {
            // Hide the area that is used to detect MouseMove:
            ElementPickerForInspection.Visibility = Visibility.Collapsed;

            // Make sure the ToggleButton is in the correct state:
            if (ButtonViewHideElementPickerForInspector.IsChecked == true)
                ButtonViewHideElementPickerForInspector.IsChecked = false;

            // Remove the element picker highlight:
            XamlInspectionHelper.HighlightElement(null, ElementPickerHighlight, MainWebBrowser.Browser);

            // Hide the tutorial:
            InformationAboutHowThePickerWorks.Visibility = Visibility.Collapsed;
        }

        void ElementPickerForInspection_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Get the element at the specified position:
            var element = XamlInspectionHelper.GetElementAtSpecifiedCoordinates(e.GetPosition(MainWebBrowser));

            // Highlight the element picker (or remove highlight if null):
            XamlInspectionHelper.HighlightElement(element, ElementPickerHighlight, MainWebBrowser.Browser);
        }

        private void ElementPickerForInspection_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            StopElementPickerForInspection();

            // Get the element at the specified position:
            var element = XamlInspectionHelper.GetElementAtSpecifiedCoordinates(e.GetPosition(MainWebBrowser));

            if (element != null)
            {
                // Select the TreeNode in the Visual Tree Inspector that corresponds to the specified element:
                if (!XamlInspectionTreeViewInstance.TrySelectTreeNode(element))
                {
                    MessageBox.Show("The selected element was not found in the visual tree. Please make sure that the visual tree is up to date by clicking the 'Refresh' button in the top-right corner of the window, and try again.");
                }
            }
            else
            {
                MessageBox.Show("No item was selected by the XAML Visual Tree inspector.");
            }
        }

        private void ButtonViewHideElementPickerForInspector_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonViewHideElementPickerForInspector.IsChecked == true)
                StartElementPickerForInspection();
            else
                StopElementPickerForInspection();
        }

        #endregion


        private bool IsNetworkAvailable()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        #region profil popup

        private void ButtonLogout_Click(object sender, RoutedEventArgs e)
        {
            LicenseChecker.LogOut();
        }

        #endregion

        private void CheckBoxCORS_Checked(object sender, RoutedEventArgs e)
        {
            CrossDomainCallsHelper.IsBypassCORSErrors = true;
        }

        private void CheckBoxCORS_Unchecked(object sender, RoutedEventArgs e)
        {
            CrossDomainCallsHelper.IsBypassCORSErrors = false;
        }

        public static MainWindow Instance { get; set; }

        public static void SaveHtmlSnapshot(string fileName = null, bool osRootOnly = true, string htmlElementId = null, string xamlElementName = null)
        {
            if (fileName == null)
            {
                var elementName = htmlElementId ?? xamlElementName ?? "";
                fileName = $"HtmlSnapshot-{elementName}-" + DateTime.Now.ToString("yy.MM.dd.hh.mm.ss") + ".html";
            }
            string simulatorExePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string debuggingFolder = Path.Combine(simulatorExePath, "debugging");
            if (!Directory.Exists(debuggingFolder))
                Directory.CreateDirectory(debuggingFolder);

            File.WriteAllText(Path.Combine(debuggingFolder, fileName), Instance.getHtmlSnapshot(osRootOnly, htmlElementId, xamlElementName));
        }
    }
}
