using HockeyApp.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HockeyApp
{
    public class ConsoleCrashHandler : IDisposable
    {
        private String _packageName { get; set; }
        private String _applicationId { get; set; }
        private String _crashFileLocaton { get; set; }
        private String _packageVersion { get; set; }
        private Boolean _disposed { get; set; }
        private IAppCrashNotifier _appCrashNotifier { get; set; }

        /// <summary>
        /// The default constructor reads the basic information per reflection from the assembly as self and the 
        /// application identifier from the app-settings. The keys is "hockeyapp.appid"
        /// </summary>
        public ConsoleCrashHandler()
        {
            _packageName = Assembly.GetEntryAssembly().GetName().Name;
            _applicationId = ConfigurationManager.AppSettings["hockeyapp.appid"];

            InitializeHockeyAppClient();
        }

        /// <summary>
        /// This constructor generates the packagename from the assembly via reflection but allows to set a 
        /// specific application id
        /// </summary>
        /// <param name="applicationId"></param>
        public ConsoleCrashHandler(String applicationId)
        {
            _packageName = Assembly.GetExecutingAssembly().GetName().Name;
            _applicationId = applicationId;

            InitializeHockeyAppClient();
        }

        /// <summary>
        /// This constructor allows to set a specific package name and application identifier manually. No magic
        /// at all
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="applicationId"></param>
        public ConsoleCrashHandler(String packageName, String applicationId)
        {
            _packageName    = packageName;
            _applicationId  = applicationId;

            InitializeHockeyAppClient();
        }

        /// <summary>
        /// This constructor allows to set a specific package name and application identifier manually,
        /// as well as to specify a custom location for recording of application crashes, and an object implementation
        /// that notifies a user on subsequent launch that there is crash data to be logged. No magic at all.
        /// </summary>
        /// <param name="packageName">Package Name (as recorded in HockeyApp)</param>
        /// <param name="applicationId">Application ID as assigned by HockeyApp</param>
        /// <param name="crashFileLocation">Directory path to location where crash files should be output to</param>
        /// <param name="appCrashNotifier">Object implementation advising user in next application session of crash data to be uploaded</param>
        public ConsoleCrashHandler(String packageName, String applicationId, String crashFileLocation, IAppCrashNotifier appCrashNotifier)
        {
            _packageName      = packageName;
            _applicationId    = applicationId;

            // If it so happens that crashFileLocation is null/empty-string, InitializeHockeyAppClient will set this to a default value
            _crashFileLocaton = crashFileLocation;

            _appCrashNotifier = appCrashNotifier;

            InitializeHockeyAppClient();
        }

        public void Dispose()
        {
            // disable the crash handler becase removing the delegate is currently
            // not supported 
            _disposed = true;            
        }

        private void InitializeHockeyAppClient()
        {
            // reset the disposed value
            _disposed = false;

            // generate the assembly version 
            _packageVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

            // init the client
            HockeyClient.Configure(_applicationId, _packageVersion);

            // configure the location of crash dumps 
            if (string.IsNullOrEmpty(_crashFileLocaton))
            {
                _crashFileLocaton = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _crashFileLocaton = Path.Combine(_crashFileLocaton, Assembly.GetEntryAssembly().GetName().Name);
                _crashFileLocaton = Path.Combine(_crashFileLocaton, "HockeyCrashLogs"); 
            }

            // add the crash handler
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

            // try to push existing crash logs
            List<String> failedReports = null;
            TryPushCrashLogs(out failedReports);
        }

        private Boolean TryPushCrashLogs(out List<String> failedReports)
        {
            // reset the failed reports
            failedReports = null;

            // check if we have a crash file directory, if not nothing todo 
            if (!Directory.Exists(_crashFileLocaton))
            {                
                return true;
            }

            var files = Directory.GetFiles(_crashFileLocaton, "*.log");

            if ((files != null) && (files.GetLength(0) > 0) && ((_appCrashNotifier == null) || (_appCrashNotifier.ConfirmUploadCrashData())))
            {
                // 
                // Visit every file and send to the backend
                foreach (string filename in files)
                {
                    try
                    {
                        FileStream fs = File.OpenRead(filename);
                        ICrashData cd = HockeyClient.Instance.Deserialize(fs);
                        fs.Close();
                        cd.SendDataAsync().Wait();
                        File.Delete(filename);
                    }
                    catch (Exception)
                    {
                        // create a failed report list if needed
                        if (failedReports == null)
                            failedReports = new List<string>();

                        // add the missing report
                        failedReports.Add(filename);
                    }
                } 
            }
            
            // done
            return (failedReports == null);
        }

        private void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            // check if we are allowed to handle exceptions
            if (_disposed)
                return;

            // the exception handler as self should never throw an exception so that the flow is 
            // not interrupted by some bad code here             
            try
            {
                // We need to ensure that the crash file location realy exists before we start 
                // writing a file into it
                if (!Directory.Exists(_crashFileLocaton))
                    Directory.CreateDirectory(_crashFileLocaton);

                // Every exception needs a unique identifier
                string crashID = Guid.NewGuid().ToString();

                // The filename is generated by the identifier 
                String filename = String.Format("{0}.log", crashID);

                // generate the full filename
                filename = Path.Combine(_crashFileLocaton, filename);

                // Generate the model HockeyApp is using for logging crashes
                CrashLogInformation logInfo = new CrashLogInformation()
                {
                    PackageName     = _packageName,
                    Version         = _packageVersion,
                    OperatingSystem = Environment.OSVersion.ToString(),
                };

                
                // Now it's time to build a real creash report and write them to the file
                ICrashData crash = HockeyClient.Instance.CreateCrashData(e.ExceptionObject as Exception, logInfo);
                using (FileStream stream = File.Create(filename))
                {
                    crash.Serialize(stream);
                }                               
            }
            catch (Exception)
            { 
                // Ignore all other exceptions
            }
        }
    }
}




