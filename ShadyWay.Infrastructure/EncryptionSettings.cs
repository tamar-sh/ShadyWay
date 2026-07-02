namespace ShadyWay.Infrastructure
{
    // מחזיק את מפתח ההצפנה שנטען מה-Configuration פעם אחת בהפעלה
    public class EncryptionSettings
    {
        public byte[] Key { get; set; } = Array.Empty<byte>();
    }
}
