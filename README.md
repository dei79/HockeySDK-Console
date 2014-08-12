HockeySDK-Console
=================

A wrapper for the HockeySDK-Core for Windows to use them in commandline applications and windows forms applications.

Install the package via NuGet

```
Install-Package HockeySDK-Console
```

Add the following app setting into the app.config 

```xml
<add key="hockeyapp.appid" value="<<YOUR HOCKEYAPP ID>>" />
```

After that just encapsulate the code fragment you want to observe as follows:

```c#
using (var hockeyApp = new HockeyApp.ConsoleCrashHandler())
{

}
``` 
