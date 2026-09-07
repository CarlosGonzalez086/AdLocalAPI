namespace AdLocalAPI.DTOs
{
    public class UploadPhotoDto
    {
        public string Base64 { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
