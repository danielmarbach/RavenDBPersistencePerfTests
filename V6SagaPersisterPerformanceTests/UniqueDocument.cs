using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace V6SagaPersisterPerformanceTests
{
    class UniqueDocument
    {
        public static string FormatId(Type sagaType, string uniquePropertyKey, object uniquePropertyValue)
        {
            if(uniquePropertyValue == null)
            {
                throw new ArgumentNullException("uniqueProperty", $"Property {uniquePropertyKey} is marked with the [Unique] attribute on {sagaType.Name} but contains a null value. Make sure that all unique properties are set on the SagaData and/or that you have marked the correct properties as unique.");
            }

            // use MD5 hash to get a 16-byte hash of the string
            using(var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(uniquePropertyValue.ToString());
                var hashBytes = provider.ComputeHash(inputBytes);

                // generate a guid from the hash:
                var value = new Guid(hashBytes);

                var id = $"{sagaType.FullName.Replace('+', '-')}/{uniquePropertyKey}/{value}";

                // raven has a size limit of 255 bytes == 127 unicode chars
                if(id.Length > 127)
                {
                    // generate a guid from the hash:
                    var hash = provider.ComputeHash(Encoding.Default.GetBytes(sagaType.FullName + uniquePropertyKey));
                    var key = new Guid(hash);

                    id = $"MoreThan127/{key}/{value}";
                }

                return id;
            }
        }
    }
}
