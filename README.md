# visualstudio-wakatime

[![Build Status](https://ci.appveyor.com/api/projects/status/pwyxjwo2q78ao3jl/branch/master?svg=true)](https://ci.appveyor.com/project/alanhamlett/visualstudio-wakatime/branch/master)
[![Coding time tracker](https://wakatime.com/badge/github/wakatime/visualstudio-wakatime.svg)](https://wakatime.com/badge/github/wakatime/visualstudio-wakatime)

Visual Studio extension to quantify your coding using <https://wakatime.com/>.

## Installation

1. Inside Visual Studio, navigate to `Tools` → `Extensions...`
2. Click the `Online` category at the left, then search for `wakatime`.
3. Click the `Download` button, then click `Install`.
4. Click the `Restart Now` button.
5. Enter your [api key](https://wakatime.com/settings#apikey), then press `enter`.
6. Use Visual Studio and your coding activity will be displayed on your [WakaTime dashboard](https://wakatime.com).

## Screen Shots

![Project Overview](Dev17/Assets/dashboard.png)

## Supported Visual Studio Editions

* Visual Studio 2015
* Visual Studio 2017
* Visual Studio 2019
* Visual Studio 2022

## Visual Studio for Mac

Visual Studio for Mac is supported with the [WakaTime for Monodevelop extension][monodevelop].

## Visual Studio Express

Microsoft [does not allow][express article] extensions for Visual Studio Express edition in the gallery.
To use WakaTime for Visual Studio Express, download and install [WakaTime for Express][latest release].

Alternatively, you may clone the github repo and build the extension using the `Express` build profile.
The resulting `bin/Express/WakaTime.vsix` extension file will install into Visual Studio Express when run.

## Contributing

To open and build this project, please use Visual Studio 2019.

For debugging, configure the product to open in a new Visual Studio instance:

1. Open the project properties (ALT + ENTER)
2. In the Debug tab, set to Start external program. e.g: ```C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\devenv.exe```
3. Add command line arguments: ```/rootsuffix Exp```
4. Change the solution according to the version of Visual Studio you're targeting:

* Release - Visual Studio 2015+

> Note: The Debug solution is only for including debugger symbols.

[latest release]: https://github.com/wakatime/visualstudio-wakatime/releases/latest
[legacy extension]: https://marketplace.visualstudio.com/items?itemName=WakaTime.WakaTime2010
[express article]: https://visualstudiomagazine.com/articles/2014/05/21/no-extensions-for-visual-studio-express.aspx

## Troubleshooting

Look for a `Tools` → `WakaTime Settings` menu in Visual Studio.
If that menu doesn't exist, something prevented the WakaTime extension from loading.

Turn on debug mode from `Tools` → `WakaTime Settings`.

Are there error messages in your Visual Studio Output window?

Open the Output window from `View` → `Output` (`ctrl` + `alt` + `O`).

![Output Window](Dev17/Assets/output-window.png)

If there are no messages in your Visual Studio Output window, check your `.wakatime.log` file:

`C:\Users\<user>\.wakatime.log`

Lastly, uncaught exceptions go to [ActivityLog.xml][activitylog]. Uncaught exceptions are rare, so check your ActivityLog.xml only after checking your Output Window and `.wakatime.log` file.

The [How to Debug Plugins][how to debug] guide shows how to check when coding activity was last received from your IDE using the [User Agents API][user agents api].
For more general troubleshooting info, see the [wakatime-cli Troubleshooting Section][wakatime-cli-help].

[wakatime-cli-help]: https://github.com/wakatime/wakatime#troubleshooting
[how to debug]: https://wakatime.com/faq#debug-plugins
[user agents api]: https://wakatime.com/developers#user_agents
[monodevelop]: https://wakatime.com/help/plugins/monodevelop
[activitylog]: http://blogs.msdn.com/b/visualstudio/archive/2010/02/24/troubleshooting-with-the-activity-log.aspx
