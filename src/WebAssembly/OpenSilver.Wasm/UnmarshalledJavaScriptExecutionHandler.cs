
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

using DotNetForHtml5;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace OpenSilver.WebAssembly
{
    public sealed class UnmarshalledJavaScriptExecutionHandler : IJavaScriptExecutionHandler2
    {
        private const string MethodName = "callJSUnmarshalled";

        private readonly WebAssemblyJSRuntime _runtime;

        public UnmarshalledJavaScriptExecutionHandler(IJSRuntime runtime)
        {
            if (runtime is not WebAssemblyJSRuntime wasmJSRuntime)
            {
                throw new ArgumentException($"'runtime' should be of type '{typeof(WebAssemblyJSRuntime)}'.", nameof(runtime));
            }

            _runtime = wasmJSRuntime;
        }

        public void ExecuteJavaScript(string js)
            => _runtime.InvokeUnmarshalled<string, object>(MethodName, js);

        public object ExecuteJavaScriptWithResult(string js)
            => _runtime.InvokeUnmarshalled<string, object>(MethodName, js);

        public TResult InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0)
            => _runtime.InvokeUnmarshalled<T0, TResult>(identifier, arg0);
    }
}