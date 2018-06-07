using System;
using Events.EventData;

namespace Events
{
    public class EventCertificatePrinted : IEvent
    {
        public EventCertificatePrinted(CertificatePrintedData data, Guid correlationId, string type)
        {
            Data = data;
            CorrelationId = correlationId;
            Type = type;
        }

        // getter only
        public CertificatePrintedData Data { get; }
        public Guid CorrelationId { get; }
        public string Type { get; }
    }
}
