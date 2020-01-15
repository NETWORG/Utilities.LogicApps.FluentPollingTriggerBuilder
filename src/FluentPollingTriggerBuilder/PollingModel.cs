using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace NETWORG.Utilities.LogicApps.FluentPollingTriggerBuilder
{
    public class PollingModel<TDto, TState>
    {
        public TimeSpan RetryAfter { get; set; }
        public string RetryAfterString => RetryAfter.TotalSeconds.ToString("F0");
        public string Location => $"{Path}{State.ToQueryString()}";
        public TState State { get; set; }
        public string Path { get; set; }
        public ICollection<TDto> Polled { get; set; }
        public HttpStatusCode StatusCode =>
            Polled != null && Polled.Any() ? HttpStatusCode.OK : HttpStatusCode.Accepted;
    }
}
