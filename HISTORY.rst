
History
-------


5.0.4 (2015-07-01)
++++++++++++++++++

- support for VS2012 by changing the version o Microsoft.VisualStudio.Shell
- correct priority for project detection
- fix offline logging
- limit language detection to known file extensions, unless file contents has a vim modeline
- guess language using multiple methods, then use most accurate guess
- use entity and type for new heartbeats api resource schema
- upgrade wakatime cli to v4.1.0


5.0.3 (2015-06-08)
++++++++++++++++++

- look for Python binary location in Windows registry
- added debug option into SettingsForm


5.0.2 (2015-06-05)
++++++++++++++++++

- detect python binary from successful execution of python, without checking output


5.0.1 (2015-06-01)
++++++++++++++++++

- update wakatime cli to v4.0.14
- correctly log message from py.warnings module


5.0.2 (2015-06-05)
++++++++++++++++++

- detect python binary from successful execution of python, without checking output


5.0.1 (2015-06-01)
++++++++++++++++++

- update wakatime cli to v4.0.14
- correctly log message from py.warnings module


5.0.2 (2015-06-05)
++++++++++++++++++

- detect python binary from successful execution of python, without checking output


5.0.1 (2015-06-01)
++++++++++++++++++

- update wakatime cli to v4.0.14
- correctly log message from py.warnings module


5.0.0 (2015-05-30)
++++++++++++++++++

- better UX around api key and settings form
- cache Python binary location and wakatime cli location for better performance
- move wakatime cli dependency into AppData folder
- proxy field added to settings form


4.0.4 (2015-05-24)
++++++++++++++++++

- support for Visual Studio 2012


4.0.2 (2015-05-11)
++++++++++++++++++

- more changes for extension gallery


4.0.1 (2015-05-08)
++++++++++++++++++

- changes for extension gallery


4.0.0 (2015-05-08)
++++++++++++++++++

- support for Visual Studio 2015


3.0.0 (2015-04-29)
++++++++++++++++++

- refactor plugin code and fix major bugs
- support for Visual Studio 2013


2.0.2 (2014-12-21)
++++++++++++++++++

- wrap wakatime cli in quotes when executing
- use solution name as backup for project name
- send hearbeat every 2 minutes when activity detected in IDE


2.0.1 (2014-12-20)
++++++++++++++++++

- only send heartbeats when actively using IDE, not when idle
- send heartbeat asyncronously


2.0.0 (2014-12-20)
++++++++++++++++++

- fix logging
- correctly log heartbeats from IDE activity
- correctly detect Python binary
- download and install python if not already installed


1.0.0 (2014-12-18)
++++++++++++++++++

- Birth

