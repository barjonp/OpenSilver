
/*===================================================================================
* 
*   Copyright (c) Userware/OpenSilver.net
*      
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/

using System;

namespace DotNetForHtml5.Core
{
    public static class INTERNAL_Simulator
    {
        // Note: all the properties here are populated by the Simulator, which "injects" stuff here when the application is launched in the Simulator.

        public static dynamic HtmlDocument { internal get; set; }

        // Here we get the Document from DotNetBrowser
        public static dynamic DOMDocument { internal get; set; }

        // BeginInvoke of the WebControl's Dispatcher
        public static Action<Action> WebControlDispatcherBeginInvoke { internal get; set; }

        // Invoke of the WebControl's Dispatcher
        public static Action<Action, TimeSpan> WebControlDispatcherInvoke { internal get; set; }

        /// <summary>
        /// CheckAccess() of WebControl's Dispatcher.
        /// </summary>
        public static Func<bool> WebControlDispatcherCheckAccess { get; internal set; }
        
        public static IJavaScriptExecutionHandler JavaScriptExecutionHandler
        {
            get => JavaScriptExecutionHandler2;
            set
            {
                IJavaScriptExecutionHandler2 jsRuntime = null;
                if (value is not null)
                {
                    jsRuntime = value as IJavaScriptExecutionHandler2 ?? new JSRuntimeWrapper(value);
                }

                JavaScriptExecutionHandler2 = jsRuntime;
            }
        }

        // Intended to be injected when the app is initialized.
        internal static IJavaScriptExecutionHandler2 JavaScriptExecutionHandler2 { get; set; }

        public static dynamic DynamicJavaScriptExecutionHandler { internal get; set; }

        public static dynamic WpfMediaElementFactory { internal get; set; }

        public static dynamic WebClientFactory { get; set; }

        public static dynamic ClipboardHandler { internal get; set; }

        public static dynamic SimulatorProxy { internal get; set; }

        // In OpenSilver Version, we use this work-around to know if we're in the simulator
        public static bool IsRunningInTheSimulator_WorkAround { get; set; }

        public static Func<object, object> ConvertBrowserResult { get; set; }
    }
}
