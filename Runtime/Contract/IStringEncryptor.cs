namespace SWeetJelly.Serialization.Contract
{
    public interface IStringEncryptor
    {
        string Encrypt(string plainText, string key);
        string Decrypt(string cipherText, string key);
    }
}