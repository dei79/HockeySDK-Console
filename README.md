HockeySDK-Console
=================

A wrapper for the HockeySDK-Core for Windows to use them in commandline applications and windows forms applications.

Install the package via NuGet

```
Install-Package HockeySDK-Console
```

Add the following app setting into the app.config 

```xml
<add key="hockeyapp.appid" value="5d3179c84f59c017ee602664170a3fdd" />
```

After that just encapsulate the code fragment you want to observe as follows:

```c#
using (var hockeyApp = new HockeyApp.ConsoleCrashHandler())
{

}
``` 
