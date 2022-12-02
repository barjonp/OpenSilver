
This repository contains the source code of **OpenSilver** (www.opensilver.net), an open source reimplementation of Silverlight that runs in modern browsers with WebAssembly.

The main branches are:
- **develop**: this branch is where day to day development occurs
- **master**: this branch corresponds to the version of the packages that are on nuget.org


# How to download the software and get started?

Read the "[Getting Started](http://doc.opensilver.net/documentation/general/getting-started-tour.html)" page of the OpenSilver [documentation](http://doc.opensilver.net/) for a step-by-step tutorial.

Basically, you should download the .VSIX file (the extension for Microsoft Visual Studio) which installs the Project Templates. It can be downloaded at: https://opensilver.net/download.aspx (Free, Open Source, MIT Licensed)

Then, launch Visual Studio, click "Create a new project", and choose one of the installed templates.

After creating the project, you may then want to update the NuGet package to reference the very latest version (note: in the NuGet Package Manager, be sure to check the option "include pre-releases", otherwise you may not see the latest package version).



# How to build the source code in this repository?

1. **Update Visual Studio:** Make sure you are using the very latest version of Visual Studio 2019. To check for updates, please launch the Visual Studio Installer from your Start Menu.

2. **Clone the repo:** Clone this repository locally or download it as a ZIP archive and extract it on your machine

3. **Launch Developer Command Prompt:** Open the "Developer Command Prompt for VS 2019" (or newer) from your Start Menu

4. **Run the compilation .BAT:** Navigate to the **build/BatFiles** folder and launch the file "**SL_Build.bat**" *(or "**UWP_Build.bat**" depending on which configuration of OpenSilver you are interested by)* with an unique argument in the form of a valid version identifier.

For convenience, instead of building the whole packages as instructed above, you can alternatively build only the Runtime DLLs. To do so, open the solution file "OpenSilver.sln" and choose the appropriate "Solution Configuration" (see below) 

# What are those solution configurations?

- **SL**: Uses the Silverlight-like dialect of XAML.
- **UWP**: Uses the UWP-like dialect of XAML.

# What if I get a compilation error with the code in this repository?

If you get a compilation error, it may be that a Visual Studio workload needs to be installed. To find out, please open the solution "OpenSilver.sln" and attempt to compile it with the Visual Studio IDE (2019 or newer).

If you are compiling using the Command Prompt, please double-check that you are using the "Developer Command Prompt", not the standard Command Prompt, and that the current directory is set to the "build" directory of this repository, because some paths in the .BAT files may be relative to the current directory.

If you still encounter any issues, please contact the OpenSilver team at: https://opensilver.net/contact.aspx





