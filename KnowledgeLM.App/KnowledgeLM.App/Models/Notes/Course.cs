using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace KnowledgeLM.App.Models.Notes
{
    using System;
    using System.Collections.Generic;

    namespace KnowledgeLM.App.Models.Notes
    {
        public abstract class CommonItemProperties
        {
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastModified { get; set; }
            public string Location { get; set; }
        }

        public class Course : CommonItemProperties
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public List<Section> Sections { get; set; } = [];
        }

        public class Section : CommonItemProperties
        {
            public int Id { get; set; }
            public int CourseId { get; set; }
            public Course Course { get; set; }
            public string Description { get; set; }
            public List<Resource> Resources { get; set; } = [];
        }

        public class Resource : CommonItemProperties
        {
            public int Id { get; set; }
            public int SectionId { get; set; }
            public Section Section { get; set; }
            public ResourceType ResourceType { get; set; }
        }

        public enum ResourceType
        {
            Image,
            Video,
            Other
        }
    }
}
