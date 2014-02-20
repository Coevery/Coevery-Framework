using Coevery.ContentManagement.Records;

namespace Coevery.ContentManagement.Handlers {
    public class VersionContentContext {
        public int Id { get; set; }
        public string ContentType { get; set; }

        public ContentItemRecord ContentItemRecord { get; set; }
        public ContentItemVersionRecord ExistingItemVersionRecord { get; set; }
        public ContentItemVersionRecord BuildingItemVersionRecord { get; set; }

        public ContentItem ExistingContentItem { get; set; }
        public ContentItem BuildingContentItem { get; set; }
    }
}