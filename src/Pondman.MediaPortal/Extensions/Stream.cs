namespace System.IO
{
    /// <summary>
    /// Stream extensions
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Copies the stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        public static void CopyStream(this Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }
    }
}
