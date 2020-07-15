# Umbraco S3 Provider

[Amazon Web Services S3](http://aws.amazon.com/s3/) IFileSystem provider for Umbraco 8. Used to offload media and/or forms to the cloud! You don't have to be hosting your code in EC2 to get the benefits like handling large media libraries, freeing up disk space and removing static files from your deployment process.

Many thanks must go to [Elijah Glover](https://github.com/ElijahGlover/) for initially creating this project for Umbraco 7. The upgrade to support Umbraco 8 and Umbraco Forms only builds on his earlier work.

If you encounter any problems feel free to raise an issue, or maybe even a pull request if you're feeling generous!


## Installation & Configuration

Install via NuGet.org
```powershell
Install-Package Our.Umbraco.FileSystemProviders.S3.Media
```
or
```powershell
Install-Package Our.Umbraco.FileSystemProviders.S3.Forms
```

Add the following keys to `~/Web.config`
```xml
<?xml version="1.0"?>
<configuration>
  <appSettings>
    <add key="BucketFileSystem:Region" value="" />
    <add key="BucketFileSystem:BucketName" value="" />
    <add key="BucketFileSystem:MediaPrefix" value="media" />
    <add key="BucketFileSystem:FormsPrefix" value="forms" />
    <add key="BucketFileSystem:BucketHostname" value="" />
    <add key="BucketFileSystem:DisableVirtualPathProvider" value="false" />
  </appSettings>
</configuration>
```

| Key | Required | Default | Description
| --- | --- | --- | --- |
| `Region` | Yes | N/A | The code for the region your S3 bucket is located in, e.g. `eu-west-2` |
| `MediaPrefix` | Sometimes | N/A | The prefix for any media files being added to S3. Essentially a root directory name. Required when using `Umbraco.Storage.S3.Media` |
| `FormsPrefix` | Sometimes | N/A | The prefix for any Umbraco Forms data files being added to S3. Essentially a root directory name. Required when using `Umbraco.Storage.S3.Forms` |
| `BucketName` | Yes | N/A | The name of your S3 bucket. |
| `BucketHostname` | Sometimes | N/A | The hostname for your bucket (e.g. `test-s3-bucket.s3.eu-west-2.amazonaws.com`). Required when `DisableVirtualPathProvider` is set to `true` |
| `DisableVirtualPathProvider` | No | `false` | Setting this to `true` will disable the VPP functionality. See below for more info. |

If `DisableVirtualPathProvider` is set to `false` or left empty, then you'll need to add the following to `~/Web.config`
```xml
<?xml version="1.0"?>
<configuration>
  <location path="Media">
    <system.webServer>
      <handlers>
        <remove name="StaticFileHandler" />
        <add name="StaticFileHandler" path="*" verb="*" preCondition="integratedMode" type="System.Web.StaticFileHandler" />
      </handlers>
    </system.webServer>
  </location>
</configuration>
```
You also need to add the following to `~/Media/Web.config`
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <clear />
      <add name="StaticFileHandler" path="*" verb="*" preCondition="integratedMode" type="System.Web.StaticFileHandler" />
      <add name="StaticFile" path="*" verb="*" modules="StaticFileModule,DefaultDocumentModule,DirectoryListingModule" resourceType="Either" requireAccess="Read" />
    </handlers>
  </system.webServer>
</configuration>
```


## AWS Authentication

Ok so where are the [IAM access keys?](http://docs.aws.amazon.com/IAM/latest/UserGuide/ManagingCredentials.html) Depending on how you host your project they already exist if deploying inside an EC2 instance via environment variables specified during deployment and creation of infrastructure.
It's also a good idea to use [AWS best security practices](http://docs.aws.amazon.com/general/latest/gr/aws-access-keys-best-practices.html). Like not using your root access account, use short lived access keys and don't EVER commit them to source control.

If you aren't using EC2/ElasticBeanstalk to access generated temporary keys, you can put them into `~/Web.config`
```xml
<?xml version="1.0"?>
<configuration>
  <appSettings>
    <add key="AWSAccessKey" value="" />
    <add key="AWSSecretKey" value="" />
  </appSettings>
</configuration>
```


## Should I use the Virtual Path Provider?
Using a custom [Virtual Path Provider](https://msdn.microsoft.com/en-us/library/system.web.hosting.virtualpathprovider%28v=vs.110%29.aspx) (the default configuration) means your files are routed transparently through your domain (e.g. `https://example.com/media`). Anyone visiting your site won't be able to tell your files are stored on S3.

Turning the VPP functionality off will store the full S3 URL for each media item, and this will be visible to anyone visiting your site.

Before making a decision either way you might want to read how Virtual Path Providers affect performance/caching, as it differs from IIS's [unmanaged handler](http://www.paraesthesia.com/archive/2011/05/02/when-staticfilehandler-is-not-staticfilehandler.aspx/).


## Using ImageProcessor
Support for remote files has been added to ImageProcessor in version > `2.3.2`. You'll also want to ensure that you are using Virtual Path Provider as ImageProcessor only hijacks requests when parameters are present in the querystring (like width, height, etc).

```powershell
Install-Package ImageProcessor.Web.Config
```

Replace config file located `~/config/imageprocessor/security.config`
```xml
<?xml version="1.0" encoding="utf-8"?>
<security>
  <services>
    <service prefix="media/" name="CloudImageService" type="ImageProcessor.Web.Services.CloudImageService, ImageProcessor.Web">
      <settings>
        <setting key="MaxBytes" value="8194304"/>
        <setting key="Timeout" value="30000"/>
        <setting key="Host" value="http://{Your Unique Bucket Name}.s3.amazonaws.com/{Your Key Prefix}/"/>
      </settings>
    </service>
  </services>
</security>
```

## Using Minio

It is possible to use the provider with a (self-hosted) Minio (https://min.io). Minio requires that ForcePathStyle config settings is true and there needs to be specified a custom URL for the Minio/S3 instance.

Add the following extra settings for Minio to work

```xml
    <add key="BucketFileSystem:ServiceUrl" value="http://localhost:9000" />
    <add key="BucketFileSystem:ForcePathStyle" value="true" />
```

Note: Minio uses `us-east-1` as the default region.

## Future work on this project
Due to not having access to the original Umbraco.Storage.S3 package name I've released the NuGet package under `Our.Umbraco.FileSystemProviders.S3...`. Add in the Web.config keys and we have 3 different naming conventions. I intend to resolve this at some point in the future. If the Web.config keys are changed then I will ensure the new names are optional and the old keys will continue to work.

I also plan to setup an automated build process for this repository, so it can automatically be published to NuGet.