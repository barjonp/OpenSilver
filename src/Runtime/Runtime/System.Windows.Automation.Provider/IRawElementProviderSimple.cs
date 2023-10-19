
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

using System.Diagnostics;
using System.Windows.Automation.Peers;

namespace System.Windows.Automation.Provider
{
	/// <summary>
	/// Provides methods and properties that expose basic information about a UI element.
	/// </summary>
	public sealed class IRawElementProviderSimple
	{
		internal IRawElementProviderSimple(AutomationPeer peer)
        {
			Debug.Assert(peer != null);

			Peer = peer; 
        }

		internal AutomationPeer Peer { get; }
	}
}
