using System;
using System.Collections.Generic;
using System.Text;

namespace FluentPollingTriggerBuilder.Tests.DTO
{

    public class PollableDto
    {
        public PollableDto(int data) => Data = data;
        public int Data { get; }
        public DateTime Timestamp { get; set; }
    }
}
