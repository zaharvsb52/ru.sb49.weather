namespace Sb49.Security.Core
{
    public interface ICryptoSymmetric
    {
        string Encrypt(string message);
        string Decrypt(string encryptedMessage);
    }
}