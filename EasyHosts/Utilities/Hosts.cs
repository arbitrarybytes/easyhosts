using System;

namespace EasyHosts.Utilities
{
    public static class Hosts
    {
        private static readonly string _hostFile;
        private static readonly string _hostFileDir;
        private const string HostsDir = "drivers\\etc";

        static Hosts()
        {
            _hostFileDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), HostsDir);
            _hostFile = System.IO.Path.Combine(_hostFileDir, "hosts");
        }

        public static bool UpdateHostsFile(string content)
        {
            System.IO.File.WriteAllText(_hostFile, content);
            return true;
        }

        #region :: Properties ::

        public static string HostFilePath
        {
            get
            {
                return _hostFile;
            }
        }

        /// <summary>
        /// Gets the system's hosts file directory
        /// </summary>
        public static string HostFileDirectory
        {
            get
            {
                return _hostFileDir;
            }
        }

        /// <summary>
        /// Gets whether the system has an active hosts file
        /// </summary>
        public static bool HostFileExists
        {
            get
            {
                return System.IO.File.Exists(_hostFile);
            }
        }

        #endregion
    }
}
