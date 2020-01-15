NETWORG.Utilities.LogicApps.FluentPollingTriggerBuilder
---
The easiest way, how to support [Logic Apps Polling Trigger](https://docs.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-actions-triggers#trigger-multiple-runs) in your .Net Core API.

## Features
* Fluent API - easy to use functional API, just like others 
* State handling - first polling, previous polling, changes detected, no changes detected
* Splitting - start new Logic App workflow for every polled item

## How to start
1. Initial state factory - How to create initial state when the Logic App start polling
2. Initial state predicate - Recognize empty state - Logic App started polling (timestamp not specified, etc)
3. Polling task - Task that can poll changes based on a current state
4. State transfer - compute new polling state based on previous state and polled data

## What is done automatically
* State serialization and deserialization
* Providing correct Status Code for Logic App
* Providing correct location for next polling in response headers
* Providing correct retry time for next polling in reponse headers

# Use
## Basic usage - polling with timestamp
```cs
public class PollingDto
{
    public PollingDto()
    {
    }

    public PollingDto(DateTime? timeStamp) => TimeStamp = timeStamp;
    public DateTime? TimeStamp { get; set; }
}

// ------------------------------------------------------------------------
private readonly IHttpContextAccessor _contextAccessor;

[HttpGet(nameof(OnItemsUpdated))]
[ProducesResponseType(202)]
[ProducesResponseType(200)]
public Task<IActionResult> OnItemsUpdated([FromQuery] PollingDto pollingState)
{
    var now = DateTime.UtcNow; //record tiem before polling started
    return new FluentAsyncPollingTrigger<PollingDto, PollingOutput>()
        .SetStateFactory(() => new PollingDto(DateTime.UtcNow))
        .SetStateUpdate((oldState, polled) => new PollingDto(now))
        .SetPollingStateEmptyPredicate(state => !state.TimeStamp.HasValue) // timestamp empty, Logic App is polling for the first time
        .SetPollingTask(async state => await items.Where(x => x.UpdatedAt > state.TimeStamp).ToListAsync())
        .PollAsAction(pollingState, _contextAccessor);
}
```

* FluentAsyncPollingTrigger is strongly typed
* You don't have to create new instance every time
* All you have to do is take infromation from the request, and poll data

## Advanced usage - polling with filters
Sometime we only want to track specific data

```cs
public class PollingDto
{
    public PollingDto()
    {
    }

    public PollingDto(DateTime? timeStamp, string companyName)
    {
        TimeStamp = timeStamp;
        CompanyName = companyName;
    }
    public DateTime? TimeStamp { get; set; }
    public string CompanyName {get; set; }
}

// ------------------------------------------------------------------------
private readonly IHttpContextAccessor _contextAccessor;

[HttpGet(nameof(OnItemsUpdated))]
[ProducesResponseType(202)]
[ProducesResponseType(200)]
public Task<IActionResult> OnItemsUpdated([FromQuery] PollingDto pollingState)
{
    var now = DateTime.UtcNow; //record tiem before polling started
    return new FluentAsyncPollingTrigger<PollingDto, PollingOutput>()
    // first, Logic App sends empty state with specified filter (e.g. CompanyName)
    // we create initial polling state, with current timestamp and we keep, filter (CompanyName) unchanged
        .SetStateFactory(() => new PollingDto(DateTime.UtcNow, pollingState.CompanyName))
        .SetStateUpdate((oldState, polled) => {oldState.Timestamp = now; return oldState;})
        .SetPollingStateEmptyPredicate(state => !state.TimeStamp.HasValue) // timestamp empty, Logic App is polling for the first time
        .SetPollingTask(async state => await items.Where(x => x.UpdatedAt > state.TimeStamp && x.Company.Name == state.CompanyName).ToListAsync())
        .PollAsAction(pollingState, _contextAccessor);
}
```

* Timestamp and CompanyName are automatically serialized as query parametrs
* So the next polling location for Logic App would be https://myapi/OnItemsUpdated?timestamp=2020.3.3&companyName=contoso
* Once the Logic App polling trigger calls our API again, the query parameters gets deserialized into PollingDto automatically