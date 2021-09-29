namespace Olimpo.BitcoinPKArrayGenerator.Service
{
    public interface IPrivateKeyBytesGenerator
    {
        byte[] GetRandomBytes(int size, int seedStretchingIterations=5000);
    }
}