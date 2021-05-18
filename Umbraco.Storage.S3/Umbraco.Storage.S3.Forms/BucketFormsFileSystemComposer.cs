using System.Configuration;
using Amazon.S3;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Exceptions;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Forms.Core.Components;
using Umbraco.Forms.Data.FileSystem;
using Umbraco.Storage.S3.Extensions;
using Umbraco.Storage.S3.Services;

namespace Umbraco.Storage.S3.Forms
{

    [ComposeAfter(typeof(UmbracoFormsComposer))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class BucketFormsFileSystemComposer : IComposer
    {
        private const string AppSettingsKey = "BucketFileSystem";
        private readonly char[] Delimiters = "/".ToCharArray();

        public void Compose(Composition composition)
        {

            var bucketName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketName"];
            if (bucketName != null)
            {
                var config = CreateConfiguration();

                composition.RegisterUnique(config);
                composition.Register<IMimeTypeResolver>(new DefaultMimeTypeResolver());

                composition.RegisterUniqueFor<IFileSystem, FormsFileSystemForSavedData>(f => new BucketFileSystem(
                    config: config,
                    mimeTypeResolver: f.GetInstance<IMimeTypeResolver>(),
                    fileCacheProvider: null,
                    logger: f.GetInstance<ILogger>(),
                    s3Client: new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(config.Region))
                ));
            }

        }

        private BucketFileSystemConfig CreateConfiguration()
        {
            var bucketName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketName"];
            var bucketHostName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketHostname"];
            var bucketPrefix = ConfigurationManager.AppSettings[$"{AppSettingsKey}:FormsPrefix"];
            var region = ConfigurationManager.AppSettings[$"{AppSettingsKey}:Region"];
            var fileACL = ConfigurationManager.AppSettings[$"{AppSettingsKey}:FileACL"];
            bool.TryParse(ConfigurationManager.AppSettings[$"{AppSettingsKey}:DisableVirtualPathProvider"], out var disableVirtualPathProvider);

            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullOrEmptyException("BucketName", $"The AWS S3 Bucket File System (Forms) is missing the value '{AppSettingsKey}:BucketName' from AppSettings");

            if (string.IsNullOrEmpty(bucketPrefix))
                throw new ArgumentNullOrEmptyException("BucketPrefix", $"The AWS S3 Bucket File System (Forms) is missing the value '{AppSettingsKey}:FormsPrefix' from AppSettings");

            if (string.IsNullOrEmpty(region))
                throw new ArgumentNullOrEmptyException("Region", $"The AWS S3 Bucket File System (Forms) is missing the value '{AppSettingsKey}:Region' from AppSettings");

            if (disableVirtualPathProvider && string.IsNullOrEmpty(bucketHostName))
                throw new ArgumentNullOrEmptyException("BucketHostname", $"The AWS S3 Bucket File System (Forms) is missing the value '{AppSettingsKey}:BucketHostname' from AppSettings");

            if(string.IsNullOrEmpty(fileACL))
                throw new ArgumentNullOrEmptyException("FileACL", $"The AWS S3 Bucket File System (Forms) is missing the value '{AppSettingsKey}:FileACL' from AppSettings");

            return new BucketFileSystemConfig
            {
                BucketName = bucketName,
                BucketHostName = bucketHostName,
                BucketPrefix = bucketPrefix.Trim(Delimiters),
                Region = region,
                CannedACL = AclExtensions.ParseCannedAcl(fileACL),
                ServerSideEncryptionMethod = "",
                DisableVirtualPathProvider = disableVirtualPathProvider
            };
        }
    }
}
