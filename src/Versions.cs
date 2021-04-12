namespace ProcBridge_CSharp
{
    public class Versions
    {
        private static readonly byte[] V1_0 = {1, 0};
        private static readonly byte[] V1_1 = {1, 1};
        public static readonly byte[] CURRENT = V1_1;

        public static byte[] GetCurrent()
        {
            byte[] copy = new byte[CURRENT.Length];
            CURRENT.CopyTo(copy, 0);
            return copy;
        }

        private Versions()
        {
        }
    }
}