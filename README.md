visualstudio-wakatime
=====================

WakaTime is a productivity & time tracking tool for programmers. Once the WakaTime plugin is installed, you get a dashboard with reports about your programming by time, language, project, and branch.


Installation
------------

1. Inside Visual Studio, navigate to `Tools` -> `Extensions and Updates...`

2. Click the `Online` category at the left, then search for `wakatime`.

3. Click the `Download` button, then click `Install`.

4. Click the `Restart Now` button.

3. Enter your [api key](https://wakatime.com/settings#apikey), then press `enter`.

4. Use Visual Studio like you normally do and your time will be tracked for you automatically.

5. Visit https://wakatime.com to see your logged time.


Screen Shots
------------

![Project Overview](https://wakatime.com/static/img/ScreenShots/ScreenShot-2014-10-29.png)


Supported Visual Studio Editions
--------------------------------

* Visual Studio 2012 Professional
* Visual Studio 2012 Premium
* Visual Studio 2012 Ultimate
* Visual Studio 2013 Professional
* Visual Studio 2013 Premium
* Visual Studio 2013 Ultimate
* Visual Studio 2015 Community
* Visual Studio 2015 Professional
* Visual Studio 2015 Enterprise

#### Visual Studio Express

Microsoft [does not allow](https://visualstudiomagazine.com/articles/2014/05/21/no-extensions-for-visual-studio-express.aspx) extensions for Visual Studio Express edition in the gallery. To use WakaTime for Visual Studio Express, clone the github repo and build the extension using the `Express` build profile. The resulting `bin/Express/WakaTime.vsix` extension file will install into Visual Studio Express when run.


Contributing
------------

To open and build this project, please use Visual Studio 2013 and install the Visual Studio 2013 SDK:

https://www.microsoft.com/en-us/download/details.aspx?id=40758

To Debug follow this instructions:

1. Open Wakatime project properties (ALT + ENTER)
2. Into Debug tab set to Start external program. e.g: ```C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe```
3. Add command line arguments: ```/rootsuffix Exp```
