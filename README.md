NETWORG.Utilities.LogicApps.FluentPollingTriggerBuilder
---
The easiest way, how to support [Logic Apps Polling Trigger](https://docs.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-actions-triggers#trigger-multiple-runs) in your .Net Core API.

## Features
* Fluent API - easy to use functional API, just like others 
* State handling - first polling, previous polling, changes detected, no changes detected
* Splitting - start new Logic App workflow for every polled item

## How to start
1. Initial state factory - How to create initial state when the Logic App start polling
2. Initial state factory - Recognize empty state - Logic App started polling (timestamp not specified, etc)
3. Polling task - Task that can poll changes based on a current state
4. State transfer - compute new polling state based on previous state and polled data