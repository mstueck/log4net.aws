using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using log4net.Appender.Language;
using log4net.Core;
using System.Text;
using System.IO;

namespace log4net.Appender
{
    public class S3Appender : BufferingAppenderSkeleton
    {
        private string _bucketName;

        public string BucketName
        {
            get
            {
                if (String.IsNullOrEmpty(_bucketName))
                    throw new ApplicationException(Resource.BucketNameNotSpecified);
                return _bucketName;
            }
            set { _bucketName = value; }
        }

        public override void ActivateOptions()
        {
            var client = new AmazonS3Client();
            ListBucketsResponse response = client.ListBuckets();
            bool found = response.Buckets.Any(bucket => bucket.BucketName == BucketName);

            if (found == false)
            {
                client.PutBucket(new PutBucketRequest().WithBucketName(BucketName));
            }

            base.ActivateOptions();
        }

        /// <summary>
        /// Sends the events.
        /// </summary>
        /// <param name="events">The events that need to be send.</param>
        /// <remarks>
        /// <para>
        /// The subclass must override this method to process the buffered events.
        /// </para>
        /// </remarks>
        protected override void SendBuffer(LoggingEvent[] events)
        {
            var client = new AmazonS3Client();
            UploadEvents(events, client);
        }

        private void UploadEvents(LoggingEvent[] events, AmazonS3 client)
        {
            var key = Filename(Guid.NewGuid().ToString());

            var content = new StringBuilder(events.Count());
            Array.ForEach(events, logEvent =>
                {
                    using (var writer = new StringWriter())
                    {
                        Layout.Format(writer, logEvent);
                        content.AppendLine(writer.ToString());
                    }
                });

            var request = new PutObjectRequest();
            request.WithBucketName(_bucketName);
            request.WithKey(key);
            request.WithContentBody(content.ToString());
            client.PutObject(request);
        }

        private static string Filename(string key)
        {
            return string.Format("{0}.log4net.xml", key);
        }
    }
}
