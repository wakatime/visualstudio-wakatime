visualstudio-wakatime
=====================

Visual Studio extension to quantify your coding using https://wakatime.com/.


Installation
------------

1. Inside Visual Studio, navigate to `Tools` → `Extensions and Updates...`

2. Click the `Online` category at the left, then search for `wakatime`.

3. Click the `Download` button, then click `Install`.

4. Click the `Restart Now` button.

3. Enter your [api key](https://wakatime.com/settings#apikey), then press `enter`.

4. Use Visual Studio and your coding activity will be displayed on your [WakaTime dashboard](https://wakatime.com).


Screen Shots
------------

![Project Overview](https://wakatime.com/static/img/ScreenShots/Screen-Shot-2016-03-21.png)


Supported Visual Studio Editions
--------------------------------

* Visual Studio 2010
* Visual Studio 2012
* Visual Studio 2013
* Visual Studio 2015
* Visual Studio 2017


#### Visual Studio 2010 ( Legacy )

For legacy Visual Studio 2010, use the [legacy WakaTime 2010 extension][legacy extension].


#### Visual Studio Express

Microsoft [does not allow][express article] extensions for Visual Studio Express edition in the gallery.
To use WakaTime for Visual Studio Express, download and install [WakaTime for Express][latest release].

Alternatively, you may clone the github repo and build the extension using the `Express` build profile.
The resulting `bin/Express/WakaTime.vsix` extension file will install into Visual Studio Express when run.


Contributing
------------

To open and build this project, please use Visual Studio 2017.

For debugging, configure the product to open in a new Visual Studio instance:

1. Open the project properties (ALT + ENTER)
2. In the Debug tab, set to Start external program. e.g: ```C:\Program Files (x86)\Microsoft Visual Studio 15.0\Common7\IDE\devenv.exe```
3. Add command line arguments: ```/rootsuffix Exp```

[latest release]: https://github.com/wakatime/visualstudio-wakatime/releases/latest
[legacy extension]: https://marketplace.visualstudio.com/items?itemName=WakaTime.WakaTime2010
[express article]: https://visualstudiomagazine.com/articles/2014/05/21/no-extensions-for-visual-studio-express.aspx
