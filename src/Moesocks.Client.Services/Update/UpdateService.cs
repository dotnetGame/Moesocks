using Microsoft.Extensions.Options;
using Moesocks.Client.Services.Configuration;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Client.Services.Update
{
    class UpdateService : IUpdateService
    {
        private readonly UpdateConfiguration _update;
        private CancellationTokenSource _cts;
        private readonly Timer _checkUpdate;
        private readonly TimeSpan _checkPeriod = TimeSpan.FromHours(1);
        private readonly GitHubClient _github;
        private readonly Version _currentVersion;
        private FileInfo _updatePack = null;
        private string _newVersion;

        public event EventHandler<NewReleaseFoundEventArgs> NewReleaseFound;

        protected bool CanUpdate => _updatePack == null && _update.Enabled && NetworkInterface.GetIsNetworkAvailable();

        public UpdateService(IOptions<UpdateConfiguration> updateConfig, IProductInformation productInfo)
        {
            _currentVersion = productInfo.ProductVersion;
            _github = new GitHubClient(new ProductHeaderValue(productInfo.ProductName, _currentVersion.ToString()));
            _checkUpdate = new Timer(CheckUpdate, this, Timeout.InfiniteTimeSpan, _checkPeriod);
            _update = updateConfig.Value;
            _update.PropertyChanged += update_PropertyChanged;
        }

        private void update_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateJob();
        }

        private void UpdateJob()
        {
            if (!CanUpdate)
            {
                var oldCts = Interlocked.Exchange(ref _cts, null);
                if (oldCts != null)
                {
                    oldCts.Cancel();
                    _checkUpdate.Change(Timeout.InfiniteTimeSpan, _checkPeriod);
                    _cts = null;
                }
            }
            else
            {
                var cts = new CancellationTokenSource();
                if (Interlocked.CompareExchange(ref _cts, cts, null) == null)
                    _checkUpdate.Change(TimeSpan.Zero, _checkPeriod);
            }
            UpdateProduct();
        }

        private static async void CheckUpdate(object me)
        {
            var service = (UpdateService)me;
            try
            {
                var token = service._cts.Token;
                token.ThrowIfCancellationRequested();
                if (service.CanUpdate)
                {
                    var releaseClient = service._github.Repository.Release;
                    var latest = await releaseClient.GetLatest("sunnycase", "Moesocks");
                    token.ThrowIfCancellationRequested();
                    var latestVersion = Version.Parse(latest.TagName);
                    if (latestVersion > service._currentVersion)
                    {
                        var asset = latest.Assets.FirstOrDefault(o => o.Name == "client.bin.zip");
                        if (asset != null)
                        {
                            var updatePackFile = CreateUpdatePackFile(latestVersion);
                            if (!updatePackFile.Exists || updatePackFile.Length != asset.Size)
                            {
                                updatePackFile.Directory.Create();
                                using (var updatePack = updatePackFile.OpenWrite())
                                using (var client = new HttpClient())
                                {
                                    token.ThrowIfCancellationRequested();
                                    var source = await client.GetStreamAsync(asset.BrowserDownloadUrl);
                                    token.ThrowIfCancellationRequested();
                                    await source.CopyToAsync(updatePack);
                                }
                            }
                            token.ThrowIfCancellationRequested();
                            service._newVersion = latest.Name;
                            service._updatePack = updatePackFile;
                            service.UpdateProduct();
                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
                Interlocked.Exchange(ref service._cts, null);
            }
        }

        private void UpdateProduct()
        {
            var pack = _updatePack;
            if (pack != null)
                NewReleaseFound?.Invoke(this, new NewReleaseFoundEventArgs(_newVersion, pack));
        }

        private static FileInfo CreateUpdatePackFile(Version latestVersion)
        {
            return new FileInfo(Path.Combine("update", "client-v" + latestVersion + ".zip"));
        }

        public void Startup()
        {
            UpdateJob();
        }
    }
}
