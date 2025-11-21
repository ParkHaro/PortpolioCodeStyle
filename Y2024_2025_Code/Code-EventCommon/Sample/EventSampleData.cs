using Data;

namespace Content.EventCommon
{
    public class EventSampleData : BaseTableData
    {
        public string Id { get; init; }

        public static EventSampleData GetById(string id) => new();
    }
}