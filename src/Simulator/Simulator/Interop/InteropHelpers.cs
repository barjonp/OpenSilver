

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



using DotNetBrowser;
using DotNetBrowser.DOM;
using System;
using System.Reflection;
using System.Windows;
using DotNetBrowser.WPF;

namespace DotNetForHtml5.EmulatorWithoutJavascript
{
    static class InteropHelpers
    {
#if OPENSILVER
        internal static void InjectIsRunningInTheSimulator_WorkAround(Assembly jsInteropAssembly)
        {
            InjectPropertyValue("IsRunningInTheSimulator_WorkAround", true, jsInteropAssembly);
        }
#endif

        internal static void InjectDOMDocument(DOMDocument document, Assembly jsInteropAssembly)
        {
            InjectPropertyValue("DOMDocument", document, jsInteropAssembly);
        }

        internal static void InjectHtmlDocument(JSValue htmlDocument, Assembly jsInteropAssembly)
        {
            InjectPropertyValue("HtmlDocument", htmlDocument, jsInteropAssembly);
        }

        internal static void InjectWebControlDispatcherBeginInvoke(WPFBrowserView webControl, Assembly jsInteropAssembly)
        {
            InjectPropertyValue("WebControlDispatcherBeginInvoke", new Action<Action>((method) => webControl.Dispatcher.BeginInvoke(method)), jsInteropAssembly);
        }

        internal static void InjectWebControlDispatcherInvoke(WPFBrowserView webControl, Assembly jsInteropAssembly)
        {
            InjectPropertyValue("WebControlDispatcherInvoke", new Action<Action, TimeSpan>((method, timeout) => webControl.Dispatcher.Invoke(method, timeout)), jsInteropAssembly);
        }

        internal static void InjectWebControlDispatcherCheckAccess(WPFBrowserView webControl, Assembly jsInteropAssembly)
        {
            InjectPropertyValue("WebControlDispatcherCheckAccess", new Func<bool>(() => webControl.Dispatcher.CheckAccess()), jsInteropAssembly);
        }

        internal static void InjectConvertBrowserResult(Func<object, object> func, Assembly jsInteropAssembly)
        {
            InjectPropertyValue("ConvertBrowserResult", func, jsInteropAssembly);
        }

        internal static void InjectJavaScriptExecutionHandler(dynamic javaScriptExecutionHandler, Assembly jsInteropAssembly)
        {
#if OPENSILVER
            InjectPropertyValue("DynamicJavaScriptExecutionHandler", javaScriptExecutionHandler, jsInteropAssembly);
#else
            InjectPropertyValue("JavaScriptExecutionHandler", javaScriptExecutionHandler, coreAssembly);
#endif
        }

        internal static void InjectWpfMediaElementFactory(Assembly jsInteropAssembly)
        {
            InjectPropertyValue("WpfMediaElementFactory", new WpfMediaElementFactory(), jsInteropAssembly);
        }

        internal static void InjectWebClientFactory(Assembly jsInteropAssembly)
        {
            InjectPropertyValue("WebClientFactory", new WebClientFactory(), jsInteropAssembly);
        }

        internal static void InjectClipboardHandler(Assembly jsInteropAssembly)
        {
            InjectPropertyValue("ClipboardHandler", new ClipboardHandler(), jsInteropAssembly);
        }

        internal static void InjectSimulatorProxy(SimulatorProxy simulatorProxy, Assembly jsInteropAssembly)
        {
            InjectPropertyValue("SimulatorProxy", simulatorProxy, jsInteropAssembly);
        }

        internal static dynamic GetPropertyValue(string propertyName, Assembly coreAssembly)
        {
            var typeInCoreAssembly = coreAssembly.GetType("DotNetForHtml5.Core.INTERNAL_Simulator");
            if (typeInCoreAssembly != null)
            {
                PropertyInfo staticProperty = typeInCoreAssembly.GetProperty(propertyName);
                if (staticProperty != null)
                {
                    return staticProperty.GetValue(null);
                }
                else
                {
                    MessageBox.Show("ERROR: Could not find the public static property \"" + propertyName + "\" in the type \"INTERNAL_Simulator\" in the core assembly.");
                    return null;
                }
            }
            else
            {
                MessageBox.Show("ERROR: Could not find the type \"INTERNAL_Simulator\" in the core assembly.");
                return null;
            }
        }

        static void InjectPropertyValue(string propertyName, object propertyValue, Assembly jsInteropAssembly)
        {
            Type typeInCoreAssembly = jsInteropAssembly.GetType("DotNetForHtml5.Core.INTERNAL_Simulator");
            if (typeInCoreAssembly == null)
            {
                MessageBox.Show("ERROR: Could not find the type \"INTERNAL_Simulator\" in the core assembly.");
                return;
            }

            PropertyInfo staticProperty = typeInCoreAssembly.GetProperty(propertyName);
            if (staticProperty == null)
            {
                MessageBox.Show("ERROR: Could not find the public static property \"" + propertyName + "\" in the type \"INTERNAL_Simulator\" in the core assembly.");
                return;
            }

            staticProperty.SetValue(null, propertyValue);
        }

        internal static void InjectCodeToDisplayTheMessageBox(
            Func<string, string, bool, bool> codeToShowTheMessageBoxWithTitleAndButtons,
            Assembly coreAssembly)
        {
            Type type = coreAssembly.GetType("Windows.UI.Xaml.MessageBox");
            if (type == null)
            {
                type = coreAssembly.GetType("System.Windows.MessageBox"); // For "SL Migration" projects.
            }
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

        internal static void RaiseReloadedEvent(Assembly coreAssembly)
        {
            var type = coreAssembly.GetType("Windows.UI.Xaml.Application");
            if (type == null)
                type = coreAssembly.GetType("System.Windows.Application");
            var method = type.GetMethod("INTERNAL_RaiseReloadedEvent", BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null, null);
        }
    }
}
