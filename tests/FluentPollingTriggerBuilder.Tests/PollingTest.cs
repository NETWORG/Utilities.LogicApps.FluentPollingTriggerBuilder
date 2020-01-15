using FluentPollingTriggerBuilder.Tests.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NETWORG.Utilities.LogicApps.FluentPollingTriggerBuilder.Tests
{
    public class PollingTest
    {
        private static IEnumerable<PollableDto> Collection1()
        {
            yield return new PollableDto(1) { Timestamp = DateTime.Parse("2018-01-02") };
            yield return new PollableDto(2) { Timestamp = DateTime.Parse("2018-01-02") };
            yield return new PollableDto(3) { Timestamp = DateTime.Parse("2018-01-02") };
            yield return new PollableDto(4) { Timestamp = DateTime.Parse("2018-01-02") };
        }

        private static IEnumerable<PollableDto> Collection2()
        {
            yield return new PollableDto(5) { Timestamp = DateTime.Parse("2018-01-03") };
            yield return new PollableDto(6) { Timestamp = DateTime.Parse("2018-01-04") };
        }

        private static IEnumerable<PollableDto> Collection3()
        {
            yield return new PollableDto(7) { Timestamp = DateTime.Parse("2018-01-05") };
            yield return new PollableDto(8) { Timestamp = DateTime.Parse("2018-01-06") };
            yield return new PollableDto(8) { Timestamp = DateTime.Parse("2018-01-06") };
            yield return new PollableDto(8) { Timestamp = DateTime.Parse("2018-01-07") };
        }

        private static IEnumerable<PollableDto> All() => Collection1().Concat(Collection2()).Concat(Collection3());

        [Theory]
        [InlineData("2018-01-01", 200, 15, 10)]
        [InlineData("2018-01-02", 200, 15, 6)]
        [InlineData("2018-01-04", 200, 15, 4)]
        [InlineData("2018-01-07", 202, 15, 0)]
        public async Task AsyncPolling(string dateTime, int expectedStatusCode, int retryAfter, int expectedData)
        {
            var trigger = new FluentAsyncPollingTrigger<PollingDto, PollableDto>()
                .SetPollingTask(async state => await Task.Run(() =>
                {
                    var tmp = All().Where(x => state.Timestamp < x.Timestamp).OrderBy(x => x.Data)
                        .ToList();
                    Assert.Equal(expectedData, tmp.Count);
                    return tmp;
                }))
                .SetStateUpdate((state, polled) =>
                {
                    state.Timestamp = polled.OrderBy(x => x.Timestamp).Last().Timestamp;
                    return state;
                });
            var model = await trigger.Poll(new PollingDto() { Timestamp = DateTime.Parse(dateTime) }, new HttpContextAccessor()
            {
                HttpContext = new DefaultHttpContext()
            });
            Assert.Equal(expectedStatusCode, (int)model.StatusCode);
            Assert.Equal(retryAfter, model.RetryAfter.TotalSeconds);
        }

        [Fact]
        public async Task PollWithEmptyState()
        {
            var trigger = new FluentAsyncPollingTrigger<PollingDto, PollableDto>()
                .SetPollingTask(async state => await Task.Run(() =>
                {
                    var tmp = All().Where(x => state.Timestamp < x.Timestamp).OrderBy(x => x.Data)
                        .ToList();
                    return tmp;
                }))
                .SetStateUpdate((state, polled) =>
                {
                    state.Timestamp = polled.OrderBy(x => x.Timestamp).Last().Timestamp;
                    return state;
                });

            var model = await trigger.Poll(null, new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            });
            Assert.NotNull(model.State.Timestamp);
        }

        [Fact]
        public async Task PollWithEmptyStateFactory()
        {
            var start = new PollingDto();
            var trigger = new FluentAsyncPollingTrigger<PollingDto, PollableDto>()
                .SetStateFactory(() => start)
                .SetPollingTask(async state => await Task.Run(() =>
                {
                    var tmp = All().Where(x => state.Timestamp < x.Timestamp).OrderBy(x => x.Data)
                        .ToList();
                    return tmp;
                }))
                .SetStateUpdate((state, polled) =>
                {
                    state.Timestamp = polled.OrderBy(x => x.Timestamp).Last().Timestamp;
                    return state;
                });

            var model = await trigger.Poll(null, new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            });
            Assert.Equal(start, model.State);
        }

        [Fact]
        public async Task PollActionWithEmptyState()
        {
            var trigger = new FluentAsyncPollingTrigger<PollingDto, PollableDto>()
                .SetPollingTask(async state => await Task.Run(() =>
                {
                    var tmp = All().Where(x => state.Timestamp < x.Timestamp).OrderBy(x => x.Data)
                        .ToList();
                    return tmp;
                }))
                .SetStateUpdate((state, polled) =>
                {
                    state.Timestamp = polled.OrderBy(x => x.Timestamp).Last().Timestamp;
                    return state;
                });

            foreach (var state in new[] { null, new PollingDto() })
            {
                var action = await trigger.PollAsAction(state, new HttpContextAccessor
                {
                    HttpContext = new DefaultHttpContext()
                });

                if (action is ObjectResult o)
                {
                    Assert.Equal(202, o.StatusCode);
                }
                else
                {
                    Assert.False(true);
                }
            }
        }

        [Theory]
        [InlineData("2018-01-01", 200, 15, 10)]
        [InlineData("2018-01-02", 200, 15, 6)]
        [InlineData("2018-01-04", 200, 15, 4)]
        [InlineData("2018-01-07", 202, 15, 0)]
        public async Task AsyncPollingAsAction(string dateTime, int expectedStatusCode, int retryAfter, int expectedData)
        {
            var trigger = new FluentAsyncPollingTrigger<PollingDto, PollableDto>()
                .SetPollingTask(async state => await Task.Run(() =>
                {
                    var tmp = All().Where(x => state.Timestamp < x.Timestamp).OrderBy(x => x.Data)
                        .ToList();
                    Assert.Equal(expectedData, tmp.Count);
                    return tmp;
                }))
                .SetStateUpdate((state, polled) =>
                {
                    state.Timestamp = polled.OrderBy(x => x.Timestamp).Last().Timestamp;
                    return state;
                });

            var action = await trigger.PollAsAction(new PollingDto() { Timestamp = DateTime.Parse(dateTime) }, new HttpContextAccessor()
            {
                HttpContext = new DefaultHttpContext()
            });

            if (action is ObjectResult o)
            {
                Assert.Equal(expectedStatusCode, o.StatusCode);
            }
            else
            {
                Assert.False(true);
            }
        }
    }
}
