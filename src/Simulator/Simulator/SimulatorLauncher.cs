using DotNetForHtml5.EmulatorWithoutJavascript;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenSilver.Simulator
{
    public static class SimulatorLauncher
    {
        public static int Start(Type userApplicationType, SimulatorLaunchParameters parameters = null)
        {
            if (userApplicationType == null)
            {
                throw new ArgumentNullException(nameof(userApplicationType));
            }

            return Start(() => Activator.CreateInstance(userApplicationType), userApplicationType.Assembly, parameters);
        }

        public static int Start(Action appCreationDelegate, Assembly appAssembly, SimulatorLaunchParameters parameters = null)
        {
            if (appCreationDelegate == null)
            {
                throw new ArgumentNullException(nameof(appCreationDelegate));
            }

            if (appAssembly == null)
            {
                throw new ArgumentNullException(nameof(appAssembly));
            }

            App app = new App();
            app.InitializeComponent();
            return app.Run(new MainWindow(appCreationDelegate, appAssembly, parameters));
        }
    }
}

