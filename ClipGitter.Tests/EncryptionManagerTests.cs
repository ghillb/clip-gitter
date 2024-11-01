using Xunit;
using ClipGitter;

namespace ClipGitter.Tests;

public class EncryptionManagerTests
{
    [Fact]
    public void EncryptDecrypt_WithValidPassword_ReturnsOriginalText()
    {
        // Arrange
        string originalText = "Test message";
        string password = "testPassword123";

        // Act
        string encrypted = EncryptionManager.Encrypt(originalText, password);
        string decrypted = EncryptionManager.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void Encrypt_WithDifferentPasswords_ProducesDifferentResults()
    {
        // Arrange
        string text = "Test message";
        string password1 = "password1";
        string password2 = "password2";

        // Act
        string encrypted1 = EncryptionManager.Encrypt(text, password1);
        string encrypted2 = EncryptionManager.Encrypt(text, password2);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_WithWrongPassword_ReturnsErrorMessage()
    {
        // Arrange
        string originalText = "Test message";
        string correctPassword = "correct";
        string wrongPassword = "wrong";

        // Act
        string encrypted = EncryptionManager.Encrypt(originalText, correctPassword);
        string decrypted = EncryptionManager.Decrypt(encrypted, wrongPassword);

        // Assert
        Assert.Equal("Decryption failed. Please provide the correct password.", decrypted);
    }
}
