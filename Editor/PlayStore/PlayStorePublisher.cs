﻿using System;
using System.IO;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;

namespace ConsoleGPlayAPITool
{
    using System.Threading;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using UniModules.UniCore.EditorTools.Editor;
    using UniRx;
    using UnityEngine;

    /// <summary>
    /// publish your android artifact by configuration to your target branch
    /// </summary>
    [Serializable]
    public class PlayStorePublisher : IPlayStorePublisher
    {
        public const int UploadSecondsTimeout = 600;
        public const int UploadAwaitTimeout = 200;

        private long _uploadSize = 0;
        private ProgressData _progressData;
        private ReactiveProperty<ProgressData> _progress = new ReactiveProperty<ProgressData>();

        public IReadOnlyReactiveProperty<ProgressData> Progress => _progress;

        public async void Publish(IAndroidDistributionSettings configs)
        {
            _uploadSize = 0;
            _progressData = new ProgressData();
            _progress.Value = _progressData;

            if (configs == null)
            {
                throw new Exception("Cannot load a valid BundleConfig");
            }

            var uploadedFile = configs.ArtifactPath;
            if (!File.Exists(uploadedFile))
            {
                Debug.LogError($"{nameof(PlayStorePublisher)} : File not found");
                return;
            }

            var fileSource = new FileInfo(uploadedFile);

            _uploadSize = fileSource.Length;
            _progressData.Title = uploadedFile;

            //Create publisherService
            using (var androidPublisherService = CreateGoogleConsoleAPIService(configs))
            {
                var appEdit = CreateAppEdit(androidPublisherService, configs);
                await UploadArtifact(androidPublisherService, configs, appEdit);
            }
        }

        private async UniTask UploadArtifact(AndroidPublisherService androidPublisherService,
            IAndroidDistributionSettings configs, AppEdit appEdit)
        {
            var isAppBundle = configs.IsAppBundle;
            var uploader = isAppBundle
                ? new AndroidAppBundlerUploader()
                : new AndroidApkUploader() as IAndroidArtifactUploader;

            Debug.Log($"{nameof(PlayStorePublisher)} : Upload to store With {uploader.GetType().Name}");

            // Upload new apk to developer console
            var upload = uploader.Upload(configs, androidPublisherService, appEdit);

            upload.UploadAsync().Start();
            
            var uploadProgress = upload.GetProgress();

            while (uploadProgress == null || (uploadProgress.Status != UploadStatus.Completed && uploadProgress.Status != UploadStatus.Failed))
            {
                uploadProgress = upload.GetProgress();
                if (uploadProgress != null)
                {
                    OnUploadProgressChanged(uploadProgress);
                }

                Thread.Sleep(UploadAwaitTimeout);
            }

            switch (uploadProgress)
            {
                case {Status: UploadStatus.Completed }:
                    CommitChangesToGooglePlay(androidPublisherService, configs, appEdit);
                    break;
                case {Exception: { }}:
                    throw new Exception(uploadProgress.Exception.Message);
                case {} x when x.Status != UploadStatus.Completed :
                    throw new Exception("File upload failed. Reason: unknown :(");
            }
            
        }

        private void OnUploadProgressChanged(IUploadProgress upload)
        {
            var uploadStatus = _uploadSize == 0 ? 1 : upload.BytesSent / (double) _uploadSize;
            _progressData.Progress = (float) uploadStatus;
            _progressData.Content = $"STATUS: {upload.Status.ToString()} : {upload.BytesSent} : {_uploadSize} bytes";
            _progressData.IsDone = upload.Status == UploadStatus.Completed || upload.Status == UploadStatus.Failed;
            _progress.SetValueAndForceNotify(_progressData);
        }

        private AppEdit CreateAppEdit(
            AndroidPublisherService androidPublisherService,
            IAndroidDistributionSettings configs)
        {
            var edit = androidPublisherService.Edits
                .Insert(null /** no content */, configs.PackageName)
                .Execute();
            
            Debug.Log($"Created edit with id: {edit.Id} (valid for {edit.ExpiryTimeSeconds} seconds)");
            
            return edit;
        }

        private void CommitChangesToGooglePlay(
            AndroidPublisherService androidPublisherService,
            IAndroidDistributionSettings configs,
            AppEdit edit)
        {
            var commitRequest = androidPublisherService.Edits.Commit(configs.PackageName, edit.Id);
            var appEdit = commitRequest.Execute();
            Debug.Log("App edit with id " + appEdit.Id + " has been comitted");
        }

        private AndroidPublisherService CreateGoogleConsoleAPIService(IAndroidDistributionSettings configs)
        {
            var cred = GoogleCredential.FromJson(File.ReadAllText(configs.JsonKeyPath));
            cred = cred.CreateScoped(new[] {AndroidPublisherService.Scope.Androidpublisher});

            // Create the AndroidPublisherService.
            var androidPublisherService = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred
            });
            
            androidPublisherService.HttpClient.Timeout = TimeSpan.FromSeconds(UploadSecondsTimeout);
            
            return androidPublisherService;
        }
    }
}