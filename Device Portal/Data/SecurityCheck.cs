using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public class SecurityCheck : IEntity
    {
        public int Id { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime? ValidTill { get; set; }
        public DeviceStatus Status { get; set; }
        public DateTime StatusEffectiveDate { get; set; }

        [ForeignKey("User")]
        public string UserName { get; set; }
        public User User { get; set; }

        public int DeviceId { get; set; }
        public Device Device { get; set; }

        public HashSet<SecurityCheckQuestions> Questions { get; set; }
    }

    public class SecurityCheckQuestions : IEntity
    {
        public int Id { get; set; }

        public string Question { get; set; }
        public DeviceType Mask { get; set; }

        public bool Answer { get; set; }
        public string Explanation { get; set; }

        public int SecurityCheckId { get; set; }
        [JsonIgnore]
        public SecurityCheck SecurityCheck { get; set; }
    }

    public class SecurityQuestions
    {
        public int Id { get; set; }

        public string Text { get; set; }
        public DeviceType Mask { get; set; }

        public HashSet<SecurityRecommendation> Recommendations { get; set;}
    }

    [Table("SecurityRecommendations")]
    public class SecurityRecommendation
    {
        public int Id { get; set; }

        public int Order { get; set; }

        public string Content { get; set; }
        [JsonPropertyName("os_type")]
        public OS_Type OS_Type { get; set;} 

        public int SecurityQuestionsId { get; set; }
        [JsonIgnore]
        public SecurityQuestions SecurityQuestion { get; set; }
    }
}
