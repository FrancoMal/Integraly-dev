using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("WebinarRegistrations")]
public class WebinarRegistration
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ContactId { get; set; }

    public int WebinarDateId { get; set; }

    public bool KnowsChatGPT { get; set; }

    public bool KnowsClaude { get; set; }

    public bool KnowsGrok { get; set; }

    public bool KnowsGemini { get; set; }

    public bool KnowsCopilot { get; set; }

    public bool KnowsPerplexity { get; set; }

    public bool KnowsDeepSeek { get; set; }

    [Required]
    [MaxLength(50)]
    public string VibeCodingKnowledge { get; set; } = "no_idea";

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ContactId")]
    public WebinarContact? Contact { get; set; }

    [ForeignKey("WebinarDateId")]
    public WebinarDate? WebinarDate { get; set; }
}
