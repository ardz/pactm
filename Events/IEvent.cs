using System;

namespace Events
{
    /// <summary>
    /// Event
    /// An event is used to communicate that some action has taken place.
    /// Events should be immutable as it describes something that has already happened. You cannot change the past!
    /// Should be named in past tense. CORRECT=RegistrationCreated, WRONG=CreateRegistration
    /// An event can have multiple handlers or zero.
    /// </summary>
    public interface IEvent
    {
        Guid CorrelationId { get; }
        string Type { get; }
    }
}
