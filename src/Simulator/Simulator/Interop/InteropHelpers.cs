

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
using System.Reflection;
using System.Windows;
using DotNetBrowser;
using DotNetForHtml5.Core;
using DotNetBrowser.DOM;
using DotNetBrowser.WPF;
using System.Linq;
using DotNetForHtml5.Compiler;

namespace DotNetForHtml5.EmulatorWithoutJavascript
{
    internal static class InteropHelpers
    {
        internal static void InjectIsRunningInTheSimulator_WorkAround()
        {
            INTERNAL_Simulator.IsRunningInTheSimulator_WorkAround = true;
        }

        internal static void InjectDOMDocument(DOMDocument document)
        {
            INTERNAL_Simulator.DOMDocument = document;
        }

        internal static void InjectHtmlDocument(JSValue htmlDocument)
        {
            INTERNAL_Simulator.HtmlDocument = htmlDocument;
        }

        internal static void InjectWebControlDispatcherBeginInvoke(WPFBrowserView webControl)
        {
            INTERNAL_Simulator.WebControlDispatcherBeginInvoke = 
                new Action<Action>((method) => webControl.Dispatcher.BeginInvoke(method));
        }

        internal static void InjectWebControlDispatcherInvoke(WPFBrowserView webControl)
        {
            INTERNAL_Simulator.WebControlDispatcherInvoke =
                new Action<Action, TimeSpan>((method, timeout) => webControl.Dispatcher.Invoke(method, timeout));
        }

        internal static void InjectWebControlDispatcherCheckAccess(WPFBrowserView webControl)
        {
            INTERNAL_Simulator.WebControlDispatcherCheckAccess = new Func<bool>(() => webControl.Dispatcher.CheckAccess());
        }

        internal static void InjectConvertBrowserResult(Func<object, object> func)
        {
            INTERNAL_Simulator.ConvertBrowserResult = func;
        }

        internal static void InjectJavaScriptExecutionHandler(dynamic javaScriptExecutionHandler)
        {
            INTERNAL_Simulator.DynamicJavaScriptExecutionHandler = javaScriptExecutionHandler;
        }

        internal static void InjectWpfMediaElementFactory()
        {
            INTERNAL_Simulator.WpfMediaElementFactory = new WpfMediaElementFactory();
        }

        internal static void InjectWebClientFactory()
        {
            INTERNAL_Simulator.WebClientFactory = new WebClientFactory();
        }

        internal static void InjectClipboardHandler()
        {
            INTERNAL_Simulator.ClipboardHandler = new ClipboardHandler();
        }

        internal static void InjectSimulatorProxy(SimulatorProxy simulatorProxy)
        {
            INTERNAL_Simulator.SimulatorProxy = simulatorProxy;
        }

        internal static void InjectCodeToDisplayTheMessageBox(Func<string, string, bool, bool> codeToShowTheMessageBoxWithTitleAndButtons)
        {
            Type type = ReflectionInUserAssembliesHelper.GetTypeFromCoreAssembly("Windows.UI.Xaml.MessageBox")
                ?? ReflectionInUserAssembliesHelper.GetTypeFromCoreAssembly("System.Windows.MessageBox");
            if (type != null)
            {
                PropertyInfo staticProperty = type.GetProperty("INTERNAL_CodeToShowTheMessageBoxWithTitleAndButtons");
                if (staticProperty != null)
                {
                    staticProperty.SetValue(null, codeToShowTheMessageBoxWithTitleAndButtons);
                }
                else
                {
                    MessageBox.Show("ERROR: Could not find the public static property \"INTERNAL_CodeToShowTheMessageBoxWithTitleAndButtons\" in the type \"MessageBox\" in the core assembly.");
                }
            }
            else
            {
                MessageBox.Show("ERROR: Could not find the type \"MessageBox\" in the core assembly.");
            }
        }

        internal static void RaiseReloadedEvent()
        {
            var type = ReflectionInUserAssembliesHelper.GetTypeFromCoreAssembly("Windows.UI.Xaml.Application")
                ?? ReflectionInUserAssembliesHelper.GetTypeFromCoreAssembly("System.Windows.Application");
            var method = type.GetMethod("INTERNAL_RaiseReloadedEvent", BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null, null);
        }
    }
}
