using System;

namespace FluentPollingTriggerBuilder.Tests.DTO
{
    public class PollingDto
    {
        public PollingDto()
        {
            Timestamp = DateTime.UtcNow;
            Skip = 0;
        }

        public DateTime? Timestamp { get; set; }
        public int? Skip { get; set; }
    }
}
