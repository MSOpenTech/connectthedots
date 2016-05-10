using System;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WorkerHost.Data.Outputs
{
    public class BlobWriter
    {
        private CloudBlobContainer _ContainerReference;
        private CloudBlockBlob _Blob;
        private Stream _StreamWriter;

        public bool Connect(string blobNamePrefix, string containerName, string storageConnectionString)
        {
            try
            {
                _ContainerReference = SetUpContainer(storageConnectionString, containerName);
                _ContainerReference.CreateIfNotExists();
                
                _Blob = _ContainerReference.GetBlockBlobReference(blobNamePrefix + DateTime.UtcNow.Ticks);
                _StreamWriter = _Blob.OpenWrite();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public void WriteLine(string line)
        {
            if (_StreamWriter == null)
            {
                return;
            }
            try
            {
                var jsonBytes = Encoding.UTF8.GetBytes(line);
                _StreamWriter.Write(jsonBytes, 0, jsonBytes.Length);
            }
            catch (Exception)
            {
            }
        }

        public void Flush()
        {
            try
            {
                _StreamWriter.Flush();
            }
            catch (Exception)
            {
            }
        }

        private CloudBlobContainer SetUpContainer(string storageConnectionString,
            string containerName)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            return cloudBlobContainer;
        }
    }
}
