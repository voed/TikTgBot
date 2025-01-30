using System.IO;

public static class StreamExtensions
{
    public static byte[] ReadAllBytes(this Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}