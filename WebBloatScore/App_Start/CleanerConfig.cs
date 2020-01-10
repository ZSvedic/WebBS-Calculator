using System;
using System.IO;
using System.Threading;
using System.Web.Hosting;

namespace WebBloatScore
{
    public sealed class CleanerConfig : IRegisteredObject, IDisposable
    {
        private static CleanerConfig Instance;

        private readonly DirectoryInfo cleanerDirectory;
        private readonly object locker;
        private bool isShutingDown;
        private Timer timer;

        public static void RegisterCleaner() { Instance = new CleanerConfig(); }

        private CleanerConfig()
        {
            this.cleanerDirectory = new DirectoryInfo(Utilities.ScreenshotsPath);

            this.locker = new object();
            this.isShutingDown = false;

            this.timer = new Timer(this.Cleanup);
            this.timer.Change(TimeSpan.Zero, TimeSpan.FromHours(Utilities.ScreenshotsLifetime));

            HostingEnvironment.RegisterObject(this);
        }

        public void Stop(bool immediate)
        {
            lock (locker)
            {
                this.Dispose();
                this.isShutingDown = true;
            }

            HostingEnvironment.UnregisterObject(this);
        }

        private void Cleanup(object sender)
        {
            lock (locker)
            {
                if (this.isShutingDown)
                    return;

                DateTime nonCachedExpirationTime = DateTime.Now.AddHours(- Utilities.ScreenshotsLifetime);
                DateTime cachedExpirationTime = DateTime.Now.AddHours(- (Utilities.CachedScreenshotsLifetime + 24)); // rather keep it there one day more so it doesn't happen that this expires sooner than cache
                foreach (FileInfo file in this.cleanerDirectory.GetFiles())
                {
                    var expirationTime = IsCached(file) ? cachedExpirationTime : nonCachedExpirationTime;

                    if (file.CreationTime < expirationTime)
                        file.Delete();
                }
                    
            }
        }

        private bool IsCached(FileInfo file)
        {
            return file.Name.Contains(Utilities.CachedItemNameStart);
        }

        public void Dispose()
        {
            if (this.timer != null)
                this.timer.Dispose();
            this.timer = null;
        }
    }
}