using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NETWORG.Utilities.LogicApps.FluentPollingTriggerBuilder
{
    public class FluentAsyncPollingTrigger<TState, TDto> where TState : new()
    {
        private readonly ILogger _log = new NullLoggerFactory().CreateLogger("NULL");
        private PollingTask _pollingTask;
        private UpdateStateFunction _updateStateFunction;
        private StateFactory _stateFactory = () => new TState();
        public delegate Task<ICollection<TDto>> PollingTask(TState state);
        public delegate TState UpdateStateFunction(TState state, ICollection<TDto> polled);
        public delegate TState StateFactory();

        private Predicate<TState> _isPollingStateEmptyPredicate = IsPollingStateEmptyDefaultPredicate;

        public TimeSpan RetryTime { get; set; } = TimeSpan.FromSeconds(15);

        public FluentAsyncPollingTrigger()
        {
        }

        public FluentAsyncPollingTrigger(ILogger log) => _log = log;

        public FluentAsyncPollingTrigger(ILogger<FluentAsyncPollingTrigger<TState, TDto>> log) : this((ILogger)log)
        {
        }

        private static bool IsPollingStateEmptyDefaultPredicate(TState state) =>
            state == null || state.GetType().GetProperties().All(x => x.GetValue(state) == null);

        private bool IsPollingStateEmpty(TState state) =>
            _isPollingStateEmptyPredicate(state);

        /// <summary>
        /// Set polling task that poll for changes with given state.
        /// </summary>
        /// <example> 
        /// This sample shows how to call the method.
        /// <code>
        /// Trigger.SetPollingTask(state => await service.PollAsync(state.Timestamp));
        /// </code>
        /// </example>
        /// <param name="task">Task that polls with given state.</param>
        /// <returns></returns>
        public FluentAsyncPollingTrigger<TState, TDto> SetPollingTask(PollingTask task)
        {
            _pollingTask = task;
            return this;
        }

        /// <summary>
        /// Set updating function
        /// </summary>
        /// <param name="updateStateFunction">Function that changes state from one to another.</param>
        /// <example> 
        /// This sample shows how to call the method.
        /// <code>
        /// Trigger.SetStateUpdate((state, polled) => new State() {Timestamp = polled.Last().Timestamp});
        /// </code>
        /// </example>
        /// <returns></returns>
        public FluentAsyncPollingTrigger<TState, TDto> SetStateUpdate(UpdateStateFunction updateStateFunction)
        {
            _updateStateFunction = updateStateFunction;
            return this;
        }

        /// <summary>
        /// Set factory, that creates the new state upon first poll
        /// </summary>
        /// <param name="factory">Can be just a constructor or advanced factory method</param>
        /// <returns></returns>
        public FluentAsyncPollingTrigger<TState, TDto> SetStateFactory(StateFactory factory)
        {
            _stateFactory = factory;
            return this;
        }

        /// <summary>
        /// Set polling state is empty predicate
        /// </summary>
        /// <param name="predicate">Predicate, return true if polling state is empty</param>
        /// <returns></returns>
        public FluentAsyncPollingTrigger<TState, TDto> SetPollingStateEmptyPredicate(Predicate<TState> predicate)
        {
            _isPollingStateEmptyPredicate = predicate;
            return this;
        }

        /// <summary>
        /// Poll with current state
        /// </summary>
        /// <param name="state">Current state received in HTTP request</param>
        /// <param name="contextAccessor">Context accessor for accessing Request data</param>
        /// <returns></returns>
        public async Task<PollingModel<TDto, TState>> Poll(TState state, IHttpContextAccessor contextAccessor)
        {
            var request = contextAccessor.HttpContext.Request;
            var path = $"{request.Scheme}://{request.Host.Host}{request.Path}";

            var model = new PollingModel<TDto, TState>
            {
                Path = path,
                RetryAfter = RetryTime,
            };

            if (IsPollingStateEmpty(state))
            {
                _log.LogWarning("Polling state is null or empty. Creating new state.");
                var newState = _stateFactory();
                _log.LogInformation($"New state: {newState.ToQueryString()}");
                return new PollingModel<TDto, TState>
                {
                    Polled = new List<TDto>(),
                    RetryAfter = RetryTime,
                    State = newState,
                    Path = path
                };
            }

            _log.LogInformation($"Polling with state {state.ToQueryString()}");

            // poll
            var polled = await _pollingTask(state);
            model.Polled = polled;

            //observed polled
            if (polled.Any())
            {
                //changes detected
                var newState = _updateStateFunction(state, polled);
                _log.LogInformation($"Polled {polled.Count} items. Updating state to: {newState.ToQueryString()}");
                //update state
                model.State = newState;
            }

            else
            {
                //no changes
                _log.LogInformation("Nothing polled. Polling state unchanged.");
                //keep state unchanged
                model.State = state;
            }

            return model;
        }

        /// <summary>
        /// Poll with current state as IActionResult
        /// </summary>
        /// <param name="state">Current state received in HTTP request</param>
        /// <param name="contextAccessor">Context accessor for accessing Request data</param>
        /// <returns></returns>
        public async Task<IActionResult> PollAsAction(TState state, IHttpContextAccessor contextAccessor)
        {
            var model = await Poll(state, contextAccessor);
            var response = contextAccessor.HttpContext.Response;
            response.Headers.Add("location", model.Location);
            response.Headers.Add("retry-after", model.RetryAfterString);
            return new ObjectResult(model)
            {
                StatusCode = (int?)model.StatusCode
            };
        }
    }
}
