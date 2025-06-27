using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RQLite.AWS
{
    public class S3Config
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string AccessKeyID { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool ForcePathStyle { get; set; }
    }

    public class S3ClientOpts
    {
        public bool ForcePathStyle { get; set; }
        public bool Timestamp { get; set; }
    }

    public class S3Client
    {
        public const string AWSS3IDKey = "x-rqlite-auto-backup-id";

        private readonly AmazonS3Client _client;
        private readonly string _bucket;
        private readonly string _key;
        private readonly bool _timestamp;
        public Func<DateTime> Now { get; set; } = () => DateTime.UtcNow;

        public S3Client(string endpoint, string region, string accessKey, string secretKey, string bucket, string key, S3ClientOpts? opts = null)
        {
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(region) };
            if (!string.IsNullOrEmpty(endpoint))
            {
                if (!endpoint.Contains("://"))
                    endpoint = "https://" + endpoint;
                config.ServiceURL = endpoint;
                if (opts?.ForcePathStyle == true)
                    config.ForcePathStyle = true;
            }

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                _client = new AmazonS3Client(accessKey, secretKey, config);
            else
                _client = new AmazonS3Client(config);

            _bucket = bucket;
            _key = key;
            _timestamp = opts?.Timestamp ?? false;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_client.Config.ServiceURL) || _client.Config.ServiceURL.Contains("amazonaws.com"))
                return $"s3://{_bucket}/{_key}";
            return $"{_client.Config.ServiceURL}/{_bucket}/{_key}";
        }

        public Task EnsureBucketAsync()
        {
            return _client.PutBucketAsync(_bucket);
        }

        public async Task UploadAsync(Stream reader, string id)
        {
            var key = _key;
            if (_timestamp)
                key = TimestampedPath(key, Now());
            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = reader
            };
            if (!string.IsNullOrEmpty(id))
                request.Metadata.Add(AWSS3IDKey, id);
            await _client.PutObjectAsync(request);
        }

        public async Task<string> CurrentIDAsync()
        {
            var response = await _client.GetObjectMetadataAsync(_bucket, _key);
            if (response.Metadata.TryGetValue(AWSS3IDKey, out var id))
                return id;
            throw new InvalidOperationException("sum metadata not found");
        }

        public async Task DownloadAsync(Stream writer)
        {
            using var response = await _client.GetObjectAsync(_bucket, _key);
            await response.ResponseStream.CopyToAsync(writer);
        }

        public Task DeleteAsync()
        {
            return _client.DeleteObjectAsync(_bucket, _key);
        }

        public static string TimestampedPath(string path, DateTime t)
        {
            var parts = path.Split('/');
            parts[^1] = $"{t:yyyyMMddHHmmss}_{parts[^1]}";
            return string.Join('/', parts);
        }
    }
}
