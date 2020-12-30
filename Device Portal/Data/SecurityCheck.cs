using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public class SecurityCheck
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public DateTime SubmissionDate { get; set; }

        public int DeviceId { get; set; }
        public Device Device { get; set; }

        public HashSet<SecurityCheckQuestions> Questions { get; set; }
    }

    public class SecurityCheckQuestions
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
    }
}
