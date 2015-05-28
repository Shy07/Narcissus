


namespace Narcissus
{
    class Zlib
    {
        public static void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }

        private static void compressFile(byte[] input, byte[] output)
        {
            System.IO.MemoryStream inStream = new System.IO.MemoryStream(input);
            zlib.ZOutputStream outZStream = new zlib.ZOutputStream(inStream, zlib.zlibConst.Z_DEFAULT_COMPRESSION);
            //System.IO.FileStream inFileStream = new System.IO.FileStream(inFile, System.IO.FileMode.Open);
            System.IO.MemoryStream outStream = new System.IO.MemoryStream();
            try
            {
                CopyStream(outStream, outZStream);
            }
            finally
            {
                outZStream.Close();
                outStream.Close();
                inStream.Close();
            }
        }

        private static void decompressFile(string inFile, string outFile)
        {
            System.IO.FileStream outFileStream = new System.IO.FileStream(outFile, System.IO.FileMode.Create);
            zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outFileStream);
            System.IO.FileStream inFileStream = new System.IO.FileStream(inFile, System.IO.FileMode.Open);
            try
            {
                CopyStream(inFileStream, outZStream);
            }
            finally
            {
                outZStream.Close();
                outFileStream.Close();
                inFileStream.Close();
            }
        }
    }
}
