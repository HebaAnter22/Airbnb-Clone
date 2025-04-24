using System.ComponentModel.DataAnnotations;

namespace API.DTOs.HostVerification
{
    public class  HostVerificationInputDTO
    {
        [Required]
        public string VerificationDocumentUrl1 { get; set; } = string.Empty;

        [Required]
        public string VerificationDocumentUrl2 { get; set; } = string.Empty;


    }
}
